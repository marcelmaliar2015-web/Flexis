using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobApplicationLogRepository : IJobApplicationLogRepository
{
    private readonly FlexisDbContext _db;

    public JobApplicationLogRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<JobApplicationLog> Items, int TotalCount)> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        string? category,
        string? query,
        CancellationToken cancellationToken)
    {
        var filtered = _db.JobApplicationLogs.AsNoTracking().Where(item => item.UserId == userId);
        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(item => item.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var needle = query.Trim();
            filtered = filtered.Where(item =>
                EF.Functions.ILike(item.Summary, $"%{needle}%")
                || EF.Functions.ILike(item.Detail, $"%{needle}%")
                || EF.Functions.ILike(item.Action, $"%{needle}%")
                || EF.Functions.ILike(item.Category, $"%{needle}%"));
        }

        var totalCount = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task AddAsync(JobApplicationLog log, CancellationToken cancellationToken)
    {
        await _db.JobApplicationLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
