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
    private const int BodyLimit = 8000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string InboxFolder = "inbox";
    private const string JunkFolder = "junkemail";

    private readonly HttpClient _http;

    public OutlookMailboxClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<IReadOnlyDictionary<MailCheckDecision, string>> EnsureLabelsAsync(
        string accessToken,
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
            .GroupBy(item => item.DisplayName!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Key, StringComparer.Ordinal);
        var map = new Dictionary<MailCheckDecision, string>();
        foreach (var decision in MailCheckLabels.KeepDecisions)
        {
            var name = MailCheckLabels.NameFor(decision);
            if (byName.ContainsKey(name))
            {
                map[decision] = name;
                continue;
            }

            await SendJson<object>(
                accessToken,
                HttpMethod.Post,
                $"{GraphBase}/me/outlook/masterCategories",
                new { displayName = name, color = "preset0" },
                cancellationToken);
            map[decision] = name;
        }

        return map;
    }

    public async Task<MailCandidatePage> ListCandidatesAsync(
        string accessToken,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        var (folder, graphUrl) = ParsePageToken(pageToken);
        var url = graphUrl
            ?? $"{GraphBase}/me/mailFolders/{folder}/messages?$top={CandidatePageSize}&$orderby=receivedDateTime desc&$select=id,categories";
        var page = await FetchCandidatePageAsync(accessToken, url, cancellationToken);
        if (!string.IsNullOrWhiteSpace(page.NextPageToken))
        {
            return new MailCandidatePage(page.Messages, EncodePageToken(folder, page.NextPageToken));
        }

        if (string.Equals(folder, InboxFolder, StringComparison.Ordinal))
        {
            if (page.Messages.Count == 0)
            {
                return await ListCandidatesAsync(accessToken, EncodePageToken(JunkFolder, null), cancellationToken);
            }

            return new MailCandidatePage(page.Messages, EncodePageToken(JunkFolder, null));
        }

        return new MailCandidatePage(page.Messages, null);
    }

    private static (string Folder, string? GraphUrl) ParsePageToken(string? pageToken)
    {
        if (string.IsNullOrWhiteSpace(pageToken))
        {
            return (InboxFolder, null);
        }

        var separator = pageToken.IndexOf('|');
        if (separator <= 0)
        {
            if (string.Equals(pageToken, JunkFolder, StringComparison.Ordinal)
                || string.Equals(pageToken, InboxFolder, StringComparison.Ordinal))
            {
                return (pageToken, null);
            }

            return (InboxFolder, pageToken);
        }

        var folder = pageToken[..separator];
        var graphUrl = pageToken[(separator + 1)..];
        if (!string.Equals(folder, InboxFolder, StringComparison.Ordinal)
            && !string.Equals(folder, JunkFolder, StringComparison.Ordinal))
        {
            return (InboxFolder, pageToken);
        }

        return (folder, string.IsNullOrWhiteSpace(graphUrl) ? null : graphUrl);
    }

    private static string EncodePageToken(string folder, string? graphUrl)
    {
        return string.IsNullOrWhiteSpace(graphUrl) ? folder : $"{folder}|{graphUrl}";
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

        return ToContent(payload, labels);
    }

    public async Task ApplyLabelAndPinAsync(
        string accessToken,
        string messageId,
        string labelId,
        IReadOnlyList<string> currentLabelIds,
        CancellationToken cancellationToken)
    {
        var categories = currentLabelIds
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Append(labelId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        await SendJson<object>(
            accessToken,
            HttpMethod.Patch,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}",
            new
            {
                categories,
                flag = new { flagStatus = "flagged" }
            },
            cancellationToken);

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

    public Task TrashAsync(string accessToken, string messageId, CancellationToken cancellationToken)
    {
        return SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"{GraphBase}/me/messages/{Uri.EscapeDataString(messageId)}/move",
            new { destinationId = "deleteditems" },
            cancellationToken);
    }

    public async Task<IReadOnlyList<MailLabeledMessage>> ListLabeledAsync(
        string accessToken,
        IReadOnlyDictionary<MailCheckDecision, string> labels,
        MailCheckDecision? filter,
        CancellationToken cancellationToken)
    {
        var wanted = filter is MailCheckDecision one
            ? new[] { one }
            : MailCheckLabels.KeepDecisions.ToArray();
        var results = new List<MailLabeledMessage>();
        foreach (var decision in wanted)
        {
            if (!labels.TryGetValue(decision, out var categoryName))
            {
                continue;
            }

            var escaped = categoryName.Replace("'", "''", StringComparison.Ordinal);
            var url =
                $"{GraphBase}/me/messages?$top={LabeledPageSize}&$orderby=receivedDateTime desc&$select=id,conversationId,subject,from,receivedDateTime,bodyPreview,categories,flag&$filter=categories/any(c:c eq '{escaped}')";
            var listed = await SendJson<MessageList>(accessToken, HttpMethod.Get, url, null, cancellationToken);
            foreach (var row in listed.Value ?? [])
            {
                if (string.IsNullOrWhiteSpace(row.Id))
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
                    decision,
                    string.Equals(row.Flag?.FlagStatus, "flagged", StringComparison.OrdinalIgnoreCase)));
            }
        }

        return results
            .OrderByDescending(item => item.Date, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<MailCandidatePage> FetchCandidatePageAsync(
        string accessToken,
        string url,
        CancellationToken cancellationToken)
    {
        var listed = await SendJson<MessageList>(accessToken, HttpMethod.Get, url, null, cancellationToken);
        var ourNames = MailCheckLabels.KeepDecisions
            .Select(MailCheckLabels.NameFor)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var messages = (listed.Value ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Where(item => item.Categories is null || !item.Categories.Any(ourNames.Contains))
            .Select(item => new MailMessageRef(item.Id!))
            .ToList();
        return new MailCandidatePage(messages, listed.NextLink);
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
