namespace Flexis.Domain.MailCheck;

public sealed class MailCheckScanState
{
    private MailCheckScanState()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid MailConnectionId { get; private set; }

    public DateTimeOffset? CheckedUntilAt { get; private set; }

    public DateTimeOffset? LastScanAt { get; private set; }

    public bool ScanCaughtUp { get; private set; }

    public static MailCheckScanState Create(Guid userId, Guid mailConnectionId)
    {
        return new MailCheckScanState
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MailConnectionId = mailConnectionId
        };
    }

    public void RecordMessageChecked(DateTimeOffset messageDate)
    {
        LastScanAt = DateTimeOffset.UtcNow;
        ScanCaughtUp = false;
        if (CheckedUntilAt is null || messageDate < CheckedUntilAt)
        {
            CheckedUntilAt = messageDate;
        }
    }

    public void TouchScan()
    {
        LastScanAt = DateTimeOffset.UtcNow;
    }

    public void MarkCaughtUp()
    {
        LastScanAt = DateTimeOffset.UtcNow;
        ScanCaughtUp = true;
    }

    public void Reset()
    {
        CheckedUntilAt = null;
        LastScanAt = null;
        ScanCaughtUp = false;
    }
}
