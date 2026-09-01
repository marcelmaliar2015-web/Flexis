using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailConnectionRepository : IMailConnectionRepository
{
    private readonly FlexisDbContext _db;

    public MailConnectionRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MailConnection>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.MailConnections
            .Where(connection => connection.UserId == userId)
            .OrderBy(connection => connection.ConnectedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<MailConnection?> GetByIdForUserAsync(
        Guid userId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        return _db.MailConnections.FirstOrDefaultAsync(
            connection => connection.UserId == userId && connection.Id == connectionId,
            cancellationToken);
    }

    public Task<MailConnection?> GetByUserProviderSubjectAsync(
        Guid userId,
        MailProvider provider,
        string externalSubject,
        CancellationToken cancellationToken)
    {
        return _db.MailConnections.FirstOrDefaultAsync(
            connection => connection.UserId == userId
                && connection.Provider == provider
                && connection.ExternalSubject == externalSubject,
            cancellationToken);
    }

    public async Task AddAsync(MailConnection connection, CancellationToken cancellationToken)
    {
        await _db.MailConnections.AddAsync(connection, cancellationToken);
    }

    public void Remove(MailConnection connection)
    {
        _db.MailConnections.Remove(connection);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
