using Flexis.Application.Google;
using Flexis.Domain.Google;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.Google;

internal sealed class GoogleConnectionRepository : IGoogleConnectionRepository
{
    private readonly FlexisDbContext _db;

    public GoogleConnectionRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<GoogleConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _db.GoogleConnections.FirstOrDefaultAsync(
            connection => connection.UserId == userId,
            cancellationToken);
    }

    public async Task AddAsync(GoogleConnection connection, CancellationToken cancellationToken)
    {
        await _db.GoogleConnections.AddAsync(connection, cancellationToken);
    }

    public void Remove(GoogleConnection connection)
    {
        _db.GoogleConnections.Remove(connection);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
