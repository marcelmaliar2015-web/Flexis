using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailConnectionRepository
{
    Task<MailConnection?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(MailConnection connection, CancellationToken cancellationToken);

    void Remove(MailConnection connection);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
