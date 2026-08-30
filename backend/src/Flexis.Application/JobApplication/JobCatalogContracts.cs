namespace Flexis.Application.JobApplication;

public sealed record JobCatalogItemDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    string Url);

public sealed record JobCatalogWriteRequest(string Title, string Url);
