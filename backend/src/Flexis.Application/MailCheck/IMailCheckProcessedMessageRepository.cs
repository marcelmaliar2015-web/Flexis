using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailCheckProcessedMessageRepository
{
    Task<IReadOnlySet<string>> FindExistingAsync(
        Guid mailConnectionId,
        IReadOnlyCollection<string> messageIds,
        CancellationToken cancellationToken);

    Task AddAsync(MailCheckProcessedMessage message, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
