using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public interface IMailMailboxGateway
{
    IMailMailbox Resolve(MailProvider provider);
}
