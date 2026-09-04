namespace Flexis.Domain.JobApplication;

public sealed class JobListingCopyBatch
{
    private JobListingCopyBatch()
    {
        Items = new List<JobListingCopyItem>();
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProfileId { get; private set; }

    public Guid PipelineEntryId { get; private set; }

    public DateTimeOffset CopiedAt { get; private set; }

    public int AddedCount { get; private set; }

    public ICollection<JobListingCopyItem> Items { get; private set; }

    public static JobListingCopyBatch Create(
        Guid userId,
        Guid profileId,
        Guid pipelineEntryId,
        IReadOnlyList<string> listingKeys)
    {
        var batchId = Guid.NewGuid();
        var copiedAt = DateTimeOffset.UtcNow;
        var items = listingKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .Select(key => JobListingCopyItem.Create(batchId, key))
            .ToList();
        return new JobListingCopyBatch
        {
            Id = batchId,
            UserId = userId,
            ProfileId = profileId,
            PipelineEntryId = pipelineEntryId,
            CopiedAt = copiedAt,
            AddedCount = items.Count,
            Items = items
        };
    }
}

public sealed class JobListingCopyItem
{
    private JobListingCopyItem()
    {
        ListingKey = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid BatchId { get; private set; }

    public string ListingKey { get; private set; }

    public static JobListingCopyItem Create(Guid batchId, string listingKey)
    {
        return new JobListingCopyItem
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            ListingKey = listingKey.Trim()
        };
    }
}
