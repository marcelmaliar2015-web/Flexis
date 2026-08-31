using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobPipelineRepository
{
    Task<IReadOnlyList<JobPipelineEntry>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<JobPipelineEntry?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        Guid userId,
        Guid profileId,
        Guid sourceId,
        int locationSheetId,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task AddAsync(JobPipelineEntry entry, CancellationToken cancellationToken);

    void Remove(JobPipelineEntry entry);

    Task<IReadOnlyList<JobPipelineBannedCompany>> ListBannedAsync(
        Guid pipelineEntryId,
        CancellationToken cancellationToken);

    Task<JobPipelineBannedCompany?> GetBannedAsync(
        Guid pipelineEntryId,
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> BannedMatchKeyExistsAsync(
        Guid pipelineEntryId,
        string matchKey,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task AddBannedAsync(JobPipelineBannedCompany company, CancellationToken cancellationToken);

    void RemoveBanned(JobPipelineBannedCompany company);

    Task RemoveByCatalogItemIdAsync(Guid userId, Guid catalogItemId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
