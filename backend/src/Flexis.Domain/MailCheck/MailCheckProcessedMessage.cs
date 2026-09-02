namespace Flexis.Domain.MailCheck;

public sealed class MailCheckProcessedMessage
{
    private MailCheckProcessedMessage()
    {
        MessageId = string.Empty;
        Label = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid MailConnectionId { get; private set; }

    public string MessageId { get; private set; }

    public string Label { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }

    public static MailCheckProcessedMessage Create(
        Guid userId,
        Guid mailConnectionId,
        string messageId,
        MailCheckLabel label)
    {
        return new MailCheckProcessedMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MailConnectionId = mailConnectionId,
            MessageId = messageId,
            Label = label.ToString().ToLowerInvariant(),
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateLabel(MailCheckLabel label)
    {
        Label = label.ToString().ToLowerInvariant();
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
