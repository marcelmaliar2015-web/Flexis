using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobProfileStatisticsSnapshotRepository : IJobProfileStatisticsSnapshotRepository
{
    private readonly FlexisDbContext _db;

    public JobProfileStatisticsSnapshotRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<JobProfileStatisticsSnapshot?> GetByUserProfileAndHourAsync(
        Guid userId,
        Guid profileId,
        DateTimeOffset capturedHour,
        CancellationToken cancellationToken)
    {
        var hour = JobFinancialSnapshot.TruncateToHour(capturedHour);
        return _db.JobProfileStatisticsSnapshots.FirstOrDefaultAsync(
            item => item.UserId == userId && item.ProfileId == profileId && item.CapturedHour == hour,
            cancellationToken);
    }

    public async Task<IReadOnlyList<JobProfileStatisticsSnapshot>> ListRecentAsync(
        Guid userId,
        int takeHours,
        CancellationToken cancellationToken)
    {
        var since = JobFinancialSnapshot.TruncateToHour(DateTimeOffset.UtcNow).AddHours(-(takeHours - 1));
        return await _db.JobProfileStatisticsSnapshots
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.CapturedHour >= since)
            .OrderBy(item => item.CapturedHour)
            .ThenBy(item => item.ProfileTitle)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobProfileStatisticsSnapshot snapshot, CancellationToken cancellationToken)
    {
        await _db.JobProfileStatisticsSnapshots.AddAsync(snapshot, cancellationToken);
    }

    public async Task UpsertHourAsync(JobProfileStatisticsSnapshot snapshot, CancellationToken cancellationToken)
    {
        var existing = await GetByUserProfileAndHourAsync(
            snapshot.UserId,
            snapshot.ProfileId,
            snapshot.CapturedHour,
            cancellationToken);
        if (existing is null)
        {
            await _db.JobProfileStatisticsSnapshots.AddAsync(snapshot, cancellationToken);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (PostgresUniqueConstraint.IsViolation(exception))
            {
                _db.Entry(snapshot).State = EntityState.Detached;
                existing = await GetByUserProfileAndHourAsync(
                    snapshot.UserId,
                    snapshot.ProfileId,
                    snapshot.CapturedHour,
                    cancellationToken);
                if (existing is null)
                {
                    throw new InvalidOperationException(
                        "Profile statistics snapshot was not found after a unique constraint conflict.");
                }
            }
        }

        existing.Replace(
            snapshot.ProfileTitle,
            snapshot.Applied,
            snapshot.Interviews,
            snapshot.Unapplied,
            snapshot.Total,
            snapshot.Price);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
