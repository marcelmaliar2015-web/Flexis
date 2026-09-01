using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobCatalogService
{
    private readonly IJobCatalogRepository _items;
    private readonly IJobPipelineRepository _pipeline;
    private readonly IJobProfileBannedRepository _banned;
    private readonly GoogleAccessTokenService _tokens;
    private readonly IGoogleSheetsWorkspace _sheets;
    private readonly GoogleDriveLayoutService _driveLayout;
    private readonly JobApplicationActivity _activity;
    private readonly JobResumeService _resume;

    public JobCatalogService(
        IJobCatalogRepository items,
        IJobPipelineRepository pipeline,
        IJobProfileBannedRepository banned,
        GoogleAccessTokenService tokens,
        IGoogleSheetsWorkspace sheets,
        GoogleDriveLayoutService driveLayout,
        JobApplicationActivity activity,
        JobResumeService resume)
    {
        _items = items;
        _pipeline = pipeline;
        _banned = banned;
        _tokens = tokens;
        _sheets = sheets;
        _driveLayout = driveLayout;
        _activity = activity;
        _resume = resume;
    }

    public async Task<IReadOnlyList<JobCatalogItemDto>> ListAsync(
        Guid userId,
        JobCatalogKind kind,
        CancellationToken cancellationToken)
    {
        var items = await _items.ListAsync(userId, kind, cancellationToken);
        return items.Select(ToDto).ToArray();
    }

    public async Task<JobCatalogItemDto> CreateAsync(
        Guid userId,
        JobCatalogKind kind,
        JobCatalogWriteRequest request,
        CancellationToken cancellationToken)
    {
        var title = kind == JobCatalogKind.Profile
            ? JobCatalogRules.NormalizeProfileTitle(request.Title)
            : JobCatalogRules.NormalizeTitle(request.Title);
        if (await _items.TitleExistsAsync(userId, kind, title, null, cancellationToken))
        {
            throw new ConflictException($"A {KindLabel(kind)} with that title already exists.");
        }

        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var folders = await _driveLayout.EnsureAsync(userId, access.AccessToken, cancellationToken);
        var firstSheet = kind == JobCatalogKind.Profile
            ? JobCatalogRules.SheetTabName(title)
            : JobCatalogRules.DefaultSourceLocation;
        var workbookKind = kind == JobCatalogKind.Profile ? JobWorkbookKind.Profile : JobWorkbookKind.Source;
        var folderId = kind == JobCatalogKind.Profile ? folders.ProfilesFolderId : folders.SourcesFolderId;
        var spreadsheet = await _sheets.CreateWorkbookAsync(
            access.AccessToken,
            title,
            firstSheet,
            workbookKind,
            folderId,
            cancellationToken);

        try
        {
            await _driveLayout.PlaceWorkbookAsync(access.AccessToken, spreadsheet.SpreadsheetId, kind, folders, cancellationToken);
            if (kind == JobCatalogKind.Profile)
            {
                await _sheets.EnsureProfileInfoSheetAsync(
                    access.AccessToken,
                    spreadsheet.SpreadsheetId,
                    cancellationToken);
            }

            await _sheets.ProtectWorkbookAsync(
                access.AccessToken,
                spreadsheet.SpreadsheetId,
                access.OwnerEmail,
                workbookKind,
                cancellationToken);
            var item = JobCatalogItem.Create(userId, kind, title, spreadsheet.SpreadsheetUrl, spreadsheet.SpreadsheetId);
            await _items.AddAsync(item, cancellationToken);
            await _items.SaveChangesAsync(cancellationToken);
            await _activity.WriteAsync(
                userId,
                "catalog",
                kind == JobCatalogKind.Profile ? "create-profile" : "create-source",
                kind == JobCatalogKind.Profile
                    ? $"Created profile {title}"
                    : $"Created source {title}",
                kind == JobCatalogKind.Profile
                    ? $"Opened a profile Google Sheet named {title} with a main tab of the same name and a locked Profile info tab. Spreadsheet {spreadsheet.SpreadsheetId}."
                    : $"Opened a source Google Sheet named {title} with a first location tab {JobCatalogRules.DefaultSourceLocation}. Spreadsheet {spreadsheet.SpreadsheetId}.",
                cancellationToken);
            return ToDto(item);
        }
        catch
        {
            await _sheets.DeleteFileAsync(access.AccessToken, spreadsheet.SpreadsheetId, cancellationToken);
            throw;
        }
    }

    public async Task<JobCatalogItemDto> UpdateAsync(
        Guid userId,
        Guid id,
        JobCatalogWriteRequest request,
        CancellationToken cancellationToken)
    {
        var item = await RequireItem(userId, id, cancellationToken);
        var title = item.Kind == JobCatalogKind.Profile
            ? JobCatalogRules.NormalizeProfileTitle(request.Title)
            : JobCatalogRules.NormalizeTitle(request.Title);
        if (await _items.TitleExistsAsync(userId, item.Kind, title, item.Id, cancellationToken))
        {
            throw new ConflictException($"A {KindLabel(item.Kind)} with that title already exists.");
        }

        if (!string.Equals(item.Title, title, StringComparison.Ordinal) && HasSpreadsheet(item))
        {
            var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
            await _sheets.RenameFileAsync(access.AccessToken, item.SpreadsheetId, title, cancellationToken);
            if (item.Kind == JobCatalogKind.Profile)
            {
                var sheets = await _sheets.ListSheetsAsync(access.AccessToken, item.SpreadsheetId, cancellationToken);
                var previousMain = JobCatalogRules.SheetTabName(item.Title);
                var main = sheets.FirstOrDefault(sheet => string.Equals(sheet.Name, previousMain, StringComparison.Ordinal));
                if (main is not null)
                {
                    await _sheets.RenameSheetAsync(
                        access.AccessToken,
                        item.SpreadsheetId,
                        main.SheetId,
                        JobCatalogRules.SheetTabName(title),
                        cancellationToken);
                }
            }
        }

        var previousTitle = item.Title;
        item.SetTitle(title);
        await _items.SaveChangesAsync(cancellationToken);
        if (!string.Equals(previousTitle, title, StringComparison.Ordinal))
        {
            await _activity.WriteAsync(
                userId,
                "catalog",
                item.Kind == JobCatalogKind.Profile ? "rename-profile" : "rename-source",
                item.Kind == JobCatalogKind.Profile
                    ? $"Renamed profile to {title}"
                    : $"Renamed source to {title}",
                $"Previous title was {previousTitle}. The Google Sheet file name was updated to match.",
                cancellationToken);
            if (item.Kind == JobCatalogKind.Profile)
            {
                await _resume.SyncJobMasterAsync(userId, cancellationToken);
            }
        }

        return ToDto(item);
    }

    public async Task<ProfileInfoDto> GetProfileInfoAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var item = await RequireProfile(userId, profileId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var values = await _sheets.ReadProfileInfoAsync(accessToken, item.SpreadsheetId, cancellationToken);
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        await _sheets.ProtectWorkbookAsync(
            access.AccessToken,
            item.SpreadsheetId,
            access.OwnerEmail,
            JobWorkbookKind.Profile,
            cancellationToken);
        return ToProfileInfoDto(values);
    }

    public async Task<ProfileInfoDto> UpdateProfileInfoAsync(
        Guid userId,
        Guid profileId,
        ProfileInfoWriteRequest request,
        CancellationToken cancellationToken)
    {
        var item = await RequireProfile(userId, profileId, cancellationToken);
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var values = NormalizeProfileInfo(request);
        await _sheets.WriteProfileInfoAsync(access.AccessToken, item.SpreadsheetId, values, cancellationToken);
        await _sheets.ProtectWorkbookAsync(
            access.AccessToken,
            item.SpreadsheetId,
            access.OwnerEmail,
            JobWorkbookKind.Profile,
            cancellationToken);
        await _activity.WriteAsync(
            userId,
            "catalog",
            "update-profile-info",
            $"Updated profile info on {item.Title}",
            $"Wrote optional profile fields onto the locked Profile tab of {item.Title}.",
            cancellationToken);
        return ToProfileInfoDto(values);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var item = await RequireItem(userId, id, cancellationToken);
        var title = item.Title;
        var kind = item.Kind;
        if (HasSpreadsheet(item))
        {
            var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
            await _sheets.DeleteFileAsync(accessToken, item.SpreadsheetId, cancellationToken);
        }

        await _pipeline.RemoveByCatalogItemIdAsync(userId, id, cancellationToken);
        _items.Remove(item);
        await _items.SaveChangesAsync(cancellationToken);
        if (kind == JobCatalogKind.Profile)
        {
            await _resume.SyncJobMasterAsync(userId, cancellationToken);
        }
        await _activity.WriteAsync(
            userId,
            "catalog",
            kind == JobCatalogKind.Profile ? "delete-profile" : "delete-source",
            kind == JobCatalogKind.Profile
                ? $"Deleted profile {title}"
                : $"Deleted source {title}",
            kind == JobCatalogKind.Profile
                ? $"Removed profile {title}, its Google Sheet, and any pipeline rows that used it."
                : $"Removed source {title}, its Google Sheet, and any pipeline rows that used it.",
            cancellationToken);
    }

    public async Task<IReadOnlyList<SourceLocationDto>> ListLocationsAsync(
        Guid userId,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
        return sheets.Select(sheet => new SourceLocationDto(sheet.SheetId, sheet.Name)).ToArray();
    }

    public async Task<SourceLocationDto> AddLocationAsync(
        Guid userId,
        Guid sourceId,
        SourceLocationWriteRequest request,
        CancellationToken cancellationToken)
    {
        var name = JobCatalogRules.NormalizeLocationName(request.Name);
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(access.AccessToken, item.SpreadsheetId, cancellationToken);
        if (sheets.Any(sheet => string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("A location with that name already exists.");
        }

        var created = await _sheets.AddSourceLocationSheetAsync(access.AccessToken, item.SpreadsheetId, name, cancellationToken);
        await _sheets.ProtectWorkbookAsync(
            access.AccessToken,
            item.SpreadsheetId,
            access.OwnerEmail,
            JobWorkbookKind.Source,
            cancellationToken);
        await _activity.WriteAsync(
            userId,
            "catalog",
            "add-location",
            $"Added location {name} to {item.Title}",
            $"Created a new source tab named {name} on {item.Title}.",
            cancellationToken);
        return new SourceLocationDto(created.SheetId, created.Name);
    }

    public async Task<SourceLocationDto> RenameLocationAsync(
        Guid userId,
        Guid sourceId,
        int sheetId,
        SourceLocationWriteRequest request,
        CancellationToken cancellationToken)
    {
        var name = JobCatalogRules.NormalizeLocationName(request.Name);
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
        if (sheets.All(sheet => sheet.SheetId != sheetId))
        {
            throw new NotFoundException("Location was not found.");
        }

        if (sheets.Any(sheet => sheet.SheetId != sheetId
            && string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("A location with that name already exists.");
        }

        var previous = sheets.First(sheet => sheet.SheetId == sheetId).Name;
        await _sheets.RenameSheetAsync(accessToken, item.SpreadsheetId, sheetId, name, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "catalog",
            "rename-location",
            $"Renamed location to {name} on {item.Title}",
            $"Source {item.Title} location {previous} is now {name}.",
            cancellationToken);
        return new SourceLocationDto(sheetId, name);
    }

    public async Task DeleteLocationAsync(
        Guid userId,
        Guid sourceId,
        int sheetId,
        CancellationToken cancellationToken)
    {
        var item = await RequireSource(userId, sourceId, cancellationToken);
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, item.SpreadsheetId, cancellationToken);
        if (sheets.All(sheet => sheet.SheetId != sheetId))
        {
            throw new NotFoundException("Location was not found.");
        }

        if (sheets.Count <= 1)
        {
            throw new DomainRuleException("A source must keep at least one location.");
        }

        var locationName = sheets.First(sheet => sheet.SheetId == sheetId).Name;
        await _sheets.DeleteSheetAsync(accessToken, item.SpreadsheetId, sheetId, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "catalog",
            "delete-location",
            $"Deleted location {locationName} from {item.Title}",
            $"Removed the {locationName} tab from source {item.Title}. Pipeline rows that used that location were not changed here.",
            cancellationToken);
    }

    private async Task<JobCatalogItem> RequireItem(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        return await _items.GetByIdAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException("Item was not found.");
    }

    private async Task<JobCatalogItem> RequireSource(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var item = await RequireItem(userId, id, cancellationToken);
        if (item.Kind != JobCatalogKind.Source)
        {
            throw new NotFoundException("Source was not found.");
        }

        if (!HasSpreadsheet(item))
        {
            throw new ValidationFailedException("This source has no Google Sheet.");
        }

        return item;
    }

    private async Task<JobCatalogItem> RequireProfile(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var item = await RequireItem(userId, id, cancellationToken);
        if (item.Kind != JobCatalogKind.Profile)
        {
            throw new NotFoundException("Profile was not found.");
        }

        if (!HasSpreadsheet(item))
        {
            throw new ValidationFailedException("This profile has no Google Sheet.");
        }

        return item;
    }

    private static bool HasSpreadsheet(JobCatalogItem item)
    {
        return !string.IsNullOrWhiteSpace(item.SpreadsheetId);
    }

    private static JobCatalogItemDto ToDto(JobCatalogItem item)
    {
        return new JobCatalogItemDto(item.Id, item.Title, item.CreatedAt, item.Url, item.SpreadsheetId);
    }

    private static ProfileInfoDto ToProfileInfoDto(IReadOnlyDictionary<string, string> values)
    {
        return new ProfileInfoDto(
            ValueOrEmpty(values, "Name"),
            ValueOrEmpty(values, "Address"),
            ValueOrEmpty(values, "Mail"),
            ValueOrEmpty(values, "Password"),
            ValueOrEmpty(values, "LinkedIn"),
            ValueOrEmpty(values, "Phone"),
            ValueOrEmpty(values, "Sex"),
            ValueOrEmpty(values, "Target Rate (Monthly)"),
            ValueOrEmpty(values, "Race"),
            ValueOrEmpty(values, "Veteran Status"));
    }

    private static Dictionary<string, string> NormalizeProfileInfo(ProfileInfoWriteRequest request)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Name"] = ClipOptional(request.Name, 200),
            ["Address"] = ClipOptional(request.Address, 500),
            ["Mail"] = ClipOptional(request.Mail, 320),
            ["Password"] = ClipOptional(request.Password, 200),
            ["LinkedIn"] = ClipOptional(request.LinkedIn, 500),
            ["Phone"] = ClipOptional(request.Phone, 80),
            ["Sex"] = ClipOptional(request.Sex, 80),
            ["Target Rate (Monthly)"] = ClipOptional(request.TargetRateMonthly, 80),
            ["Race"] = ClipOptional(request.Race, 120),
            ["Veteran Status"] = ClipOptional(request.VeteranStatus, 120),
        };
    }

    private static string ClipOptional(string? value, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length > maxLength)
        {
            throw new ValidationFailedException($"A profile info field must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static string ValueOrEmpty(IReadOnlyDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static string KindLabel(JobCatalogKind itemKind)
    {
        return itemKind == JobCatalogKind.Profile ? "profile" : "source";
    }

    public async Task<IReadOnlyList<ProfileBannedCompanyDto>> ListBannedCompaniesAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        await RequireProfile(userId, profileId, cancellationToken);
        var items = await _banned.ListByProfileIdAsync(profileId, cancellationToken);
        return items.Select(ToBannedDto).ToArray();
    }

    public async Task<ProfileBannedCompanyDto> CreateBannedCompanyAsync(
        Guid userId,
        Guid profileId,
        ProfileBannedCompanyWriteRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await RequireProfile(userId, profileId, cancellationToken);
        var companyName = JobCatalogRules.NormalizeCompanyName(request.CompanyName);
        var matchKey = CompanyNameMatcher.MatchKey(companyName);
        if (await _banned.MatchKeyExistsAsync(profileId, matchKey, null, cancellationToken))
        {
            throw new ConflictException("That company is already banned on this profile.");
        }

        var created = JobProfileBannedCompany.Create(profileId, companyName, matchKey);
        await _banned.AddAsync(created, cancellationToken);
        await _banned.SaveChangesAsync(cancellationToken);
        await _activity.WriteAsync(
            userId,
            "catalog",
            "ban-add",
            $"Banned {companyName} on {profile.Title}",
            $"{companyName} is excluded from Update for every pipeline row that uses {profile.Title}. Matching listings on the profile main tab are flagged.",
            cancellationToken);
        return ToBannedDto(created);
    }

    public async Task<ProfileBannedCompanyDto> UpdateBannedCompanyAsync(
        Guid userId,
        Guid profileId,
        Guid companyId,
        ProfileBannedCompanyWriteRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await RequireProfile(userId, profileId, cancellationToken);
        var item = await _banned.GetByIdAsync(profileId, companyId, cancellationToken)
            ?? throw new NotFoundException("Banned company was not found.");
        var companyName = JobCatalogRules.NormalizeCompanyName(request.CompanyName);
        var matchKey = CompanyNameMatcher.MatchKey(companyName);
        if (await _banned.MatchKeyExistsAsync(profileId, matchKey, companyId, cancellationToken))
        {
            throw new ConflictException("That company is already banned on this profile.");
        }

        item.Replace(companyName, matchKey);
        await _banned.SaveChangesAsync(cancellationToken);
        await _activity.WriteAsync(
            userId,
            "catalog",
            "ban-edit",
            $"Renamed a banned company on {profile.Title}",
            $"Banned company is now {companyName} for profile {profile.Title}.",
            cancellationToken);
        return ToBannedDto(item);
    }

    public async Task DeleteBannedCompanyAsync(
        Guid userId,
        Guid profileId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var profile = await RequireProfile(userId, profileId, cancellationToken);
        var item = await _banned.GetByIdAsync(profileId, companyId, cancellationToken)
            ?? throw new NotFoundException("Banned company was not found.");
        var companyName = item.CompanyName;
        _banned.Remove(item);
        await _banned.SaveChangesAsync(cancellationToken);
        await _activity.WriteAsync(
            userId,
            "catalog",
            "ban-remove",
            $"Unbanned {companyName} on {profile.Title}",
            $"{companyName} can be copied onto {profile.Title} again from any source.",
            cancellationToken);
    }

    public async Task<ProfileBannedMatchesDto> ListBannedMatchesAsync(
        Guid userId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var profile = await RequireProfile(userId, profileId, cancellationToken);
        var bans = await _banned.ListByProfileIdAsync(profileId, cancellationToken);
        if (bans.Count == 0)
        {
            return new ProfileBannedMatchesDto([]);
        }

        if (string.IsNullOrWhiteSpace(profile.SpreadsheetId))
        {
            throw new ValidationFailedException("This profile has no Google Sheet.");
        }

        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var profileSheets = await _sheets.ListSheetsAsync(access.AccessToken, profile.SpreadsheetId, cancellationToken);
        var main = RequireMainProfileSheet(profileSheets, profile.Title);
        var profileRows = await _sheets.ReadListingsAsync(
            access.AccessToken,
            profile.SpreadsheetId,
            main.Name,
            cancellationToken);

        return new ProfileBannedMatchesDto(CollectBannedMatches(profileRows, bans));
    }

    private static SpreadsheetSheet RequireMainProfileSheet(
        IReadOnlyList<SpreadsheetSheet> sheets,
        string profileTitle)
    {
        var mainName = JobCatalogRules.SheetTabName(profileTitle);
        return sheets.FirstOrDefault(sheet => sheet.Name == mainName)
            ?? throw new ValidationFailedException("This profile has no main Google Sheet tab.");
    }

    private static ProfileBannedCompanyDto ToBannedDto(JobProfileBannedCompany item)
    {
        return new ProfileBannedCompanyDto(item.Id, item.CompanyName, item.CreatedAt);
    }

    private static IReadOnlyList<ProfileBannedMatchDto> CollectBannedMatches(
        IReadOnlyList<JobListingRow> rows,
        IReadOnlyList<JobProfileBannedCompany> bans)
    {
        var matches = new List<ProfileBannedMatchDto>();
        foreach (var row in rows)
        {
            if (row.IsEmpty)
            {
                continue;
            }

            var ban = MatchingBan(row.CompanyName, bans);
            if (ban is null)
            {
                continue;
            }

            matches.Add(new ProfileBannedMatchDto(row.CompanyName, row.Position, row.Link, ban));
        }

        return matches;
    }

    private static string? MatchingBan(string companyName, IReadOnlyList<JobProfileBannedCompany> bans)
    {
        foreach (var ban in bans)
        {
            if (CompanyNameMatcher.IsMatch(companyName, ban.CompanyName))
            {
                return ban.CompanyName;
            }
        }

        return null;
    }
}
