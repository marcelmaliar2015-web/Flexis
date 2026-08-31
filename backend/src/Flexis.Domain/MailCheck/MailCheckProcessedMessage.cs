namespace Flexis.Domain.MailCheck;

public sealed class MailCheckProcessedMessage
{
    private MailCheckProcessedMessage()
    {
        GmailMessageId = string.Empty;
        Decision = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string GmailMessageId { get; private set; }

    public string Decision { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }

    public static MailCheckProcessedMessage Create(Guid userId, string gmailMessageId, MailCheckDecision decision)
    {
        return new MailCheckProcessedMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GmailMessageId = gmailMessageId,
            Decision = decision.ToString(),
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }
}
