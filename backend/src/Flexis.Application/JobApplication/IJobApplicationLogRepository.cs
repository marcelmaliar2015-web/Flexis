using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobApplicationLogRepository
{
    Task<(IReadOnlyList<JobApplicationLog> Items, int TotalCount)> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        string? category,
        string? query,
        CancellationToken cancellationToken);

    Task AddAsync(JobApplicationLog log, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
