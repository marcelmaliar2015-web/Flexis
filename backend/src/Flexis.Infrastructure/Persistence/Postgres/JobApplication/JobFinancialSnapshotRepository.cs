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

    public async Task UpsertHourAsync(JobFinancialSnapshot snapshot, CancellationToken cancellationToken)
    {
        var existing = await GetByUserAndHourAsync(
            snapshot.UserId,
            snapshot.CapturedHour,
            cancellationToken);
        if (existing is null)
        {
            await _db.JobFinancialSnapshots.AddAsync(snapshot, cancellationToken);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (PostgresUniqueConstraint.IsViolation(exception))
            {
                _db.Entry(snapshot).State = EntityState.Detached;
                existing = await GetByUserAndHourAsync(
                    snapshot.UserId,
                    snapshot.CapturedHour,
                    cancellationToken);
                if (existing is null)
                {
                    throw new InvalidOperationException(
                        "Financial snapshot was not found after a unique constraint conflict.");
                }
            }
        }

        existing.Replace(
            snapshot.TodayPrice,
            snapshot.TodayTotal,
            snapshot.TodayApplied,
            snapshot.TodayInterviews,
            snapshot.MainPrice,
            snapshot.MainTotal,
            snapshot.MainApplied,
            snapshot.MainInterviews,
            snapshot.ArchivedPrice,
            snapshot.ArchivedTotal,
            snapshot.ArchivedApplied,
            snapshot.ArchivedInterviews,
            snapshot.LifetimePrice,
            snapshot.LifetimeTotal,
            snapshot.LifetimeApplied,
            snapshot.LifetimeInterviews);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
