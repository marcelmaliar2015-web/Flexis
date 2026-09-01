using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailConnectionRepository
{
    Task<IReadOnlyList<MailConnection>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<MailConnection?> GetByIdForUserAsync(Guid userId, Guid connectionId, CancellationToken cancellationToken);

    Task<MailConnection?> GetByUserProviderSubjectAsync(
        Guid userId,
        MailProvider provider,
        string externalSubject,
        CancellationToken cancellationToken);

    Task AddAsync(MailConnection connection, CancellationToken cancellationToken);

    void Remove(MailConnection connection);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
