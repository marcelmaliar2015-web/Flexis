using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailCheckScanStateRepository
{
    Task<IReadOnlyDictionary<Guid, MailCheckScanState>> ListByConnectionIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> connectionIds,
        CancellationToken cancellationToken);

    Task<MailCheckScanState> GetOrCreateAsync(
        Guid userId,
        Guid mailConnectionId,
        CancellationToken cancellationToken);

    Task ResetAsync(Guid userId, Guid? mailConnectionId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
