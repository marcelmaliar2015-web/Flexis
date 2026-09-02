using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Flexis.Application.Common;
using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;

namespace Flexis.Infrastructure.Microsoft;

internal sealed class OutlookMailboxClient : IMailMailbox
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const int CandidatePageSize = 25;
    private const int LabeledPageSize = 40;
    private const int BodyLimit = 20_000;
    private const string OutlookPinPropertyId1 = "SystemTime 0x0F01";
    private const string OutlookPinPropertyId2 = "SystemTime 0x0F02";
    private const string OutlookPinPropertyValue = "4500-09-01T00:00:00Z";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private HashSet<string>? _cachedExcludedFolderIds;

    public OutlookMailboxClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<IReadOnlyDictionary<MailCheckLabel, string>> EnsureLabelsAsync(
        string accessToken,
        IReadOnlyCollection<MailCheckLabel> labels,
        CancellationToken cancellationToken)
    {
        var listed = await SendJson<CategoryList>(
            accessToken,
            HttpMethod.Get,
            $"{GraphBase}/me/outlook/masterCategories",
            null,
            cancellationToken);
        var byName = (listed.Value ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.DisplayName))
            .GroupBy(item => item.DisplayName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Key, StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<MailCheckLabel, string>();
        foreach (var label in labels)
        {
            if (TryResolveExistingCategory(byName, label, out var existing))
            {
                map[label] = existing;
                continue;
            }

            var name = MailCheckMailboxNames.For(label);
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"{GraphBase}/me/outlook/masterCategories",
                new { displayName = name, color = "preset0" },
                cancellationToken);
            map[label] = name;
        }

        return map;
    }

    private static bool TryResolveExistingCategory(
        IReadOnlyDictionary<string, string> byName,
        MailCheckLabel label,
        out string categoryName)
    {
        foreach (var name in MailCheckMailboxNames.LookupNames(label))
        {
            if (byName.TryGetValue(name, out var existing) && !string.IsNullOrWhiteSpace(existing))
            {
                categoryName = existing;
                return true;
            }
        }

        categoryName = string.Empty;
        return false;
    }

    public async Task<MailCandidatePage> ListCandidatesAsync(
        string accessToken,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pageToken))
        {
            _cachedExcludedFolderIds = null;
        }

        _cachedExcludedFolderIds ??= await GetExcludedFolderIdsAsync(accessToken, cancellationToken);
        var url = string.IsNullOrWhiteSpace(pageToken)
            ? $"{GraphBase}/me/messages?$top={CandidatePageSize}&$orderby=receivedDateTime desc&$select=id,categories,receivedDateTime,parentFolderId&$filter=isDraft eq false"
            : pageToken;
        return await FetchCandidatePageAsync(accessToken, url, _cachedExcludedFolderIds, cancellationToken);
    }

    private async Task<HashSet<string>> GetExcludedFolderIdsAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folderName in new[] { "sentitems", "deleteditems", "drafts", "archive", "outbox" })
        {
            var folder = await SendJson<GraphFolder>(
                accessToken,
                HttpMethod.Get,
                $"{GraphBase}/me/mailFolders/{folderName}?$select=id",
                null,
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(folder.Id))
            {
                excluded.Add(folder.Id);
            }
        }

        return excluded;
    }

    public async Task<MailMessageContent> GetMessageAsync(
        string accessToken,
        string messageId,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<GraphMessage>(
            accessToken,
            HttpMethod.Get,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}?$select=id,conversationId,subject,from,receivedDateTime,bodyPreview,body,categories,flag,parentFolderId",
            null,
            cancellationToken);
        var labels = payload.Categories?.ToList() ?? [];
        var junkFolderId = await GetJunkFolderIdAsync(accessToken, cancellationToken);
        if (!string.IsNullOrWhiteSpace(junkFolderId)
            && string.Equals(payload.ParentFolderId, junkFolderId, StringComparison.Ordinal))
        {
            labels.Add("junkemail");
        }

        var deletedFolderId = await GetDeletedFolderIdAsync(accessToken, cancellationToken);
        if (!string.IsNullOrWhiteSpace(deletedFolderId)
            && string.Equals(payload.ParentFolderId, deletedFolderId, StringComparison.Ordinal))
        {
            labels.Add("TRASH");
        }

        return ToContent(payload, labels);
    }

    public async Task ApplyLabelAsync(
        string accessToken,
        string messageId,
        string labelId,
        IReadOnlyList<string> currentLabelIds,
        bool gmailStar,
        bool pinAction,
        CancellationToken cancellationToken)
    {
        var categories = currentLabelIds
            .Where(item => !string.IsNullOrWhiteSpace(item) && !string.Equals(item, "junkemail", StringComparison.OrdinalIgnoreCase))
            .Append(labelId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var patch = new Dictionary<string, object>
        {
            ["categories"] = categories,
            ["isRead"] = true
        };
        if (pinAction)
        {
            patch["flag"] = new { flagStatus = "notFlagged" };
        }

        await SendJson<object>(
            accessToken,
            HttpMethod.Patch,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}",
            patch,
            cancellationToken);

        if (pinAction)
        {
            await ApplyOutlookPinAsync(accessToken, messageId, cancellationToken);
        }

        if (currentLabelIds.Contains("junkemail", StringComparer.OrdinalIgnoreCase))
        {
            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}/move",
                new { destinationId = "inbox" },
                cancellationToken);
        }
    }

    public Task MarkAsReadAsync(
        string accessToken,
        string messageId,
        IReadOnlyList<string> currentLabelIds,
        CancellationToken cancellationToken)
    {
        _ = currentLabelIds;
        return SendJson<object>(
            accessToken,
            HttpMethod.Patch,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}",
            new { isRead = true },
            cancellationToken);
    }

    public Task TrashAsync(string accessToken, string messageId, CancellationToken cancellationToken)
    {
        return SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}/move",
            new { destinationId = "deleteditems" },
            cancellationToken);
    }

    public async Task CreateDraftReplyAsync(
        string accessToken,
        MailMessageContent message,
        string replyBody,
        CancellationToken cancellationToken)
    {
        var draft = await SendJson<GraphMessage>(
            accessToken,
            HttpMethod.Post,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(message.Id)}/createReply",
            null,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(draft.Id))
        {
            throw new MicrosoftOAuthException("Outlook did not create a reply draft.");
        }

        await SendJson<object>(
            accessToken,
            HttpMethod.Patch,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(draft.Id)}",
            new
            {
                body = new
                {
                    contentType = "Text",
                    content = replyBody.Trim()
                }
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<MailLabeledMessage>> ListLabeledAsync(
        string accessToken,
        IReadOnlyDictionary<MailCheckLabel, string> labels,
        MailCheckLabel? filter,
        CancellationToken cancellationToken)
    {
        var wanted = filter is MailCheckLabel one
            ? new[] { one }
            : labels.Keys.ToArray();
        var results = new List<MailLabeledMessage>();
        foreach (var label in wanted)
        {
            if (!labels.TryGetValue(label, out var categoryName))
            {
                continue;
            }

            var escaped = categoryName.Replace("'", "''", StringComparison.Ordinal);
            var expand = Uri.EscapeDataString($"singleValueExtendedProperties($filter=id eq '{OutlookPinPropertyId2}')");
            var deletedFolderId = await GetDeletedFolderIdAsync(accessToken, cancellationToken);
            var url =
                $"{GraphBase}/me/messages?$top={LabeledPageSize}&$orderby=receivedDateTime desc&$select=id,conversationId,subject,from,receivedDateTime,bodyPreview,categories,parentFolderId&$expand={expand}&$filter=categories/any(c:c eq '{escaped}')";
            var listed = await SendJson<MessageList>(accessToken, HttpMethod.Get, url, null, cancellationToken);
            foreach (var row in listed.Value ?? [])
            {
                if (string.IsNullOrWhiteSpace(row.Id))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(deletedFolderId)
                    && string.Equals(row.ParentFolderId, deletedFolderId, StringComparison.Ordinal))
                {
                    continue;
                }

                var content = ToContent(row);
                results.Add(new MailLabeledMessage(
                    content.Id,
                    content.ThreadId,
                    content.Subject,
                    content.From,
                    content.Date,
                    content.Snippet,
                    label,
                    IsOutlookPinned(row)));
            }
        }

        return results
            .OrderByDescending(item => item.Date, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<MailCandidatePage> FetchCandidatePageAsync(
        string accessToken,
        string url,
        IReadOnlySet<string> excludedFolderIds,
        CancellationToken cancellationToken)
    {
        var listed = await SendJson<MessageList>(accessToken, HttpMethod.Get, url, null, cancellationToken);
        var ourNames = MailCheckMailboxNames.AllLookupNames()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var messages = (listed.Value ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Where(item => string.IsNullOrWhiteSpace(item.ParentFolderId) || !excludedFolderIds.Contains(item.ParentFolderId))
            .Where(item => item.Categories is null || !item.Categories.Any(ourNames.Contains))
            .Select(item => new MailMessageRef(item.Id!, ParseSortDateMs(item.ReceivedDateTime)))
            .ToList();
        return new MailCandidatePage(messages, listed.NextLink);
    }

    private async Task ApplyOutlookPinAsync(
        string accessToken,
        string messageId,
        CancellationToken cancellationToken)
    {
        await SendJson<object>(
            accessToken,
            HttpMethod.Patch,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}",
            new
            {
                singleValueExtendedProperties = new[]
                {
                    new { id = OutlookPinPropertyId1, value = OutlookPinPropertyValue },
                    new { id = OutlookPinPropertyId2, value = OutlookPinPropertyValue },
                }
            },
            cancellationToken);
    }

    private static bool IsOutlookPinned(GraphMessage row)
    {
        foreach (var property in row.SingleValueExtendedProperties ?? [])
        {
            if (!string.Equals(property.Id, OutlookPinPropertyId2, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (DateTime.TryParse(property.Value, out var pinnedAt)
                && pinnedAt >= new DateTime(4500, 9, 1, 0, 0, 0, DateTimeKind.Utc))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<T> SendJson<T>(
        string accessToken,
        HttpMethod method,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new MicrosoftOAuthException(ReadError(payload, "Outlook request failed."));
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(payload))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new MicrosoftOAuthException("Outlook returned an empty payload.");
    }

    private static long ParseSortDateMs(string? receivedDateTime)
    {
        return DateTimeOffset.TryParse(receivedDateTime, out var parsed)
            ? parsed.ToUnixTimeMilliseconds()
            : 0L;
    }

    private static MailMessageContent ToContent(GraphMessage payload, IReadOnlyList<string>? labels = null)
    {
        var from = payload.From?.EmailAddress?.Address ?? string.Empty;
        var date = payload.ReceivedDateTime ?? string.Empty;
        var body = payload.Body?.Content ?? payload.BodyPreview ?? string.Empty;
        if (string.Equals(payload.Body?.ContentType, "html", StringComparison.OrdinalIgnoreCase))
        {
            body = StripHtml(body);
        }

        if (body.Length > BodyLimit)
        {
            body = body[..BodyLimit];
        }

        return new MailMessageContent(
            payload.Id ?? string.Empty,
            payload.ConversationId ?? string.Empty,
            payload.Subject ?? string.Empty,
            from,
            date,
            payload.BodyPreview ?? string.Empty,
            body,
            labels?.ToArray() ?? payload.Categories?.ToArray() ?? []);
    }

    private async Task<string?> GetJunkFolderIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        var folder = await SendJson<GraphFolder>(
            accessToken,
            HttpMethod.Get,
            $"{GraphBase}/me/mailFolders/junkemail?$select=id",
            null,
            cancellationToken);
        return folder.Id;
    }

    private async Task<string?> GetDeletedFolderIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        var folder = await SendJson<GraphFolder>(
            accessToken,
            HttpMethod.Get,
            $"{GraphBase}/me/mailFolders/deleteditems?$select=id",
            null,
            cancellationToken);
        return folder.Id;
    }

    private static string StripHtml(string html)
    {
        var withoutTags = Regex.Replace(html, "<[^>]+>", " ");
        return Regex.Replace(withoutTags, @"\s+", " ").Trim();
    }

    private static string ReadError(string payload, string fallback)
    {
        try
        {
            var error = JsonSerializer.Deserialize<GraphErrorEnvelope>(payload, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Error?.Message))
            {
                return error.Error.Message;
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    private sealed class CategoryList
    {
        public List<GraphCategory>? Value { get; set; }
    }

    private sealed class GraphCategory
    {
        public string? DisplayName { get; set; }
    }

    private sealed class MessageList
    {
        public List<GraphMessage>? Value { get; set; }

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }

    private sealed class GraphMessage
    {
        public string? Id { get; set; }

        public string? ConversationId { get; set; }

        public string? Subject { get; set; }

        public GraphFrom? From { get; set; }

        public string? ReceivedDateTime { get; set; }

        public string? BodyPreview { get; set; }

        public GraphBody? Body { get; set; }

        public List<string>? Categories { get; set; }

        public GraphFlag? Flag { get; set; }

        public string? ParentFolderId { get; set; }

        public List<GraphExtendedProperty>? SingleValueExtendedProperties { get; set; }
    }

    private sealed class GraphExtendedProperty
    {
        public string? Id { get; set; }

        public string? Value { get; set; }
    }

    private sealed class GraphFrom
    {
        public GraphEmailAddress? EmailAddress { get; set; }
    }

    private sealed class GraphEmailAddress
    {
        public string? Address { get; set; }
    }

    private sealed class GraphBody
    {
        public string? ContentType { get; set; }

        public string? Content { get; set; }
    }

    private sealed class GraphFlag
    {
        public string? FlagStatus { get; set; }
    }

    private sealed class GraphFolder
    {
        public string? Id { get; set; }
    }

    private sealed class GraphErrorEnvelope
    {
        public GraphErrorBody? Error { get; set; }
    }

    private sealed class GraphErrorBody
    {
        public string? Message { get; set; }
    }
}
