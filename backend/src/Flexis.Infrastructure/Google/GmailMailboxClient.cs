using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Flexis.Application.Common;
using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;

namespace Flexis.Infrastructure.Google;

internal sealed class GmailMailboxClient : IMailMailbox
{
    private const string Base = "https://gmail.googleapis.com/gmail/v1/users/me";
    private const int CandidatePageSize = 25;
    private const int LabeledPageSize = 40;
    private const int BodyLimit = 8000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string CandidateQuery = string.Join(
        ' ',
        "(in:inbox OR in:spam OR category:updates OR category:promotions OR category:forums OR category:social)",
        "-in:trash -in:draft -in:sent",
        $"-label:\"{MailCheckLabels.InterviewScheduled}\"",
        $"-label:\"{MailCheckLabels.WaitingForAnswer}\"",
        $"-label:\"{MailCheckLabels.NeedToSchedule}\"",
        $"-label:\"{MailCheckLabels.Others}\"");

    private readonly HttpClient _http;

    public GmailMailboxClient(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<IReadOnlyDictionary<MailCheckDecision, string>> EnsureLabelsAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var listed = await SendJson<LabelList>(
            accessToken,
            HttpMethod.Get,
            $"{Base}/labels",
            null,
            cancellationToken);
        var byName = (listed.Labels ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Id!, StringComparer.Ordinal);
        var map = new Dictionary<MailCheckDecision, string>();
        foreach (var decision in MailCheckLabels.KeepDecisions)
        {
            var name = MailCheckLabels.NameFor(decision);
            if (byName.TryGetValue(name, out var existing))
            {
                map[decision] = existing;
                continue;
            }

            var created = await SendJson<GmailLabel>(
                accessToken,
                HttpMethod.Post,
                $"{Base}/labels",
                new
                {
                    name,
                    labelListVisibility = "labelShow",
                    messageListVisibility = "show"
                },
                cancellationToken);
            if (string.IsNullOrWhiteSpace(created.Id))
            {
                throw new GoogleOAuthException($"Gmail did not create the {name} label.");
            }

            map[decision] = created.Id;
        }

        return map;
    }

    public async Task<MailCandidatePage> ListCandidatesAsync(
        string accessToken,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        var url = $"{Base}/messages?maxResults={CandidatePageSize}&includeSpamTrash=true&q={Uri.EscapeDataString(CandidateQuery)}";
        if (!string.IsNullOrWhiteSpace(pageToken))
        {
            url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
        }

        var listed = await SendJson<MessageList>(accessToken, HttpMethod.Get, url, null, cancellationToken);
        var messages = (listed.Messages ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(item => new MailMessageRef(item.Id!))
            .ToList();
        return new MailCandidatePage(messages, listed.NextPageToken);
    }

    public async Task<MailMessageContent> GetMessageAsync(
        string accessToken,
        string messageId,
        CancellationToken cancellationToken)
    {
        var payload = await SendJson<GmailMessage>(
            accessToken,
            HttpMethod.Get,
            $"{Base}/messages/{Uri.EscapeDataString(messageId)}?format=full",
            null,
            cancellationToken);
        return ToContent(payload);
    }

    public Task ApplyLabelAndPinAsync(
        string accessToken,
        string messageId,
        string labelId,
        IReadOnlyList<string> currentLabelIds,
        CancellationToken cancellationToken)
    {
        var add = new List<string> { labelId, "STARRED" };
        var remove = new List<string>();
        if (currentLabelIds.Contains("SPAM", StringComparer.Ordinal))
        {
            remove.Add("SPAM");
            if (!currentLabelIds.Contains("INBOX", StringComparer.Ordinal))
            {
                add.Add("INBOX");
            }
        }

        return SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"{Base}/messages/{Uri.EscapeDataString(messageId)}/modify",
            new { addLabelIds = add.Distinct(StringComparer.Ordinal).ToArray(), removeLabelIds = remove.ToArray() },
            cancellationToken);
    }

