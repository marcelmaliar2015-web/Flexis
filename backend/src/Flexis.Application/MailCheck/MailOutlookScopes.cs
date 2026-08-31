namespace Flexis.Application.MailCheck;

public static class MailOutlookScopes
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string OfflineAccess = "offline_access";
    public const string MailReadWrite = "Mail.ReadWrite";
    public const string MailboxSettingsReadWrite = "MailboxSettings.ReadWrite";

    public static readonly string Request = string.Join(
        ' ',
        OpenId,
        Profile,
        Email,
        OfflineAccess,
        MailReadWrite,
        MailboxSettingsReadWrite);
}
