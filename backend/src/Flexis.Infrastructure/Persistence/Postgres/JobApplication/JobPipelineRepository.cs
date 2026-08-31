using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobPipelineRepository : IJobPipelineRepository
{
    private readonly FlexisDbContext _db;

    public JobPipelineRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JobPipelineEntry>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.JobPipelineEntries
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<JobPipelineEntry?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        return _db.JobPipelineEntries.FirstOrDefaultAsync(
            entry => entry.UserId == userId && entry.Id == id,
            cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid userId,
        Guid profileId,
        Guid sourceId,
        int locationSheetId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _db.JobPipelineEntries.AnyAsync(
            entry => entry.UserId == userId
                && entry.ProfileId == profileId
                && entry.SourceId == sourceId
                && entry.LocationSheetId == locationSheetId
                && (excludeId == null || entry.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(JobPipelineEntry entry, CancellationToken cancellationToken)
    {
        await _db.JobPipelineEntries.AddAsync(entry, cancellationToken);
    }

    public void Remove(JobPipelineEntry entry)
    {
        _db.JobPipelineEntries.Remove(entry);
    }

    public async Task<IReadOnlyList<JobPipelineBannedCompany>> ListBannedAsync(
        Guid pipelineEntryId,
        CancellationToken cancellationToken)
    {
        return await _db.JobPipelineBannedCompanies
            .Where(item => item.PipelineEntryId == pipelineEntryId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<JobPipelineBannedCompany?> GetBannedAsync(
        Guid pipelineEntryId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return _db.JobPipelineBannedCompanies.FirstOrDefaultAsync(
            item => item.PipelineEntryId == pipelineEntryId && item.Id == id,
            cancellationToken);
    }

    public Task<bool> BannedMatchKeyExistsAsync(
        Guid pipelineEntryId,
        string matchKey,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _db.JobPipelineBannedCompanies.AnyAsync(
            item => item.PipelineEntryId == pipelineEntryId
                && item.MatchKey == matchKey
                && (excludeId == null || item.Id != excludeId),
            cancellationToken);
    }

    public async Task AddBannedAsync(JobPipelineBannedCompany company, CancellationToken cancellationToken)
    {
        await _db.JobPipelineBannedCompanies.AddAsync(company, cancellationToken);
    }

    public void RemoveBanned(JobPipelineBannedCompany company)
    {
        _db.JobPipelineBannedCompanies.Remove(company);
    }

    public async Task RemoveByCatalogItemIdAsync(Guid userId, Guid catalogItemId, CancellationToken cancellationToken)
    {
        var entries = await _db.JobPipelineEntries
            .Where(entry => entry.UserId == userId
                && (entry.ProfileId == catalogItemId || entry.SourceId == catalogItemId))
            .ToListAsync(cancellationToken);
        _db.JobPipelineEntries.RemoveRange(entries);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
