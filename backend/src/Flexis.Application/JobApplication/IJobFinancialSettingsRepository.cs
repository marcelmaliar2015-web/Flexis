using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public interface IJobFinancialSettingsRepository
{
    Task<JobFinancialSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(JobFinancialSettings settings, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
