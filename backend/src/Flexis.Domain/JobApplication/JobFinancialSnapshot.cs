namespace Flexis.Domain.JobApplication;

public sealed class JobFinancialSnapshot
{
    private JobFinancialSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public DateOnly CapturedOn { get; private set; }

    public DateTimeOffset CapturedHour { get; private set; }

    public DateTimeOffset CapturedAt { get; private set; }

    public decimal TodayPrice { get; private set; }

    public int TodayTotal { get; private set; }

    public int TodayApplied { get; private set; }

    public int TodayInterviews { get; private set; }

    public decimal MainPrice { get; private set; }

    public int MainTotal { get; private set; }

    public int MainApplied { get; private set; }

    public int MainInterviews { get; private set; }

    public decimal ArchivedPrice { get; private set; }

    public int ArchivedTotal { get; private set; }

    public int ArchivedApplied { get; private set; }

    public int ArchivedInterviews { get; private set; }

    public decimal LifetimePrice { get; private set; }

    public int LifetimeTotal { get; private set; }

    public int LifetimeApplied { get; private set; }

    public int LifetimeInterviews { get; private set; }

    public static DateTimeOffset TruncateToHour(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    public static JobFinancialSnapshot Create(
        Guid userId,
        DateTimeOffset capturedHour,
        decimal todayPrice,
        int todayTotal,
        int todayApplied,
        int todayInterviews,
        decimal mainPrice,
        int mainTotal,
        int mainApplied,
        int mainInterviews,
        decimal archivedPrice,
        int archivedTotal,
        int archivedApplied,
        int archivedInterviews,
        decimal lifetimePrice,
        int lifetimeTotal,
        int lifetimeApplied,
        int lifetimeInterviews)
    {
        var hour = TruncateToHour(capturedHour);
        return new JobFinancialSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CapturedOn = DateOnly.FromDateTime(hour.UtcDateTime),
            CapturedHour = hour,
            CapturedAt = DateTimeOffset.UtcNow,
            TodayPrice = todayPrice,
            TodayTotal = todayTotal,
            TodayApplied = todayApplied,
            TodayInterviews = todayInterviews,
            MainPrice = mainPrice,
            MainTotal = mainTotal,
            MainApplied = mainApplied,
            MainInterviews = mainInterviews,
            ArchivedPrice = archivedPrice,
            ArchivedTotal = archivedTotal,
            ArchivedApplied = archivedApplied,
            ArchivedInterviews = archivedInterviews,
            LifetimePrice = lifetimePrice,
            LifetimeTotal = lifetimeTotal,
            LifetimeApplied = lifetimeApplied,
            LifetimeInterviews = lifetimeInterviews
        };
    }

    public void Replace(
        decimal todayPrice,
        int todayTotal,
        int todayApplied,
        int todayInterviews,
        decimal mainPrice,
        int mainTotal,
        int mainApplied,
        int mainInterviews,
        decimal archivedPrice,
        int archivedTotal,
        int archivedApplied,
        int archivedInterviews,
        decimal lifetimePrice,
        int lifetimeTotal,
        int lifetimeApplied,
        int lifetimeInterviews)
    {
        CapturedAt = DateTimeOffset.UtcNow;
        TodayPrice = todayPrice;
        TodayTotal = todayTotal;
        TodayApplied = todayApplied;
        TodayInterviews = todayInterviews;
        MainPrice = mainPrice;
        MainTotal = mainTotal;
        MainApplied = mainApplied;
        MainInterviews = mainInterviews;
        ArchivedPrice = archivedPrice;
        ArchivedTotal = archivedTotal;
        ArchivedApplied = archivedApplied;
        ArchivedInterviews = archivedInterviews;
        LifetimePrice = lifetimePrice;
        LifetimeTotal = lifetimeTotal;
        LifetimeApplied = lifetimeApplied;
        LifetimeInterviews = lifetimeInterviews;
    }
}
