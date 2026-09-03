namespace Flexis.Application.MailCheck;

public sealed record MailCheckActionLogDto(
    Guid Id,
    Guid RunId,
    DateTimeOffset OccurredAt,
    string Source,
    Guid? MailboxId,
    string MailboxEmail,
    string MailboxProvider,
    string MessageId,
    string Subject,
    string From,
    string Action,
    string Label,
    string Detail,
    int DurationMs);

public sealed record MailCheckActionLogPageDto(
    IReadOnlyList<MailCheckActionLogDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record MailCheckActionLogQuery(
    int Page,
    int PageSize,
    string? Source,
    string? Action,
    Guid? MailboxId,
    string? Query);
