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

    public async Task<IReadOnlyList<JobApplicationLog>> ListAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken)
    {
        return await _db.JobApplicationLogs
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.OccurredAt)
            .Take(take)
            .ToListAsync(cancellationToken);
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
