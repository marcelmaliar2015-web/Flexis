using Flexis.Application.Common;

namespace Flexis.Application.MailCheck;

public static class MailCheckMailboxErrors
{
    public static bool IsMissingMessage(Exception exception)
    {
        var message = exception switch
        {
            GoogleOAuthException google => google.Message,
            MicrosoftOAuthException microsoft => microsoft.Message,
            _ => exception.Message
        };

        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("could not be found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("resource not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ErrorItemNotFound", StringComparison.OrdinalIgnoreCase);
    }
}
