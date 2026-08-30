using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobCatalogRepository
{
    Task<IReadOnlyList<JobCatalogItem>> ListAsync(
        Guid userId,
        JobCatalogKind kind,
        CancellationToken cancellationToken);

    Task<JobCatalogItem?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> TitleExistsAsync(
        Guid userId,
        JobCatalogKind kind,
        string title,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task AddAsync(JobCatalogItem item, CancellationToken cancellationToken);

    void Remove(JobCatalogItem item);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
