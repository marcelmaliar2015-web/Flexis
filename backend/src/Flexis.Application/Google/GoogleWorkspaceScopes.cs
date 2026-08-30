namespace Flexis.Application.Google;

public static class GoogleWorkspaceScopes
{
    public const string OpenId = "openid";
    public const string Email = "https://www.googleapis.com/auth/userinfo.email";
    public const string GmailModify = "https://www.googleapis.com/auth/gmail.modify";
    public const string Spreadsheets = "https://www.googleapis.com/auth/spreadsheets";
    public const string DriveFile = "https://www.googleapis.com/auth/drive.file";

    public static readonly string Request = string.Join(
        ' ',
        OpenId,
        Email,
        GmailModify,
        Spreadsheets,
        DriveFile);

    public static IReadOnlyList<string> Capabilities { get; } =
    [
        "Gmail: read, send, and organize job correspondence.",
        "Google Sheets: create, edit, and delete spreadsheets.",
        "Google Drive: Flexis / Job Application folders for files this app creates, not the rest of Drive."
    ];
}
