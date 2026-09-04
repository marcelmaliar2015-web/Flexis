namespace Flexis.Domain.JobApplication;

public sealed class JobProfileStatisticsSnapshot
{
    private JobProfileStatisticsSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ProfileId { get; private set; }

    public string ProfileTitle { get; private set; } = string.Empty;

    public DateOnly CapturedOn { get; private set; }

    public DateTimeOffset CapturedHour { get; private set; }

    public DateTimeOffset CapturedAt { get; private set; }

    public int Applied { get; private set; }

    public int Interviews { get; private set; }

    public int Unapplied { get; private set; }

    public int Total { get; private set; }

    public decimal Price { get; private set; }

    public static JobProfileStatisticsSnapshot Create(
        Guid userId,
        Guid profileId,
        string profileTitle,
        DateTimeOffset capturedHour,
        int applied,
        int interviews,
        int unapplied,
        int total,
        decimal price)
    {
        var hour = JobFinancialSnapshot.TruncateToHour(capturedHour);
        return new JobProfileStatisticsSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfileId = profileId,
            ProfileTitle = profileTitle.Trim(),
            CapturedOn = DateOnly.FromDateTime(hour.UtcDateTime),
            CapturedHour = hour,
            CapturedAt = DateTimeOffset.UtcNow,
            Applied = applied,
            Interviews = interviews,
            Unapplied = unapplied,
            Total = total,
            Price = price
        };
    }

    public void Replace(
        string profileTitle,
        int applied,
        int interviews,
        int unapplied,
        int total,
        decimal price)
    {
        CapturedAt = DateTimeOffset.UtcNow;
        ProfileTitle = profileTitle.Trim();
        Applied = applied;
        Interviews = interviews;
        Unapplied = unapplied;
        Total = total;
        Price = price;
    }
}
