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

    Task RemoveByCatalogItemIdAsync(Guid userId, Guid catalogItemId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
