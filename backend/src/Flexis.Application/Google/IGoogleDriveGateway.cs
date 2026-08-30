namespace Flexis.Application.Google;

public static class FlexisDriveLayout
{
    public const string RootName = "Flexis";

    public const string WorkspaceName = "Job Application";

    public const string ProfilesName = "Profiles";

    public const string SourcesName = "Sources";

    public const string RootDescription = "Flexis workspace. Created automatically for files this app stores in Drive.";

    public const string WorkspaceDescription = "Spreadsheets Flexis creates for Job Application.";

    public const string ProfilesDescription = "One Google Sheet per profile.";

    public const string SourcesDescription = "One Google Sheet per source. Location tabs stay inside each workbook.";
}

public sealed record FlexisDriveFolders(
    string RootFolderId,
    string WorkspaceFolderId,
    string ProfilesFolderId,
    string SourcesFolderId);

public interface IGoogleDriveGateway
{
    Task<bool> FolderIsActiveAsync(string accessToken, string folderId, CancellationToken cancellationToken);

    Task<string> CreateFolderAsync(
        string accessToken,
        string name,
        string? parentFolderId,
        string description,
        CancellationToken cancellationToken);

    Task<string?> FindFolderAsync(
        string accessToken,
        string name,
        string? parentFolderId,
        CancellationToken cancellationToken);

    Task MoveFileToFolderAsync(
        string accessToken,
        string fileId,
        string folderId,
        CancellationToken cancellationToken);
}
