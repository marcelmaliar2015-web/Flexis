namespace Flexis.Application.MailCheck;

public static class MailCheckMessageState
{
    public static bool IsTrashed(MailMessageContent message)
    {
        return message.LabelIds.Contains("TRASH", StringComparer.OrdinalIgnoreCase);
    }
}
