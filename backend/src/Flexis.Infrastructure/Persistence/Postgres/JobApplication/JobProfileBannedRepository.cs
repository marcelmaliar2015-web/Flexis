using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobProfileBannedRepository : IJobProfileBannedRepository
{
    private readonly FlexisDbContext _db;

    public JobProfileBannedRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<JobProfileBannedCompany>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return await _db.JobProfileBannedCompanies
            .Where(item => item.ProfileId == profileId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<JobProfileBannedCompany?> GetByIdAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken)
    {
        return _db.JobProfileBannedCompanies.FirstOrDefaultAsync(
            item => item.ProfileId == profileId && item.Id == id,
            cancellationToken);
    }

    public Task<bool> MatchKeyExistsAsync(
        Guid profileId,
        string matchKey,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return _db.JobProfileBannedCompanies.AnyAsync(
            item => item.ProfileId == profileId
                && item.MatchKey == matchKey
                && (excludeId == null || item.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(JobProfileBannedCompany company, CancellationToken cancellationToken)
    {
        await _db.JobProfileBannedCompanies.AddAsync(company, cancellationToken);
    }

    public void Remove(JobProfileBannedCompany company)
    {
        _db.JobProfileBannedCompanies.Remove(company);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
