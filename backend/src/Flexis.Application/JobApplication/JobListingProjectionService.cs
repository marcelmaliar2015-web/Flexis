using System.Security.Cryptography;
using System.Text;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobListingProjectionService
{
    private readonly IJobListingProjectionRepository _projections;
    private readonly IJobSheetSyncStateRepository _syncStates;
    private readonly IJobCatalogRepository _catalog;
    private readonly GoogleAccessTokenService _tokens;
    private readonly GoogleDriveLayoutService _driveLayout;
    private readonly IGoogleDriveGateway _drive;
    private readonly IGoogleSheetsWorkspace _sheets;

    public JobListingProjectionService(
        IJobListingProjectionRepository projections,
        IJobSheetSyncStateRepository syncStates,
        IJobCatalogRepository catalog,
        GoogleAccessTokenService tokens,
        GoogleDriveLayoutService driveLayout,
        IGoogleDriveGateway drive,
        IGoogleSheetsWorkspace sheets)
    {
        _projections = projections;
        _syncStates = syncStates;
        _catalog = catalog;
        _tokens = tokens;
        _driveLayout = driveLayout;
        _drive = drive;
        _sheets = sheets;
    }

    public async Task ReplaceProfileMainFromRowsAsync(
        Guid userId,
        Guid profileId,
        string profileName,
        IReadOnlyList<JobListingSheetRow> rows,
        CancellationToken cancellationToken)
    {
        var projections = rows
            .Where(row => !row.Listing.IsEmpty)
            .GroupBy(row => JobFinancialRules.ListingKey(row.Listing), StringComparer.Ordinal)
            .Select(group =>
            {
                var row = group.Last();
                return FromListing(
                    userId,
                    profileId,
                    JobListingProjectionSources.ProfileMain,
                    string.Empty,
                    profileName,
                    row.RowNumber,
                    row.Listing,
                    issue: string.Empty);
            })
            .ToList();
        await _projections.ReplaceScopeAsync(
            userId,
            JobListingProjectionSources.ProfileMain,
            profileId,
            string.Empty,
            projections,
            cancellationToken);
        await _projections.SaveChangesAsync(cancellationToken);
        JobFinancialService.InvalidateBoardCache(userId);
    }

    public async Task ArchiveProfileMainAsync(
        Guid userId,
        Guid profileId,
        string archiveTab,
        CancellationToken cancellationToken)
    {
        await _projections.MoveProfileMainToArchiveAsync(userId, profileId, archiveTab, cancellationToken);
        await _projections.ClearProfileMainAsync(userId, profileId, cancellationToken);
        await _projections.SaveChangesAsync(cancellationToken);
        JobFinancialService.InvalidateBoardCache(userId);
    }

    public async Task UpdateProfileMainStatusesAsync(
        Guid userId,
        Guid profileId,
        IReadOnlyDictionary<string, string> statusByListingKey,
        CancellationToken cancellationToken)
    {
        await _projections.UpdateStatusesAsync(userId, profileId, statusByListingKey, cancellationToken);
        await _projections.SaveChangesAsync(cancellationToken);
        JobFinancialService.InvalidateBoardCache(userId);
    }

    public async Task DeleteProfileAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        await _projections.DeleteByProfileAsync(userId, profileId, cancellationToken);
        await _projections.SaveChangesAsync(cancellationToken);
        JobFinancialService.InvalidateBoardCache(userId);
    }

    public Task<IReadOnlyList<JobListingProjection>> ListProfileMainAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return _projections.ListProfileMainAsync(userId, profileId, cancellationToken);
    }

    public Task<IReadOnlyList<JobListingProjection>> ListProfileArchivesAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        return _projections.ListProfileArchivesAsync(userId, profileId, cancellationToken);
    }

    public Task<IReadOnlyList<JobListingProjection>> ListSearchBaseAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _projections.ListSearchBaseAsync(userId, cancellationToken);
    }

    public Task<IReadOnlyList<JobListingProjection>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _projections.ListByUserAsync(userId, cancellationToken);
    }

    public static JobListingRow ToListingRow(JobListingProjection row)
    {
        return new JobListingRow(
            row.CompanyName,
            row.Position,
            row.Link,
            row.Jd,
            row.Download,
            row.Status);
    }

    public async Task<JobListingSyncResultDto> SyncDirtyAsync(Guid userId, CancellationToken cancellationToken)
    {
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var profiles = await _catalog.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        var pulled = 0;
        var checkedFiles = 0;
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.SpreadsheetId))
            {
                continue;
            }

            checkedFiles++;
            if (await NeedsPullAsync(userId, access.AccessToken, profile.SpreadsheetId, cancellationToken))
            {
                await PullProfileSpreadsheetAsync(
                    userId,
                    access.AccessToken,
                    profile,
                    cancellationToken);
                pulled++;
            }
        }

        var folders = await _driveLayout.EnsureAsync(userId, access.AccessToken, cancellationToken);
        var searchBaseId = await _drive.FindSpreadsheetAsync(
            access.AccessToken,
            FlexisDriveLayout.SearchBaseFileName,
            folders.WorkspaceFolderId,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(searchBaseId))
        {
            checkedFiles++;
            if (await NeedsPullAsync(userId, access.AccessToken, searchBaseId, cancellationToken))
            {
                await PullSearchBaseAsync(userId, access.AccessToken, searchBaseId, cancellationToken);
                pulled++;
            }
        }

        if (pulled > 0)
        {
            JobFinancialService.InvalidateBoardCache(userId);
        }

        return new JobListingSyncResultDto(checkedFiles, pulled);
    }

    public async Task<JobListingSyncResultDto> ReindexAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var profiles = await _catalog.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        var pulled = 0;
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.SpreadsheetId))
            {
                continue;
            }

            await PullProfileSpreadsheetAsync(userId, access.AccessToken, profile, cancellationToken);
            pulled++;
        }

        var folders = await _driveLayout.EnsureAsync(userId, access.AccessToken, cancellationToken);
        var searchBase = await _sheets.EnsureSearchBaseWorkbookAsync(
            access.AccessToken,
            folders.WorkspaceFolderId,
            await _drive.FindSpreadsheetAsync(
                access.AccessToken,
                FlexisDriveLayout.SearchBaseFileName,
                folders.WorkspaceFolderId,
                cancellationToken),
            cancellationToken);
        await _drive.MoveFileToFolderAsync(
            access.AccessToken,
            searchBase.SpreadsheetId,
            folders.WorkspaceFolderId,
            cancellationToken);
        await PullSearchBaseAsync(userId, access.AccessToken, searchBase.SpreadsheetId, cancellationToken);
        pulled++;
        JobFinancialService.InvalidateBoardCache(userId);
        return new JobListingSyncResultDto(pulled, pulled);
    }

    private async Task<bool> NeedsPullAsync(
        Guid userId,
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var modified = await _drive.GetSpreadsheetModifiedTimeAsync(accessToken, spreadsheetId, cancellationToken);
        if (modified is null)
        {
            return false;
        }

        var state = await _syncStates.GetAsync(userId, spreadsheetId, cancellationToken);
        if (state is null || state.LastSyncedAt is null)
        {
            return true;
        }

        return !string.Equals(state.DriveModifiedTime, modified, StringComparison.Ordinal);
    }

    private async Task PullProfileSpreadsheetAsync(
        Guid userId,
        string accessToken,
        JobCatalogItem profile,
        CancellationToken cancellationToken)
    {
        var sheets = await _sheets.ListSheetsAsync(accessToken, profile.SpreadsheetId!, cancellationToken);
        var mainName = JobCatalogRules.SheetTabName(profile.Title);
        var main = sheets.FirstOrDefault(sheet => string.Equals(sheet.Name, mainName, StringComparison.Ordinal));
        if (main is not null)
        {
            var listings = await _sheets.ReadProfileListingsAsync(
                accessToken,
                profile.SpreadsheetId!,
                main.Name,
                cancellationToken);
            var rows = listings
                .Select((listing, index) => new JobListingSheetRow(index + 2, listing))
                .Where(row => !row.Listing.IsEmpty)
                .ToList();
            await ReplaceProfileMainFromRowsAsync(userId, profile.Id, profile.Title, rows, cancellationToken);
        }
        else
        {
            await _projections.ClearProfileMainAsync(userId, profile.Id, cancellationToken);
        }

        var archiveTabs = sheets.Where(sheet => JobSheetNames.IsArchiveTab(sheet.Name)).ToList();
        var existingArchives = await _projections.ListProfileArchivesAsync(userId, profile.Id, cancellationToken);
        var existingTabs = existingArchives.Select(item => item.ArchiveTab).Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        foreach (var tab in existingTabs.Where(tab => archiveTabs.All(sheet => sheet.Name != tab)))
        {
            await _projections.ReplaceScopeAsync(
                userId,
                JobListingProjectionSources.ProfileArchive,
                profile.Id,
                tab,
                [],
                cancellationToken);
        }

        foreach (var sheet in archiveTabs)
        {
            var listings = await _sheets.ReadProfileListingsAsync(
                accessToken,
                profile.SpreadsheetId!,
                sheet.Name,
                cancellationToken);
            var projections = listings
                .Select((listing, index) => FromListing(
                    userId,
                    profile.Id,
                    JobListingProjectionSources.ProfileArchive,
                    sheet.Name,
                    profile.Title,
                    index + 2,
                    listing,
                    issue: string.Empty))
                .Where(item => item.ListingKey.Length > 0)
                .ToList();
            await _projections.ReplaceScopeAsync(
                userId,
                JobListingProjectionSources.ProfileArchive,
                profile.Id,
                sheet.Name,
                projections,
                cancellationToken);
        }

        await _projections.SaveChangesAsync(cancellationToken);
        await MarkSyncedAsync(userId, accessToken, profile.SpreadsheetId!, cancellationToken);
    }

    private async Task PullSearchBaseAsync(
        Guid userId,
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var listings = await _sheets.ReadSearchBaseListingsAsync(accessToken, spreadsheetId, cancellationToken);
        var projections = listings
            .Where(row => !row.IsEmpty)
            .Select((row, index) => FromSearchBase(userId, index + 2, row))
            .ToList();
        await _projections.ReplaceScopeAsync(
            userId,
            JobListingProjectionSources.SearchBase,
            null,
            string.Empty,
            projections,
            cancellationToken);
        await _projections.SaveChangesAsync(cancellationToken);
        await MarkSyncedAsync(userId, accessToken, spreadsheetId, cancellationToken);
    }

    public async Task MarkSpreadsheetSyncedAsync(
        Guid userId,
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        await MarkSyncedAsync(userId, accessToken, spreadsheetId, cancellationToken);
    }

    private async Task MarkSyncedAsync(
        Guid userId,
        string accessToken,
        string spreadsheetId,
        CancellationToken cancellationToken)
    {
        var modified = await _drive.GetSpreadsheetModifiedTimeAsync(accessToken, spreadsheetId, cancellationToken)
            ?? string.Empty;
        var state = await _syncStates.GetAsync(userId, spreadsheetId, cancellationToken);
        if (state is null)
        {
            state = JobSheetSyncState.Create(userId, spreadsheetId);
            await _syncStates.AddAsync(state, cancellationToken);
        }

        state.MarkSynced(modified);
        await _syncStates.SaveChangesAsync(cancellationToken);
    }

    private static JobListingProjection FromListing(
        Guid userId,
        Guid? profileId,
        string source,
        string archiveTab,
        string profileName,
        int rowNumber,
        JobListingRow listing,
        string issue)
    {
        var key = JobFinancialRules.ListingKey(listing);
        var hash = ContentHash(
            listing.CompanyName,
            listing.Position,
            listing.Link,
            listing.Jd,
            listing.Download,
            listing.Status,
            issue,
            profileName);
        return JobListingProjection.Create(
            userId,
            profileId,
            source,
            archiveTab,
            key,
            listing.CompanyName,
            listing.Position,
            listing.Link,
            listing.Jd,
            listing.Download,
            listing.Status,
            issue,
            profileName,
            rowNumber,
            hash);
    }

    private static JobListingProjection FromSearchBase(Guid userId, int rowNumber, SearchBaseListingRow row)
    {
        var listing = new JobListingRow(
            row.CompanyName,
            row.Position,
            row.Link,
            row.Jd,
            row.Download,
            row.Status);
        return FromListing(
            userId,
            null,
            JobListingProjectionSources.SearchBase,
            string.Empty,
            row.Profile,
            rowNumber,
            listing,
            row.Issue);
    }

    private static string ContentHash(
        string companyName,
        string position,
        string link,
        string jd,
        string download,
        string status,
        string issue,
        string profileName)
    {
        var raw = string.Join(
            '\u001f',
            companyName,
            position,
            link,
            jd,
            download,
            status,
            issue,
            profileName);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}

public sealed record JobListingSyncResultDto(int Checked, int Pulled);
