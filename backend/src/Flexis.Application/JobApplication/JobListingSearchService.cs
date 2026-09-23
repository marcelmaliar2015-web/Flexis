using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobListingSearchService
{
    public static readonly IReadOnlyList<string> StatusOptions =
    [
        "Applied",
        "Interview",
        "Banned",
        "Invalid",
        "Expired",
        "Other"
    ];

    private readonly IJobCatalogRepository _catalog;
    private readonly GoogleAccessTokenService _tokens;
    private readonly GoogleDriveLayoutService _driveLayout;
    private readonly IGoogleDriveGateway _drive;
    private readonly IGoogleSheetsWorkspace _sheets;
    private readonly JobListingProjectionService _projections;

    public JobListingSearchService(
        IJobCatalogRepository catalog,
        GoogleAccessTokenService tokens,
        GoogleDriveLayoutService driveLayout,
        IGoogleDriveGateway drive,
        IGoogleSheetsWorkspace sheets,
        JobListingProjectionService projections)
    {
        _catalog = catalog;
        _tokens = tokens;
        _driveLayout = driveLayout;
        _drive = drive;
        _sheets = sheets;
        _projections = projections;
    }

    public async Task<JobSearchMetaDto> GetMetaAsync(Guid userId, CancellationToken cancellationToken)
    {
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var folders = await _driveLayout.EnsureAsync(userId, access.AccessToken, cancellationToken);
        var searchBase = await EnsureSearchBaseAsync(access.AccessToken, folders.WorkspaceFolderId, cancellationToken);
        var profiles = await _catalog.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        var profileOptions = profiles
            .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Select(item => new JobSearchProfileOptionDto(item.Id, item.Title))
            .ToList();

        var searchRows = await _projections.ListSearchBaseAsync(userId, cancellationToken);
        var known = profileOptions
            .Select(item => item.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var title in searchRows
            .Select(row => row.ProfileName.Trim())
            .Where(title => title.Length > 0 && known.Add(title))
            .OrderBy(title => title, StringComparer.OrdinalIgnoreCase))
        {
            profileOptions.Add(new JobSearchProfileOptionDto(null, title));
        }

        return new JobSearchMetaDto(
            profileOptions,
            StatusOptions,
            searchBase.SpreadsheetUrl,
            searchBase.SpreadsheetId);
    }

    public async Task<JobSearchResultDto> SearchAsync(
        Guid userId,
        JobSearchRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var folders = await _driveLayout.EnsureAsync(userId, access.AccessToken, cancellationToken);
        var searchBase = await EnsureSearchBaseAsync(access.AccessToken, folders.WorkspaceFolderId, cancellationToken);
        var profiles = (await _catalog.ListAsync(userId, JobCatalogKind.Profile, cancellationToken))
            .ToDictionary(item => item.Id);
        var queryProfile = NormalizeOptional(request.Profile);
        var queryCompany = NormalizeOptional(request.CompanyName);
        var queryPosition = NormalizeOptional(request.Position);
        var queryLink = NormalizeOptional(request.Link);
        var queryJd = NormalizeOptional(request.Jd);
        var queryStatus = NormalizeOptional(request.Status);

        var projected = await _projections.ListByUserAsync(userId, cancellationToken);
        if (projected.Count == 0)
        {
            await _projections.SyncDirtyAsync(userId, cancellationToken);
            projected = await _projections.ListByUserAsync(userId, cancellationToken);
        }

        var candidates = new List<ScoredCandidate>();
        var scanned = 0;
        foreach (var row in projected)
        {
            scanned++;
            var score = JobListingSearchRanker.Score(
                row.ProfileName,
                row.CompanyName,
                row.Position,
                row.Link,
                row.Jd,
                row.Status,
                queryProfile,
                queryCompany,
                queryPosition,
                queryLink,
                queryJd,
                queryStatus);
            if (score is null)
            {
                continue;
            }

            var spreadsheetUrl = searchBase.SpreadsheetUrl;
            Guid? profileId = row.ProfileId;
            if ((row.Source == JobListingProjectionSources.ProfileMain
                    || row.Source == JobListingProjectionSources.ProfileArchive)
                && row.ProfileId is Guid id
                && profiles.TryGetValue(id, out var profile))
            {
                spreadsheetUrl = profile.Url;
            }

            candidates.Add(new ScoredCandidate(
                score.Value,
                row.ProfileName,
                row.CompanyName,
                row.Position,
                row.Link,
                row.Jd,
                row.Download,
                row.Status,
                row.Issue,
                SourceLabel(row),
                spreadsheetUrl,
                profileId));
        }

        var ranked = candidates
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Profile, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Position, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new JobSearchHitDto(
                index + 1,
                item.Score,
                item.Profile,
                item.CompanyName,
                item.Position,
                item.Link,
                item.Jd,
                item.Download,
                item.Status,
                item.Issue,
                item.Source,
                item.SpreadsheetUrl,
                item.ProfileId))
            .ToList();

        return new JobSearchResultDto(ranked, scanned, searchBase.SpreadsheetUrl);
    }

    private async Task<CreatedSpreadsheet> EnsureSearchBaseAsync(
        string accessToken,
        string workspaceFolderId,
        CancellationToken cancellationToken)
    {
        var existingId = await _drive.FindSpreadsheetAsync(
            accessToken,
            FlexisDriveLayout.SearchBaseFileName,
            workspaceFolderId,
            cancellationToken);
        var workbook = await _sheets.EnsureSearchBaseWorkbookAsync(
            accessToken,
            workspaceFolderId,
            existingId,
            cancellationToken);
        await _drive.MoveFileToFolderAsync(
            accessToken,
            workbook.SpreadsheetId,
            workspaceFolderId,
            cancellationToken);
        return workbook;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string SourceLabel(JobListingProjection row)
    {
        if (row.Source == JobListingProjectionSources.SearchBase)
        {
            return "search-base";
        }

        if (row.Source == JobListingProjectionSources.ProfileArchive)
        {
            return string.IsNullOrWhiteSpace(row.ArchiveTab)
                ? "profile-archive"
                : $"profile-archive:{row.ArchiveTab.Trim()}";
        }

        return "profile";
    }

    private sealed record ScoredCandidate(
        double Score,
        string Profile,
        string CompanyName,
        string Position,
        string Link,
        string Jd,
        string Download,
        string Status,
        string Issue,
        string Source,
        string SpreadsheetUrl,
        Guid? ProfileId);
}
