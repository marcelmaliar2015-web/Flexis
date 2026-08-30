using Flexis.Application.Common;
using Flexis.Application.JobApplication;
using Flexis.Domain.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.Google;

public sealed class GoogleDriveLayoutService
{
    private readonly IGoogleConnectionRepository _connections;
    private readonly IGoogleDriveGateway _drive;
    private readonly IJobCatalogRepository _items;

    public GoogleDriveLayoutService(
        IGoogleConnectionRepository connections,
        IGoogleDriveGateway drive,
        IJobCatalogRepository items)
    {
        _connections = connections;
        _drive = drive;
        _items = items;
    }

    public async Task<FlexisDriveFolders> EnsureAsync(
        Guid userId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ValidationFailedException("Connect Gmail first.");
        var folders = await BuildFoldersAsync(accessToken, connection, cancellationToken);
        if (!connection.HasDriveLayout
            || connection.DriveRootFolderId != folders.RootFolderId
            || connection.DriveWorkspaceFolderId != folders.WorkspaceFolderId
            || connection.DriveProfilesFolderId != folders.ProfilesFolderId
            || connection.DriveSourcesFolderId != folders.SourcesFolderId)
        {
            connection.SetDriveLayout(
                folders.RootFolderId,
                folders.WorkspaceFolderId,
                folders.ProfilesFolderId,
                folders.SourcesFolderId);
            await _connections.SaveChangesAsync(cancellationToken);
        }

        await PlaceCatalogAsync(userId, accessToken, folders, cancellationToken);
        return folders;
    }

    public Task PlaceWorkbookAsync(
        string accessToken,
        string spreadsheetId,
        JobCatalogKind kind,
        FlexisDriveFolders folders,
        CancellationToken cancellationToken)
    {
        var folderId = kind == JobCatalogKind.Profile ? folders.ProfilesFolderId : folders.SourcesFolderId;
        return _drive.MoveFileToFolderAsync(accessToken, spreadsheetId, folderId, cancellationToken, required: true);
    }

    private async Task<FlexisDriveFolders> BuildFoldersAsync(
        string accessToken,
        GoogleConnection connection,
        CancellationToken cancellationToken)
    {
        var rootId = await RequireFolderAsync(
            accessToken,
            connection.DriveRootFolderId,
            FlexisDriveLayout.RootName,
            null,
            FlexisDriveLayout.RootDescription,
            cancellationToken);
        var workspaceId = await RequireFolderAsync(
            accessToken,
            connection.DriveWorkspaceFolderId,
            FlexisDriveLayout.WorkspaceName,
            rootId,
            FlexisDriveLayout.WorkspaceDescription,
            cancellationToken);
        var profilesId = await RequireFolderAsync(
            accessToken,
            connection.DriveProfilesFolderId,
            FlexisDriveLayout.ProfilesName,
            workspaceId,
            FlexisDriveLayout.ProfilesDescription,
            cancellationToken);
        var sourcesId = await RequireFolderAsync(
            accessToken,
            connection.DriveSourcesFolderId,
            FlexisDriveLayout.SourcesName,
            workspaceId,
            FlexisDriveLayout.SourcesDescription,
            cancellationToken);
        return new FlexisDriveFolders(rootId, workspaceId, profilesId, sourcesId);
    }

    private async Task<string> RequireFolderAsync(
        string accessToken,
        string? folderId,
        string name,
        string? parentFolderId,
        string description,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(folderId)
            && await _drive.FolderIsActiveAsync(accessToken, folderId, cancellationToken))
        {
            return folderId;
        }

        var existing = await _drive.FindFolderAsync(accessToken, name, parentFolderId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        return await _drive.CreateFolderAsync(accessToken, name, parentFolderId, description, cancellationToken);
    }

    private async Task PlaceCatalogAsync(
        Guid userId,
        string accessToken,
        FlexisDriveFolders folders,
        CancellationToken cancellationToken)
    {
        var profiles = await _items.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        foreach (var item in profiles)
        {
            if (string.IsNullOrWhiteSpace(item.SpreadsheetId))
            {
                continue;
            }

            await _drive.MoveFileToFolderAsync(accessToken, item.SpreadsheetId, folders.ProfilesFolderId, cancellationToken);
        }

        var sources = await _items.ListAsync(userId, JobCatalogKind.Source, cancellationToken);
        foreach (var item in sources)
        {
            if (string.IsNullOrWhiteSpace(item.SpreadsheetId))
            {
                continue;
            }

            await _drive.MoveFileToFolderAsync(accessToken, item.SpreadsheetId, folders.SourcesFolderId, cancellationToken);
        }
    }
}
