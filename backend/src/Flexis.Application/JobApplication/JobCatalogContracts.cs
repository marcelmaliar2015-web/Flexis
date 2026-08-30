namespace Flexis.Application.JobApplication;

public sealed record JobCatalogItemDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    string Url,
    string SpreadsheetId);

public sealed record JobCatalogWriteRequest(string Title);

public sealed record SourceLocationDto(int SheetId, string Name);

public sealed record SourceLocationWriteRequest(string Name);
