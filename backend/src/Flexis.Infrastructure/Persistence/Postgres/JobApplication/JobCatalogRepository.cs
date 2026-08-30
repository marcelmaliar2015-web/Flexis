using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobCatalogRepository : IJobCatalogRepository
{
    private readonly FlexisDbContext _db;

    public JobCatalogRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JobCatalogItem>> ListAsync(
        Guid userId,
        JobCatalogKind kind,
        CancellationToken cancellationToken)
    {
        return await _db.JobCatalogItems
            .Where(item => item.UserId == userId && item.Kind == kind)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<JobCatalogItem?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return _db.JobCatalogItems.FirstOrDefaultAsync(
            item => item.UserId == userId && item.Id == id,
            cancellationToken);
    }

    public Task<bool> TitleExistsAsync(
        Guid userId,
        JobCatalogKind kind,
        string title,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _db.JobCatalogItems.AnyAsync(
            item => item.UserId == userId
                && item.Kind == kind
                && item.Title == title
                && (excludeId == null || item.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(JobCatalogItem item, CancellationToken cancellationToken)
    {
        await _db.JobCatalogItems.AddAsync(item, cancellationToken);
    }

    public void Remove(JobCatalogItem item)
    {
        _db.JobCatalogItems.Remove(item);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
