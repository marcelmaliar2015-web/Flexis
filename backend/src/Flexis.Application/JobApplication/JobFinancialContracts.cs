namespace Flexis.Application.JobApplication;

public sealed record JobFinancialDefaultsDto(decimal ApplyRate, decimal BonusRate);

public sealed record JobFinancialRatesRequest(decimal ApplyRate, decimal BonusRate);

public sealed record JobFinancialRowDto(
    Guid EntryId,
    Guid ProfileId,
    string ProfileTitle,
    string ProfileUrl,
    string SourceLabel,
    int Total,
    int Applied,
    int Interviews,
    int Unapplied,
    decimal ApplyRate,
    decimal BonusRate,
    decimal Price,
    int ArchivedTotal,
    int ArchivedApplied,
    int ArchivedInterviews,
    int ArchivedUnapplied,
    decimal ArchivedPrice,
    int LifetimeTotal,
    int LifetimeApplied,
    int LifetimeInterviews,
    int LifetimeUnapplied,
    decimal LifetimePrice);

public sealed record JobFinancialBoardDto(
    JobFinancialDefaultsDto Defaults,
    IReadOnlyList<JobFinancialRowDto> Rows,
    decimal AllPrice,
    int AllTotal,
    int AllApplied,
    int AllInterviews,
    decimal ArchivedAllPrice,
    int ArchivedAllTotal,
    int ArchivedAllApplied,
    int ArchivedAllInterviews,
    decimal LifetimeAllPrice,
    int LifetimeAllTotal,
    int LifetimeAllApplied,
    int LifetimeAllInterviews,
    IReadOnlyList<JobFinancialSnapshotDto> History);

public sealed record JobFinancialSnapshotDto(
    DateOnly CapturedOn,
    DateTimeOffset CapturedHour,
    DateTimeOffset CapturedAt,
    decimal TodayPrice,
    int TodayTotal,
    int TodayApplied,
    int TodayInterviews,
    decimal ArchivedPrice,
    int ArchivedTotal,
    int ArchivedApplied,
    int ArchivedInterviews,
    decimal LifetimePrice,
    int LifetimeTotal,
    int LifetimeApplied,
    int LifetimeInterviews);

public sealed record JobStatisticsProfileDto(
    Guid ProfileId,
    string ProfileTitle,
    string ProfileUrl,
    int Applied,
    int Interviews,
    int Unapplied,
    int Total,
    decimal Price,
    decimal ApplyRate,
    decimal BonusRate);

public sealed record JobStatisticsPointDto(
    Guid ProfileId,
    string ProfileTitle,
    DateOnly CapturedOn,
    DateTimeOffset CapturedHour,
    int Applied,
    int Interviews,
    int Unapplied,
    int Total,
    decimal Price);

public sealed record JobStatisticsBoardDto(
    IReadOnlyList<JobStatisticsProfileDto> Profiles,
    IReadOnlyList<JobStatisticsPointDto> History,
    int AllApplied,
    int AllInterviews,
    int AllUnapplied,
    int AllTotal,
    decimal AllPrice);
