using Flexis.Application.Users;
using Flexis.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.Users;

internal sealed class UserRepository : IUserRepository
{
    private readonly FlexisDbContext _db;

    public UserRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _db.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Users
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken)
    {
        return _db.Users.CountAsync(
            user => user.Role == UserRole.Admin && user.IsActive,
            cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _db.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
