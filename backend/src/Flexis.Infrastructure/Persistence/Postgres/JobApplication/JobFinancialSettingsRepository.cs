using Flexis.Application.JobApplication;
using Flexis.Domain.JobApplication;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.JobApplication;

internal sealed class JobFinancialSettingsRepository : IJobFinancialSettingsRepository
{
    private readonly FlexisDbContext _db;

    public JobFinancialSettingsRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<JobFinancialSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _db.JobFinancialSettings.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(JobFinancialSettings settings, CancellationToken cancellationToken)
    {
        await _db.JobFinancialSettings.AddAsync(settings, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
