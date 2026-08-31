namespace Flexis.Application.MailCheck;

public static class MailGmailScopes
{
    public const string OpenId = "openid";
    public const string Email = "https://www.googleapis.com/auth/userinfo.email";
    public const string GmailModify = "https://www.googleapis.com/auth/gmail.modify";

    public static readonly string Request = string.Join(' ', OpenId, Email, GmailModify);

    public static IReadOnlyList<string> Capabilities { get; } =
    [
        "Gmail: read, label, star, move, and trash mail for Mail Check.",
    ];
}
