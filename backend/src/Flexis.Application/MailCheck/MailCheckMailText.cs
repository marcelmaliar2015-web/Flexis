namespace Flexis.Application.MailCheck;

public static class MailCheckMailText
{
    public const int MaxBodyChars = 20_000;
    private const int HeadChars = 15_000;
    private const int TailChars = 4_500;
    private const string ClipMarker = "\n\n[... middle truncated for classification ...]\n\n";

    public static string ClipBody(string body)
    {
        if (string.IsNullOrEmpty(body) || body.Length <= MaxBodyChars)
        {
            return body;
        }

        var markerBudget = ClipMarker.Length;
        var head = Math.Min(HeadChars, MaxBodyChars - markerBudget - TailChars);
        if (head < 1_000)
        {
            head = Math.Max(1_000, MaxBodyChars - markerBudget - 500);
        }

        var tail = Math.Min(TailChars, MaxBodyChars - markerBudget - head);
        if (tail < 1)
        {
            return body[..MaxBodyChars];
        }

        return string.Concat(body.AsSpan(0, head), ClipMarker, body.AsSpan(body.Length - tail, tail));
    }

    public static string FormatForClassification(MailMessageContent message)
    {
        var subject = message.Subject.Trim();
        if (subject.Length is 0)
        {
            subject = "(no subject)";
        }

        var body = message.Body;
        if (string.IsNullOrWhiteSpace(body))
        {
            body = message.Snippet;
        }

        body = ClipBody(body.Trim());
        return $"From: {message.From}\nDate: {message.Date}\nSubject: {subject}\n\n{body}";
    }
}
