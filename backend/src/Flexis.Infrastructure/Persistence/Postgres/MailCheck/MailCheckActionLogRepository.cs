using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckActionLogRepository : IMailCheckActionLogRepository
{
    private readonly FlexisDbContext _db;

    public MailCheckActionLogRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(MailCheckActionLog log, CancellationToken cancellationToken)
    {
        await _db.MailCheckActionLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(IReadOnlyList<MailCheckActionLog> Items, int TotalCount)> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        string? source,
        string? action,
        Guid? mailboxId,
        string? query,
        CancellationToken cancellationToken)
    {
        var filtered = _db.MailCheckActionLogs.AsNoTracking().Where(item => item.UserId == userId);
        if (!string.IsNullOrWhiteSpace(source))
        {
            filtered = filtered.Where(item => item.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            filtered = filtered.Where(item => item.Action == action);
        }

        if (mailboxId is Guid id)
        {
            filtered = filtered.Where(item => item.MailConnectionId == id);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var needle = query.Trim();
            filtered = filtered.Where(item =>
                EF.Functions.ILike(item.Subject, $"%{needle}%")
                || EF.Functions.ILike(item.FromAddress, $"%{needle}%")
                || EF.Functions.ILike(item.MailboxEmail, $"%{needle}%")
                || EF.Functions.ILike(item.Detail, $"%{needle}%")
                || EF.Functions.ILike(item.Label, $"%{needle}%")
                || EF.Functions.ILike(item.Action, $"%{needle}%")
                || EF.Functions.ILike(item.MessageId, $"%{needle}%"));
        }

        var totalCount = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
