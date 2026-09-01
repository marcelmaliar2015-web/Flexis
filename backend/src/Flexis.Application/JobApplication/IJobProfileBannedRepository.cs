using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobProfileBannedRepository
{
    Task<IReadOnlyList<JobProfileBannedCompany>> ListByProfileIdAsync(
        Guid profileId,
        CancellationToken cancellationToken);

    Task<JobProfileBannedCompany?> GetByIdAsync(
        Guid profileId,
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> MatchKeyExistsAsync(
        Guid profileId,
        string matchKey,
        Guid? excludeId,
        CancellationToken cancellationToken);

    Task AddAsync(JobProfileBannedCompany company, CancellationToken cancellationToken);

    void Remove(JobProfileBannedCompany company);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
