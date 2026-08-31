using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckProcessedMessageRepository : IMailCheckProcessedMessageRepository
{
    private readonly FlexisDbContext _db;

    public MailCheckProcessedMessageRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlySet<string>> FindExistingAsync(
        Guid userId,
        IReadOnlyCollection<string> gmailMessageIds,
        CancellationToken cancellationToken)
    {
        if (gmailMessageIds.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var found = await _db.MailCheckProcessedMessages
            .Where(item => item.UserId == userId && gmailMessageIds.Contains(item.GmailMessageId))
            .Select(item => item.GmailMessageId)
            .ToListAsync(cancellationToken);
        return found.ToHashSet(StringComparer.Ordinal);
    }

    public async Task AddAsync(MailCheckProcessedMessage message, CancellationToken cancellationToken)
    {
        await _db.MailCheckProcessedMessages.AddAsync(message, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
