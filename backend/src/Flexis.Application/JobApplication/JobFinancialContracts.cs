namespace Flexis.Application.JobApplication;

public sealed record JobFinancialDefaultsDto(decimal ApplyRate, decimal BonusRate);

public sealed record JobFinancialRatesRequest(decimal ApplyRate, decimal BonusRate);

public sealed record JobFinancialRowDto(
    Guid EntryId,
    string ProfileTitle,
    string SourceLabel,
    int Total,
    int Applied,
    int Interviews,
    decimal ApplyRate,
    decimal BonusRate,
    decimal Price,
    int ArchivedTotal,
    int ArchivedApplied,
    int ArchivedInterviews,
    decimal ArchivedPrice,
    int LifetimeTotal,
    int LifetimeApplied,
    int LifetimeInterviews,
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
    int LifetimeAllInterviews);
