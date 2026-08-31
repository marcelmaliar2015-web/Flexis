namespace Flexis.Domain.JobApplication;

public sealed class JobPipelineEntry
{
    private JobPipelineEntry()
    {
        LocationName = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProfileId { get; private set; }

    public Guid SourceId { get; private set; }

    public int LocationSheetId { get; private set; }

    public string LocationName { get; private set; }

    public decimal ApplyRate { get; private set; }

    public decimal BonusRate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static JobPipelineEntry Create(
        Guid userId,
        Guid profileId,
        Guid sourceId,
        int locationSheetId,
        string locationName)
    {
        return new JobPipelineEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfileId = profileId,
            SourceId = sourceId,
            LocationSheetId = locationSheetId,
            LocationName = locationName,
            ApplyRate = JobFinancialSettings.DefaultApplyRate,
            BonusRate = JobFinancialSettings.DefaultBonusRate,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Replace(Guid profileId, Guid sourceId, int locationSheetId, string locationName)
    {
        ProfileId = profileId;
        SourceId = sourceId;
        LocationSheetId = locationSheetId;
        LocationName = locationName;
    }

    public void SetRates(decimal applyRate, decimal bonusRate)
    {
        ApplyRate = applyRate;
        BonusRate = bonusRate;
    }
}
