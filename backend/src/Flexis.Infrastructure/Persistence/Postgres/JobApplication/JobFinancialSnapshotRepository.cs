using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobFinancialSnapshotRepository : IJobFinancialSnapshotRepository
{
    private readonly FlexisDbContext _db;

    public JobFinancialSnapshotRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<JobFinancialSnapshot?> GetByUserAndHourAsync(
        Guid userId,
        DateTimeOffset capturedHour,
        CancellationToken cancellationToken)
    {
        var hour = JobFinancialSnapshot.TruncateToHour(capturedHour);
        return _db.JobFinancialSnapshots.FirstOrDefaultAsync(
            item => item.UserId == userId && item.CapturedHour == hour,
            cancellationToken);
    }

    public async Task<IReadOnlyList<JobFinancialSnapshot>> ListRecentAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken)
    {
        return await _db.JobFinancialSnapshots
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CapturedHour)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobFinancialSnapshot snapshot, CancellationToken cancellationToken)
    {
        await _db.JobFinancialSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
