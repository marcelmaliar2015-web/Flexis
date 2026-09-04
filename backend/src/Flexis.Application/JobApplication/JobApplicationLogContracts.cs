namespace Flexis.Application.JobApplication;

public sealed record JobApplicationLogDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string Category,
    string Action,
    string Summary,
    string Detail);

public sealed record JobApplicationLogPageDto(
    IReadOnlyList<JobApplicationLogDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record JobApplicationLogQuery(
    int Page,
    int PageSize,
    string? Category,
    string? Query);
