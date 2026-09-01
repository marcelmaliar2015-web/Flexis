namespace Flexis.Application.JobApplication;

public sealed record JobResumeBoardDto(
    string? JobMasterUrl,
    IReadOnlyList<string> OwnerOptions,
    IReadOnlyList<JobResumeProfileRowDto> Profiles);

public sealed record JobResumeProfileRowDto(
    Guid ProfileId,
    string Title,
    string Url,
    string Prompt,
    int? ResumeStyle,
    string Owner);

public sealed record JobResumeOwnerOptionsWriteRequest(IReadOnlyList<string> OwnerOptions);

public sealed record JobResumeProfileWriteRequest(
    string? Prompt,
    int? ResumeStyle,
    string? Owner);
