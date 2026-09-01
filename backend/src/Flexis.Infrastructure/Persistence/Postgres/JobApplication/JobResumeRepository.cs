using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobResumeRepository : IJobResumeRepository
{
    private readonly FlexisDbContext _db;

    public JobResumeRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<JobResumeSettings?> GetSettingsByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _db.JobResumeSettings.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
    }

    public async Task AddSettingsAsync(JobResumeSettings settings, CancellationToken cancellationToken)
    {
        await _db.JobResumeSettings.AddAsync(settings, cancellationToken);
    }

    public Task<JobProfileResumeSettings?> GetProfileSettingsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return _db.JobProfileResumeSettings.FirstOrDefaultAsync(item => item.ProfileId == profileId, cancellationToken);
    }

    public async Task<IReadOnlyList<JobProfileResumeSettings>> ListProfileSettingsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.JobProfileResumeSettings
            .Where(item => _db.JobCatalogItems.Any(catalog => catalog.Id == item.ProfileId && catalog.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddProfileSettingsAsync(JobProfileResumeSettings settings, CancellationToken cancellationToken)
    {
        await _db.JobProfileResumeSettings.AddAsync(settings, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
