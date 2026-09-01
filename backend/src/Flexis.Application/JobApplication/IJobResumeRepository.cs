using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobResumeRepository
{
    Task<JobResumeSettings?> GetSettingsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddSettingsAsync(JobResumeSettings settings, CancellationToken cancellationToken);

    Task<JobProfileResumeSettings?> GetProfileSettingsAsync(Guid profileId, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobProfileResumeSettings>> ListProfileSettingsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddProfileSettingsAsync(JobProfileResumeSettings settings, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
