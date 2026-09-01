namespace Flexis.Domain.MailCheck;

public sealed class MailCheckProcessedMessage
{
    private MailCheckProcessedMessage()
    {
        MessageId = string.Empty;
        Decision = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid MailConnectionId { get; private set; }

    public string MessageId { get; private set; }

    public string Decision { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }

    public static MailCheckProcessedMessage Create(
        Guid userId,
        Guid mailConnectionId,
        string messageId,
        MailCheckDecision decision)
    {
        return new MailCheckProcessedMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MailConnectionId = mailConnectionId,
            MessageId = messageId,
            Decision = decision.ToString(),
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }
}
