using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailCheckActionLogRepository
{
    Task AddAsync(MailCheckActionLog log, CancellationToken cancellationToken);

    Task<(IReadOnlyList<MailCheckActionLog> Items, int TotalCount)> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        string? source,
        string? action,
        Guid? mailboxId,
        string? query,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
