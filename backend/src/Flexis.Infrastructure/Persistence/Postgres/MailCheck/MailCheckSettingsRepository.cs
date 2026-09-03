using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckSettingsRepository : IMailCheckSettingsRepository
{
    private readonly FlexisDbContext _db;

    public MailCheckSettingsRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<MailCheckSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _db.MailCheckSettings.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListAutoCheckEligibleUserIdsAsync(CancellationToken cancellationToken)
    {
        return await _db.MailCheckSettings
            .AsNoTracking()
            .Where(item => item.AutoCheckEnabled)
            .Where(item => item.ApiKeyProtected != null && item.ApiKeyProtected != string.Empty)
            .Where(item => _db.MailConnections.Any(connection => connection.UserId == item.UserId))
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task ReloadAsync(MailCheckSettings settings, CancellationToken cancellationToken)
    {
        return _db.Entry(settings).ReloadAsync(cancellationToken);
    }

    public async Task AddAsync(MailCheckSettings settings, CancellationToken cancellationToken)
    {
        await _db.MailCheckSettings.AddAsync(settings, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
