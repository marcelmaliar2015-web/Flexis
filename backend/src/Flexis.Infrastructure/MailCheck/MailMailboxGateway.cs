using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;
using Flexis.Infrastructure.Google;
using Flexis.Infrastructure.Microsoft;

namespace Flexis.Infrastructure.MailCheck;

internal sealed class MailMailboxGateway : IMailMailboxGateway
{
    private readonly GmailMailboxClient _gmail;
    private readonly OutlookMailboxClient _outlook;

    public MailMailboxGateway(GmailMailboxClient gmail, OutlookMailboxClient outlook)
    {
        _gmail = gmail;
        _outlook = outlook;
    }

    public IMailMailbox Resolve(MailProvider provider)
    {
        return provider switch
        {
            MailProvider.Gmail => _gmail,
            MailProvider.Outlook => _outlook,
            _ => throw new InvalidOperationException("Mail provider is not supported."),
        };
    }
}
