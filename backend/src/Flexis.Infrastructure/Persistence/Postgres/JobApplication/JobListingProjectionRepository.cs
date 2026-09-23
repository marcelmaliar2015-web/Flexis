using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobListingProjectionRepository : IJobListingProjectionRepository
{
    private readonly FlexisDbContext _db;

    public JobListingProjectionRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JobListingProjection>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.JobListingProjections
            .Where(item => item.UserId == userId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobListingProjection>> ListProfileMainAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await _db.JobListingProjections
            .Where(item =>
                item.UserId == userId
                && item.ProfileId == profileId
                && item.Source == JobListingProjectionSources.ProfileMain)
            .OrderBy(item => item.RowNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobListingProjection>> ListProfileArchivesAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await _db.JobListingProjections
            .Where(item =>
                item.UserId == userId
                && item.ProfileId == profileId
                && item.Source == JobListingProjectionSources.ProfileArchive)
            .OrderBy(item => item.ArchiveTab)
            .ThenBy(item => item.RowNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobListingProjection>> ListSearchBaseAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.JobListingProjections
            .Where(item =>
                item.UserId == userId
                && item.Source == JobListingProjectionSources.SearchBase)
            .OrderBy(item => item.RowNumber)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasProfileMainAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        return _db.JobListingProjections.AnyAsync(
            item =>
                item.UserId == userId
                && item.ProfileId == profileId
                && item.Source == JobListingProjectionSources.ProfileMain,
            cancellationToken);
    }

    public async Task ReplaceScopeAsync(
        Guid userId,
        string source,
        Guid? profileId,
        string archiveTab,
        IReadOnlyList<JobListingProjection> rows,
        CancellationToken cancellationToken)
    {
        var tab = archiveTab ?? string.Empty;
        var existing = await _db.JobListingProjections
            .Where(item =>
                item.UserId == userId
                && item.Source == source
                && item.ProfileId == profileId
                && item.ArchiveTab == tab)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.JobListingProjections.RemoveRange(existing);
        }

        if (rows.Count > 0)
        {
            await _db.JobListingProjections.AddRangeAsync(rows, cancellationToken);
        }
    }

    public async Task MoveProfileMainToArchiveAsync(
        Guid userId,
        Guid profileId,
        string archiveTab,
        CancellationToken cancellationToken)
    {
        var main = await _db.JobListingProjections
            .Where(item =>
                item.UserId == userId
                && item.ProfileId == profileId
                && item.Source == JobListingProjectionSources.ProfileMain)
            .ToListAsync(cancellationToken);
        if (main.Count == 0)
        {
            return;
        }

        var archived = main
            .Select(item => JobListingProjection.Create(
                item.UserId,
                item.ProfileId,
                JobListingProjectionSources.ProfileArchive,
                archiveTab,
                item.ListingKey,
                item.CompanyName,
                item.Position,
                item.Link,
                item.Jd,
                item.Download,
                item.Status,
                item.Issue,
                item.ProfileName,
                item.RowNumber,
                item.ContentHash))
            .ToList();
        _db.JobListingProjections.RemoveRange(main);
        await _db.JobListingProjections.AddRangeAsync(archived, cancellationToken);
    }

    public async Task ClearProfileMainAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        var main = await _db.JobListingProjections
            .Where(item =>
                item.UserId == userId
                && item.ProfileId == profileId
                && item.Source == JobListingProjectionSources.ProfileMain)
            .ToListAsync(cancellationToken);
        if (main.Count > 0)
        {
            _db.JobListingProjections.RemoveRange(main);
        }
    }

    public async Task UpdateStatusesAsync(
        Guid userId,
        Guid profileId,
        IReadOnlyDictionary<string, string> statusByListingKey,
        CancellationToken cancellationToken)
    {
        if (statusByListingKey.Count == 0)
        {
            return;
        }

        var keys = statusByListingKey.Keys.ToList();
        var rows = await _db.JobListingProjections
            .Where(item =>
                item.UserId == userId
                && item.ProfileId == profileId
                && item.Source == JobListingProjectionSources.ProfileMain
                && keys.Contains(item.ListingKey))
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            if (statusByListingKey.TryGetValue(row.ListingKey, out var status))
            {
                row.SetStatus(status);
            }
        }
    }

    public async Task DeleteByProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        var rows = await _db.JobListingProjections
            .Where(item => item.UserId == userId && item.ProfileId == profileId)
            .ToListAsync(cancellationToken);
        if (rows.Count > 0)
        {
            _db.JobListingProjections.RemoveRange(rows);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class JobSheetSyncStateRepository : IJobSheetSyncStateRepository
{
    private readonly FlexisDbContext _db;

    public JobSheetSyncStateRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<JobSheetSyncState?> GetAsync(
        Guid userId,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        return _db.JobSheetSyncStates
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.SpreadsheetId == spreadsheetId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<JobSheetSyncState>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.JobSheetSyncStates
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobSheetSyncState state, CancellationToken cancellationToken)
    {
        await _db.JobSheetSyncStates.AddAsync(state, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
