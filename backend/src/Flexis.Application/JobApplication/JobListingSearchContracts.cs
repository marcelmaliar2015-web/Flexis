namespace Flexis.Application.JobApplication;

public sealed record JobSearchProfileOptionDto(Guid? ProfileId, string Title);

public sealed record JobSearchMetaDto(
    IReadOnlyList<JobSearchProfileOptionDto> Profiles,
    IReadOnlyList<string> Statuses,
    string SearchBaseUrl,
    string SearchBaseSpreadsheetId);

public sealed record JobSearchRequest(
    string? Profile,
    string? CompanyName,
    string? Position,
    string? Link,
    string? Jd,
    string? Status);

public sealed record JobSearchHitDto(
    int Rank,
    double Score,
    string Profile,
    string CompanyName,
    string Position,
    string Link,
    string Jd,
    string Download,
    string Status,
    string Issue,
    string Source,
    string SpreadsheetUrl,
    Guid? ProfileId);

public sealed record JobSearchResultDto(
    IReadOnlyList<JobSearchHitDto> Items,
    int Scanned,
    string SearchBaseUrl);
