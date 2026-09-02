using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckScanStateRepository : IMailCheckScanStateRepository
{
    private readonly FlexisDbContext _db;

    public MailCheckScanStateRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<Guid, MailCheckScanState>> ListByConnectionIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> connectionIds,
        CancellationToken cancellationToken)
    {
        if (connectionIds.Count == 0)
        {
            return new Dictionary<Guid, MailCheckScanState>();
        }

        var listed = await _db.MailCheckScanStates
            .Where(item => item.UserId == userId && connectionIds.Contains(item.MailConnectionId))
            .ToListAsync(cancellationToken);
        return listed.ToDictionary(item => item.MailConnectionId);
    }

    public async Task<MailCheckScanState> GetOrCreateAsync(
        Guid userId,
        Guid mailConnectionId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.MailCheckScanStates.FirstOrDefaultAsync(
            item => item.UserId == userId && item.MailConnectionId == mailConnectionId,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = MailCheckScanState.Create(userId, mailConnectionId);
        await _db.MailCheckScanStates.AddAsync(created, cancellationToken);
        return created;
    }

    public async Task ResetAsync(Guid userId, Guid? mailConnectionId, CancellationToken cancellationToken)
    {
        var query = _db.MailCheckScanStates.Where(item => item.UserId == userId);
        if (mailConnectionId is Guid id)
        {
            query = query.Where(item => item.MailConnectionId == id);
        }

        var listed = await query.ToListAsync(cancellationToken);
        foreach (var item in listed)
        {
            item.Reset();
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
