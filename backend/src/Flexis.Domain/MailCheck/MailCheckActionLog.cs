namespace Flexis.Domain.MailCheck;

public sealed class MailCheckActionLog
{
    private MailCheckActionLog()
    {
        Source = string.Empty;
        MailboxEmail = string.Empty;
        MailboxProvider = string.Empty;
        MessageId = string.Empty;
        Subject = string.Empty;
        FromAddress = string.Empty;
        Action = string.Empty;
        Label = string.Empty;
        Detail = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RunId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string Source { get; private set; }

    public Guid? MailConnectionId { get; private set; }

    public string MailboxEmail { get; private set; }

    public string MailboxProvider { get; private set; }

    public string MessageId { get; private set; }

    public string Subject { get; private set; }

    public string FromAddress { get; private set; }

    public string Action { get; private set; }

    public string Label { get; private set; }

    public string Detail { get; private set; }

    public int DurationMs { get; private set; }

    public static MailCheckActionLog CreateMessage(
        Guid userId,
        Guid runId,
        string source,
        Guid mailConnectionId,
        string mailboxEmail,
        string mailboxProvider,
        string messageId,
        string subject,
        string fromAddress,
        string action,
        string label,
        string detail,
        int durationMs)
    {
        return new MailCheckActionLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RunId = runId,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = Clamp(source, 16),
            MailConnectionId = mailConnectionId,
            MailboxEmail = Clamp(mailboxEmail, 320),
            MailboxProvider = Clamp(mailboxProvider, 32),
            MessageId = Clamp(messageId, 512),
            Subject = Clamp(subject, 500),
            FromAddress = Clamp(fromAddress, 500),
            Action = Clamp(action, 64),
            Label = Clamp(label, 64),
            Detail = Clamp(detail, 2000),
            DurationMs = Math.Max(0, durationMs)
        };
    }

    public static MailCheckActionLog CreateRunSummary(
        Guid userId,
        Guid runId,
        string source,
        string action,
        string detail,
        int durationMs,
        string? mailboxEmail = null,
        string? mailboxProvider = null)
    {
        return new MailCheckActionLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RunId = runId,
            OccurredAt = DateTimeOffset.UtcNow,
            Source = Clamp(source, 16),
            MailConnectionId = null,
            MailboxEmail = Clamp(mailboxEmail ?? string.Empty, 320),
            MailboxProvider = Clamp(mailboxProvider ?? string.Empty, 32),
            MessageId = string.Empty,
            Subject = string.Empty,
            FromAddress = string.Empty,
            Action = Clamp(action, 64),
            Label = string.Empty,
            Detail = Clamp(detail, 2000),
            DurationMs = Math.Max(0, durationMs)
        };
    }

    private static string Clamp(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max].TrimEnd();
    }
}
