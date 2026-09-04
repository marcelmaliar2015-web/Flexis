namespace Flexis.Domain.JobApplication;

public sealed class JobListingStatusState
{
    private JobListingStatusState()
    {
        ListingKey = string.Empty;
        Status = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProfileId { get; private set; }

    public string ListingKey { get; private set; }

    public string Status { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static JobListingStatusState Create(
        Guid userId,
        Guid profileId,
        string listingKey,
        string status)
    {
        return new JobListingStatusState
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfileId = profileId,
            ListingKey = listingKey.Trim(),
            Status = status.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetStatus(string status)
    {
        Status = status.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class JobListingStatusEvent
{
    private JobListingStatusEvent()
    {
        ListingKey = string.Empty;
        Status = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProfileId { get; private set; }

    public string ListingKey { get; private set; }

    public string Status { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static JobListingStatusEvent Create(
        Guid userId,
        Guid profileId,
        string listingKey,
        string status,
        DateTimeOffset occurredAt)
    {
        return new JobListingStatusEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfileId = profileId,
            ListingKey = listingKey.Trim(),
            Status = status.Trim(),
            OccurredAt = occurredAt
        };
    }
}
