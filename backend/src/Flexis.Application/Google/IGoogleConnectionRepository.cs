using Flexis.Domain.Google;

namespace Flexis.Application.Google;

public interface IGoogleConnectionRepository
{
    Task<GoogleConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(GoogleConnection connection, CancellationToken cancellationToken);

    void Remove(GoogleConnection connection);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
