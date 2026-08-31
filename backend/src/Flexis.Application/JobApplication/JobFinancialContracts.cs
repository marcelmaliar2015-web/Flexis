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
    decimal Price);

public sealed record JobFinancialBoardDto(
    JobFinancialDefaultsDto Defaults,
    IReadOnlyList<JobFinancialRowDto> Rows,
    decimal AllPrice,
    int AllTotal,
    int AllApplied,
    int AllInterviews);
