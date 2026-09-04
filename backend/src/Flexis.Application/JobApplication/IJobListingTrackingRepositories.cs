using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobListingCopyRepository
{
    Task ReplaceBatchAsync(JobListingCopyBatch batch, CancellationToken cancellationToken);

    Task<JobListingCopyBatch?> GetLatestByProfileAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IJobListingStatusRepository
{
    Task<IReadOnlyList<JobListingStatusState>> ListStatesAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken);

    Task AddStateAsync(JobListingStatusState state, CancellationToken cancellationToken);

    Task AddEventAsync(JobListingStatusEvent statusEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<JobListingStatusEvent>> ListEventsSinceAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
