using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobProfileStatisticsSnapshotRepository
{
    Task<JobProfileStatisticsSnapshot?> GetByUserProfileAndHourAsync(
        Guid userId,
        Guid profileId,
        DateTimeOffset capturedHour,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobProfileStatisticsSnapshot>> ListRecentAsync(
        Guid userId,
        int takeHours,
        CancellationToken cancellationToken);

    Task AddAsync(JobProfileStatisticsSnapshot snapshot, CancellationToken cancellationToken);

    Task UpsertHourAsync(JobProfileStatisticsSnapshot snapshot, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
