using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailCheckSettingsRepository
{
    Task<MailCheckSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task ReloadAsync(MailCheckSettings settings, CancellationToken cancellationToken);

    Task AddAsync(MailCheckSettings settings, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
