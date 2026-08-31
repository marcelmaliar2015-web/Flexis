namespace Flexis.Application.JobApplication;

public sealed record JobPipelineOptionDto(Guid Id, string Title);

public sealed record JobPipelineSourceOptionDto(
    Guid Id,
    string Title,
    IReadOnlyList<SourceLocationDto> Locations);

public sealed record JobPipelineEntryDto(
    Guid Id,
    Guid ProfileId,
    Guid SourceId,
    int LocationSheetId,
    string LocationName,
    DateTimeOffset CreatedAt);

public sealed record JobPipelineBoardDto(
    IReadOnlyList<JobPipelineEntryDto> Entries,
    IReadOnlyList<JobPipelineOptionDto> Profiles,
    IReadOnlyList<JobPipelineSourceOptionDto> Sources);

public sealed record JobPipelineWriteRequest(Guid ProfileId, Guid SourceId, int LocationSheetId);

public sealed record JobPipelineUpdateResultDto(int Added, int Skipped);

public sealed record JobPipelineForwardResultDto(string ArchivedSheetName, string MainSheetName);

public sealed record JobPipelineBatchForwardResultDto(int Forwarded);
