using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobListingCopyRepository : IJobListingCopyRepository
{
    private readonly FlexisDbContext _db;

    public JobListingCopyRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task ReplaceBatchAsync(JobListingCopyBatch batch, CancellationToken cancellationToken)
    {
        var existing = await _db.JobListingCopyBatches
            .Where(item => item.UserId == batch.UserId && item.ProfileId == batch.ProfileId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.JobListingCopyBatches.RemoveRange(existing);
        }

        await _db.JobListingCopyBatches.AddAsync(batch, cancellationToken);
    }

    public async Task<JobListingCopyBatch?> GetLatestByProfileAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await _db.JobListingCopyBatches
            .Include(item => item.Items)
            .Where(item => item.UserId == userId && item.ProfileId == profileId)
            .OrderByDescending(item => item.CopiedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class JobListingStatusRepository : IJobListingStatusRepository
{
    private readonly FlexisDbContext _db;

    public JobListingStatusRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JobListingStatusState>> ListStatesAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await _db.JobListingStatusStates
            .Where(item => item.UserId == userId && item.ProfileId == profileId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddStateAsync(JobListingStatusState state, CancellationToken cancellationToken)
    {
        await _db.JobListingStatusStates.AddAsync(state, cancellationToken);
    }

    public async Task AddEventAsync(JobListingStatusEvent statusEvent, CancellationToken cancellationToken)
    {
        await _db.JobListingStatusEvents.AddAsync(statusEvent, cancellationToken);
    }

    public async Task<IReadOnlyList<JobListingStatusEvent>> ListEventsSinceAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        return await _db.JobListingStatusEvents
            .Where(item => item.UserId == userId && item.OccurredAt >= since)
            .OrderBy(item => item.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresUniqueConstraint.IsViolation(exception))
        {
            foreach (var entry in _db.ChangeTracker.Entries<JobListingStatusState>()
                .Where(item => item.State == EntityState.Added)
                .ToList())
            {
                var entity = entry.Entity;
                var exists = await _db.JobListingStatusStates
                    .AsNoTracking()
                    .AnyAsync(
                        item => item.UserId == entity.UserId
                            && item.ProfileId == entity.ProfileId
                            && item.ListingKey == entity.ListingKey,
                        cancellationToken);
                if (exists)
                {
                    entry.State = EntityState.Detached;
                }
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException retryException) when (PostgresUniqueConstraint.IsViolation(retryException))
            {
                foreach (var entry in _db.ChangeTracker.Entries<JobListingStatusState>()
                    .Where(item => item.State == EntityState.Added)
                    .ToList())
                {
                    entry.State = EntityState.Detached;
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
