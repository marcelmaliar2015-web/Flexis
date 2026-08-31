using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobApplicationLogRepository
{
    Task<IReadOnlyList<JobApplicationLog>> ListAsync(Guid userId, int take, CancellationToken cancellationToken);

    Task AddAsync(JobApplicationLog log, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
