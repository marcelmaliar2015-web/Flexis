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

    public Task<MailConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _db.MailConnections.FirstOrDefaultAsync(
            connection => connection.UserId == userId,
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