    public Task TrashAsync(string accessToken, string messageId, CancellationToken cancellationToken)
    {
        return SendJson<object>(
            accessToken,
            HttpMethod.Post,
            $"{Base}/messages/{Uri.EscapeDataString(messageId)}/trash",
            null,
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
            if (!labels.TryGetValue(decision, out var labelId))
            {
                continue;
            }

            var url = $"{Base}/messages?maxResults={LabeledPageSize}&includeSpamTrash=true&labelIds={Uri.EscapeDataString(labelId)}";
            var listed = await SendJson<MessageList>(accessToken, HttpMethod.Get, url, null, cancellationToken);
            foreach (var row in listed.Messages ?? [])
            {
                if (string.IsNullOrWhiteSpace(row.Id))
                {
                    continue;
                }

                var payload = await SendJson<GmailMessage>(
                    accessToken,
                    HttpMethod.Get,
                    $"{Base}/messages/{Uri.EscapeDataString(row.Id)}?format=metadata&metadataHeaders=From&metadataHeaders=Subject&metadataHeaders=Date",
                    null,
                    cancellationToken);
                var content = ToContent(payload);
                results.Add(new MailLabeledMessage(
                    content.Id,
                    content.ThreadId,
                    content.Subject,
                    content.From,
                    content.Date,
                    content.Snippet,
                    decision,
                    content.LabelIds.Contains("STARRED", StringComparer.Ordinal)));
            }
        }

        return results
            .OrderByDescending(item => item.Date, StringComparer.Ordinal)
            .ToList();
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
            throw new GoogleOAuthException(ReadGoogleError(payload, "Gmail request failed."));
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(payload))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new GoogleOAuthException("Gmail returned an empty payload.");
    }

    private static MailMessageContent ToContent(GmailMessage payload)
    {
        var headers = payload.Payload?.Headers ?? [];
        var subject = Header(headers, "Subject");
        var from = Header(headers, "From");
        var date = Header(headers, "Date");
        if (string.IsNullOrWhiteSpace(date) && long.TryParse(payload.InternalDate, out var ms))
        {
            date = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToString("u");
        }

        var body = ReadBody(payload.Payload);
        if (string.IsNullOrWhiteSpace(body))
        {
            body = payload.Snippet ?? string.Empty;
        }

        if (body.Length > BodyLimit)
        {
            body = body[..BodyLimit];
        }

        return new MailMessageContent(
            payload.Id ?? string.Empty,
            payload.ThreadId ?? string.Empty,
            subject,
            from,
            date,
            payload.Snippet ?? string.Empty,
            body,
            payload.LabelIds ?? []);
    }

    private static string ReadBody(GmailPart? part)
    {
        if (part is null)
        {
            return string.Empty;
        }

        var plain = FindPart(part, "text/plain");
        if (!string.IsNullOrWhiteSpace(plain))
        {
            return plain;
        }

        var html = FindPart(part, "text/html");
        if (!string.IsNullOrWhiteSpace(html))
        {
            return StripHtml(html);
        }

        return Decode(part.Body?.Data);
    }

    private static string FindPart(GmailPart part, string mime)
    {
        if (string.Equals(part.MimeType, mime, StringComparison.OrdinalIgnoreCase))
        {
            var text = Decode(part.Body?.Data);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        foreach (var child in part.Parts ?? [])
        {
            var nested = FindPart(child, mime);
            if (!string.IsNullOrWhiteSpace(nested))
            {
                return nested;
            }
        }

        return string.Empty;
    }

    private static string Decode(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return string.Empty;
        }

        var padded = data.Replace('-', '+').Replace('_', '/');
        var remainder = padded.Length % 4;
        if (remainder != 0)
        {
            padded = padded.PadRight(padded.Length + (4 - remainder), '=');
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }

    private static string StripHtml(string html)
    {
        var withoutTags = Regex.Replace(html, "<[^>]+>", " ");
        return Regex.Replace(withoutTags, @"\s+", " ").Trim();
    }

    private static string Header(IReadOnlyList<GmailHeader> headers, string name)
    {
        return headers.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))?.Value
            ?? string.Empty;
    }

    private static string ReadGoogleError(string payload, string fallback)
    {
        try
        {
            var error = JsonSerializer.Deserialize<GoogleErrorEnvelope>(payload, JsonOptions);
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

    private sealed class LabelList
    {
        public List<GmailLabel>? Labels { get; set; }
    }

    private sealed class GmailLabel
    {
        public string? Id { get; set; }

        public string? Name { get; set; }
    }

    private sealed class MessageList
    {
        public List<GmailRef>? Messages { get; set; }

        public string? NextPageToken { get; set; }
    }

    private sealed class GmailRef
    {
        public string? Id { get; set; }
    }

    private sealed class GmailMessage
    {
        public string? Id { get; set; }

        public string? ThreadId { get; set; }

        public string? Snippet { get; set; }

        public string? InternalDate { get; set; }

        public List<string>? LabelIds { get; set; }

        public GmailPart? Payload { get; set; }
    }

    private sealed class GmailPart
    {
        public string? MimeType { get; set; }

        public List<GmailHeader>? Headers { get; set; }

        public GmailBody? Body { get; set; }

        public List<GmailPart>? Parts { get; set; }
    }

    private sealed class GmailBody
    {
        public string? Data { get; set; }
    }

    private sealed class GmailHeader
    {
        public string? Name { get; set; }

        public string? Value { get; set; }
    }

    private sealed class GoogleErrorEnvelope
    {
        public GoogleErrorBody? Error { get; set; }
    }

    private sealed class GoogleErrorBody
    {
        public string? Message { get; set; }
    }
}
