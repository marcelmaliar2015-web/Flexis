using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailCheckProcessedMessageRepository
{
    Task<IReadOnlySet<string>> FindExistingAsync(
        Guid userId,
        IReadOnlyCollection<string> gmailMessageIds,
        CancellationToken cancellationToken);

    Task AddAsync(MailCheckProcessedMessage message, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
