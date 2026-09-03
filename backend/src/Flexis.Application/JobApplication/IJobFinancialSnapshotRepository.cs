using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobFinancialSnapshotRepository
{
    Task<JobFinancialSnapshot?> GetByUserAndHourAsync(
        Guid userId,
        DateTimeOffset capturedHour,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<JobFinancialSnapshot>> ListRecentAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken);

    Task AddAsync(JobFinancialSnapshot snapshot, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
