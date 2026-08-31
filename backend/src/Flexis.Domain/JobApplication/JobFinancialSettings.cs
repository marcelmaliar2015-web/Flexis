namespace Flexis.Domain.JobApplication;

public sealed class JobFinancialSettings
{
    public const decimal DefaultApplyRate = 0.06m;

    public const decimal DefaultBonusRate = 1.5m;

    private JobFinancialSettings()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public decimal ApplyRate { get; private set; }

    public decimal BonusRate { get; private set; }

    public static JobFinancialSettings Create(Guid userId)
    {
        return new JobFinancialSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ApplyRate = DefaultApplyRate,
            BonusRate = DefaultBonusRate
        };
    }

    public void SetRates(decimal applyRate, decimal bonusRate)
    {
        ApplyRate = applyRate;
        BonusRate = bonusRate;
    }
}
