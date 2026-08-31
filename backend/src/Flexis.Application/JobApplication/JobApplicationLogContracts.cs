namespace Flexis.Application.JobApplication;

public sealed record JobApplicationLogDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string Category,
    string Action,
    string Summary,
    string Detail);
