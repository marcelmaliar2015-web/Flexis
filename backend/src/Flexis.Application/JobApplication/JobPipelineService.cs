using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobPipelineService
{
    private readonly IJobPipelineRepository _entries;
    private readonly IJobCatalogRepository _items;
    private readonly GoogleAccessTokenService _tokens;
    private readonly IGoogleSheetsWorkspace _sheets;
    private readonly GoogleDriveLayoutService _driveLayout;
    private readonly JobFinancialService _financial;
    private readonly JobApplicationActivity _activity;

    public JobPipelineService(
        IJobPipelineRepository entries,
        IJobCatalogRepository items,
        GoogleAccessTokenService tokens,
        IGoogleSheetsWorkspace sheets,
        GoogleDriveLayoutService driveLayout,
        JobFinancialService financial,
        JobApplicationActivity activity)
    {
        _entries = entries;
        _items = items;
        _tokens = tokens;
        _sheets = sheets;
        _driveLayout = driveLayout;
        _financial = financial;
        _activity = activity;
    }

    public async Task<JobPipelineBoardDto> GetBoardAsync(Guid userId, CancellationToken cancellationToken)
    {
        GoogleSheetAccess? access = null;
        try
        {
            access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
            await _driveLayout.EnsureAsync(userId, access.AccessToken, cancellationToken);
        }
        catch (ValidationFailedException)
        {
        }

        var profiles = await _items.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        var sources = await _items.ListAsync(userId, JobCatalogKind.Source, cancellationToken);
        if (access is not null)
        {
            foreach (var item in profiles.Concat(sources))
            {
                if (string.IsNullOrWhiteSpace(item.SpreadsheetId))
                {
                    continue;
                }

                if (item.Kind == JobCatalogKind.Source)
                {
                    await _sheets.RemoveStatusColumnAsync(access.AccessToken, item.SpreadsheetId, cancellationToken);
                }
                else
                {
                    await _sheets.EnsureProfileStatusDropdownAsync(
                        access.AccessToken,
                        item.SpreadsheetId,
                        cancellationToken);
                }

                await _sheets.SetFixedRowHeightAsync(access.AccessToken, item.SpreadsheetId, cancellationToken);
                await _sheets.ProtectWorkbookAsync(
                    access.AccessToken,
                    item.SpreadsheetId,
                    access.OwnerEmail,
                    item.Kind == JobCatalogKind.Profile ? JobWorkbookKind.Profile : JobWorkbookKind.Source,
                    cancellationToken);
            }
        }

        var sourceOptions = new List<JobPipelineSourceOptionDto>(sources.Count);
        foreach (var source in sources)
        {
            IReadOnlyList<SourceLocationDto> locations = [];
            if (!string.IsNullOrWhiteSpace(source.SpreadsheetId))
            {
                locations = await ListSourceLocations(userId, source, cancellationToken);
            }

            sourceOptions.Add(new JobPipelineSourceOptionDto(source.Id, source.Title, locations));
        }

        var stored = await _entries.ListAsync(userId, cancellationToken);
        var entries = stored.Select(entry =>
        {
            var source = sourceOptions.FirstOrDefault(option => option.Id == entry.SourceId);
            var location = source?.Locations.FirstOrDefault(item => item.SheetId == entry.LocationSheetId);
            return new JobPipelineEntryDto(
                entry.Id,
                entry.ProfileId,
                entry.SourceId,
                entry.LocationSheetId,
                location?.Name ?? entry.LocationName,
                entry.CreatedAt);
        }).ToArray();

        return new JobPipelineBoardDto(
            entries,
            profiles.Select(profile => new JobPipelineOptionDto(profile.Id, profile.Title)).ToArray(),
            sourceOptions);
    }

    public async Task<JobPipelineEntryDto> CreateAsync(
        Guid userId,
        JobPipelineWriteRequest request,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveAsync(userId, request, null, cancellationToken);
        var settings = await _financial.GetOrCreateSettingsAsync(userId, cancellationToken);
        resolved.SetRates(settings.ApplyRate, settings.BonusRate);
        await _entries.AddAsync(resolved, cancellationToken);
        await _entries.SaveChangesAsync(cancellationToken);
        var description = await DescribeAsync(resolved, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "create",
            $"Added {description.ProfileTitle} to the pipeline",
            $"{description.ProfileTitle} is paired with {description.SourceTitle} · {description.LocationName}. Apply rate {resolved.ApplyRate}, bonus rate {resolved.BonusRate}.",
            cancellationToken);
        return ToDto(resolved);
    }

    public async Task<JobPipelineEntryDto> UpdateAsync(
        Guid userId,
        Guid id,
        JobPipelineWriteRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await RequireEntry(userId, id, cancellationToken);
        var resolved = await ResolveAsync(userId, request, id, cancellationToken);
        entry.Replace(resolved.ProfileId, resolved.SourceId, resolved.LocationSheetId, resolved.LocationName);
        await _entries.SaveChangesAsync(cancellationToken);
        var description = await DescribeAsync(entry, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "update",
            $"Changed pipeline pairing for {description.ProfileTitle}",
            $"{description.ProfileTitle} now uses {description.SourceTitle} · {description.LocationName}.",
            cancellationToken);
        return ToDto(entry);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entry = await RequireEntry(userId, id, cancellationToken);
        var description = await DescribeAsync(entry, cancellationToken);
        _entries.Remove(entry);
        await _entries.SaveChangesAsync(cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "delete",
            $"Removed {description.ProfileTitle} from the pipeline",
            $"{description.ProfileTitle} · {description.SourceTitle} · {description.LocationName} was removed. Listings already on the profile sheet were left in place.",
            cancellationToken);
    }

    public async Task DeleteAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entries = await _entries.ListAsync(userId, cancellationToken);
        await _entries.RemoveAllAsync(userId, cancellationToken);
        await _entries.SaveChangesAsync(cancellationToken);
        if (entries.Count == 0)
        {
            return;
        }

        await _activity.WriteAsync(
            userId,
            "pipeline",
            "delete-all",
            entries.Count == 1
                ? "Removed 1 pipeline entry"
                : $"Removed {entries.Count} pipeline entries",
            "Every pipeline pairing for this account was deleted. Profile sheets and listings were not deleted.",
            cancellationToken);
    }

    public async Task<JobPipelineUpdateResultDto> ApplyAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await ApplyCoreAsync(userId, id, cancellationToken);
        var entry = await RequireEntry(userId, id, cancellationToken);
        var description = await DescribeAsync(entry, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "update-listings",
            $"Updated listings for {description.ProfileTitle}",
            $"Copied {result.Added} listing(s) from {description.SourceTitle} · {description.LocationName} onto the {description.ProfileTitle} main tab. Skipped {result.Skipped} duplicate(s). Blocked {result.Banned} banned company match(es).",
            cancellationToken);
        return result;
    }

    public async Task<JobPipelineUpdateResultDto> ApplyAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entries = await _entries.ListAsync(userId, cancellationToken);
        var added = 0;
        var skipped = 0;
        var banned = 0;
        foreach (var entry in entries)
        {
            var result = await ApplyCoreAsync(userId, entry.Id, cancellationToken);
            added += result.Added;
            skipped += result.Skipped;
            banned += result.Banned;
        }

        if (entries.Count > 0)
        {
            await _activity.WriteAsync(
                userId,
                "pipeline",
                "update-all",
                entries.Count == 1
                    ? "Ran Update All on 1 pipeline row"
                    : $"Ran Update All on {entries.Count} pipeline rows",
                $"Copied {added} listing(s) onto profile main tabs. Skipped {skipped} duplicate(s). Blocked {banned} banned company match(es).",
                cancellationToken);
        }

        return new JobPipelineUpdateResultDto(added, skipped, banned);
    }

    private async Task<JobPipelineUpdateResultDto> ApplyCoreAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await RequireEntry(userId, id, cancellationToken);
        var profile = await RequireCatalog(userId, entry.ProfileId, JobCatalogKind.Profile, cancellationToken);
        var source = await RequireCatalog(userId, entry.SourceId, JobCatalogKind.Source, cancellationToken);
        if (string.IsNullOrWhiteSpace(profile.SpreadsheetId) || string.IsNullOrWhiteSpace(source.SpreadsheetId))
        {
            throw new ValidationFailedException("Profile and source must have a Google Sheet.");
        }

        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var sourceSheets = await _sheets.ListSheetsAsync(access.AccessToken, source.SpreadsheetId, cancellationToken);
        var location = sourceSheets.FirstOrDefault(sheet => sheet.SheetId == entry.LocationSheetId)
            ?? throw new NotFoundException("Source location was not found.");
        var profileSheets = await _sheets.ListSheetsAsync(access.AccessToken, profile.SpreadsheetId, cancellationToken);
        var main = RequireMainProfileSheet(profileSheets, profile.Title);

        var bans = await _entries.ListBannedAsync(entry.Id, cancellationToken);
        var incoming = await _sheets.ReadListingsAsync(
            access.AccessToken,
            source.SpreadsheetId,
            location.Name,
            cancellationToken);
        var existing = await _sheets.ReadListingsAsync(
            access.AccessToken,
            profile.SpreadsheetId,
            main.Name,
            cancellationToken);
        var seen = new HashSet<string>(existing.Select(ListingKey), StringComparer.Ordinal);
        var fresh = new List<JobListingRow>();
        var skipped = 0;
        var banned = 0;
        foreach (var listing in incoming)
        {
            if (listing.IsEmpty)
            {
                continue;
            }

            if (MatchingBan(listing.CompanyName, bans) is not null)
            {
                banned++;
                continue;
            }

            if (!seen.Add(ListingKey(listing)))
            {
                skipped++;
                continue;
            }

            fresh.Add(listing);
        }

        if (fresh.Count > 0)
        {
            await _sheets.AppendListingsAsync(
                access.AccessToken,
                profile.SpreadsheetId,
                main.Name,
                fresh,
                cancellationToken);
        }

        await _sheets.ProtectWorkbookAsync(
            access.AccessToken,
            profile.SpreadsheetId,
            access.OwnerEmail,
            JobWorkbookKind.Profile,
            cancellationToken);
        await _sheets.ProtectWorkbookAsync(
            access.AccessToken,
            source.SpreadsheetId,
            access.OwnerEmail,
            JobWorkbookKind.Source,
            cancellationToken);

        return new JobPipelineUpdateResultDto(fresh.Count, skipped, banned);
    }

    public async Task<JobPipelineForwardResultDto> ForwardAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await ForwardCoreAsync(userId, id, cancellationToken);
        var entry = await RequireEntry(userId, id, cancellationToken);
        var description = await DescribeAsync(entry, cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "forward",
            $"Forwarded {description.ProfileTitle}",
            $"Archived the {description.ProfileTitle} main tab as {result.ArchivedSheetName} and created a new empty main tab named {result.MainSheetName}.",
            cancellationToken);
        return result;
    }

    public async Task<JobPipelineBatchForwardResultDto> ForwardAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var entries = await _entries.ListAsync(userId, cancellationToken);
        var forwardedProfiles = new HashSet<Guid>();
        var archiveNames = new List<string>();
        foreach (var entry in entries)
        {
            if (!forwardedProfiles.Add(entry.ProfileId))
            {
                continue;
            }

            var result = await ForwardCoreAsync(userId, entry.Id, cancellationToken);
            archiveNames.Add(result.ArchivedSheetName);
        }

        if (forwardedProfiles.Count > 0)
        {
            await _activity.WriteAsync(
                userId,
                "pipeline",
                "forward-all",
                forwardedProfiles.Count == 1
                    ? "Forwarded 1 profile sheet"
                    : $"Forwarded {forwardedProfiles.Count} profile sheets",
                forwardedProfiles.Count == 1
                    ? $"Archived the current main tab as {archiveNames[0]} and opened a new empty main tab."
                    : $"Archived main tabs as {string.Join(", ", archiveNames)} and opened a new empty main tab for each profile.",
                cancellationToken);
        }

        return new JobPipelineBatchForwardResultDto(forwardedProfiles.Count);
    }

    private async Task<JobPipelineForwardResultDto> ForwardCoreAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await RequireEntry(userId, id, cancellationToken);
        var profile = await RequireCatalog(userId, entry.ProfileId, JobCatalogKind.Profile, cancellationToken);
        if (string.IsNullOrWhiteSpace(profile.SpreadsheetId))
        {
            throw new ValidationFailedException("This profile has no Google Sheet.");
        }

        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var profileSheets = await _sheets.ListSheetsAsync(access.AccessToken, profile.SpreadsheetId, cancellationToken);
        var main = RequireMainProfileSheet(profileSheets, profile.Title);
        var archiveName = JobSheetNames.NextArchiveTab(profileSheets.Select(sheet => sheet.Name));
        await _sheets.ReplaceProfileMainSheetAsync(
            access.AccessToken,
            profile.SpreadsheetId,
            main.SheetId,
            archiveName,
            main.Name,
            cancellationToken);
        await _sheets.ProtectWorkbookAsync(
            access.AccessToken,
            profile.SpreadsheetId,
            access.OwnerEmail,
            JobWorkbookKind.Profile,
            cancellationToken);

        return new JobPipelineForwardResultDto(archiveName, main.Name);
    }

    public async Task<IReadOnlyList<JobPipelineBannedCompanyDto>> ListBannedAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await RequireEntry(userId, id, cancellationToken);
        var items = await _entries.ListBannedAsync(id, cancellationToken);
        return items.Select(ToBannedDto).ToArray();
    }

    public async Task<JobPipelineBannedCompanyDto> CreateBannedAsync(
        Guid userId,
        Guid id,
        JobPipelineBannedCompanyWriteRequest request,
        CancellationToken cancellationToken)
    {
        await RequireEntry(userId, id, cancellationToken);
        var companyName = JobCatalogRules.NormalizeCompanyName(request.CompanyName);
        var matchKey = CompanyNameMatcher.MatchKey(companyName);
        if (await _entries.BannedMatchKeyExistsAsync(id, matchKey, null, cancellationToken))
        {
            throw new ConflictException("That company is already banned on this pipeline entry.");
        }

        var created = JobPipelineBannedCompany.Create(id, companyName, matchKey);
        await _entries.AddBannedAsync(created, cancellationToken);
        await _entries.SaveChangesAsync(cancellationToken);
        var description = await DescribeAsync(await RequireEntry(userId, id, cancellationToken), cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "ban-add",
            $"Banned {companyName} on {description.ProfileTitle}",
            $"{companyName} is now excluded from Update for {description.ProfileTitle} · {description.SourceTitle} · {description.LocationName}. Matching listings will be blocked.",
            cancellationToken);
        return ToBannedDto(created);
    }

    public async Task<JobPipelineBannedCompanyDto> UpdateBannedAsync(
        Guid userId,
        Guid id,
        Guid companyId,
        JobPipelineBannedCompanyWriteRequest request,
        CancellationToken cancellationToken)
    {
        await RequireEntry(userId, id, cancellationToken);
        var item = await _entries.GetBannedAsync(id, companyId, cancellationToken)
            ?? throw new NotFoundException("Banned company was not found.");
        var companyName = JobCatalogRules.NormalizeCompanyName(request.CompanyName);
        var matchKey = CompanyNameMatcher.MatchKey(companyName);
        if (await _entries.BannedMatchKeyExistsAsync(id, matchKey, companyId, cancellationToken))
        {
            throw new ConflictException("That company is already banned on this pipeline entry.");
        }

        item.Replace(companyName, matchKey);
        await _entries.SaveChangesAsync(cancellationToken);
        var description = await DescribeAsync(await RequireEntry(userId, id, cancellationToken), cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "ban-edit",
            $"Renamed a banned company on {description.ProfileTitle}",
            $"Banned company is now {companyName} for {description.ProfileTitle} · {description.SourceTitle} · {description.LocationName}.",
            cancellationToken);
        return ToBannedDto(item);
    }

    public async Task DeleteBannedAsync(Guid userId, Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        await RequireEntry(userId, id, cancellationToken);
        var item = await _entries.GetBannedAsync(id, companyId, cancellationToken)
            ?? throw new NotFoundException("Banned company was not found.");
        var companyName = item.CompanyName;
        var description = await DescribeAsync(await RequireEntry(userId, id, cancellationToken), cancellationToken);
        _entries.RemoveBanned(item);
        await _entries.SaveChangesAsync(cancellationToken);
        await _activity.WriteAsync(
            userId,
            "pipeline",
            "ban-remove",
            $"Unbanned {companyName} on {description.ProfileTitle}",
            $"{companyName} can be copied onto {description.ProfileTitle} again from {description.SourceTitle} · {description.LocationName}.",
            cancellationToken);
    }

    public async Task<JobPipelineBannedMatchesDto> ListBannedMatchesAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await RequireEntry(userId, id, cancellationToken);
        var bans = await _entries.ListBannedAsync(id, cancellationToken);
        if (bans.Count == 0)
        {
            return new JobPipelineBannedMatchesDto([], []);
        }

        var profile = await RequireCatalog(userId, entry.ProfileId, JobCatalogKind.Profile, cancellationToken);
        var source = await RequireCatalog(userId, entry.SourceId, JobCatalogKind.Source, cancellationToken);
        if (string.IsNullOrWhiteSpace(profile.SpreadsheetId) || string.IsNullOrWhiteSpace(source.SpreadsheetId))
        {
            throw new ValidationFailedException("Profile and source must have a Google Sheet.");
        }

        var access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        var sourceSheets = await _sheets.ListSheetsAsync(access.AccessToken, source.SpreadsheetId, cancellationToken);
        var location = sourceSheets.FirstOrDefault(sheet => sheet.SheetId == entry.LocationSheetId)
            ?? throw new NotFoundException("Source location was not found.");
        var profileSheets = await _sheets.ListSheetsAsync(access.AccessToken, profile.SpreadsheetId, cancellationToken);
        var main = RequireMainProfileSheet(profileSheets, profile.Title);
        var sourceRows = await _sheets.ReadListingsAsync(
            access.AccessToken,
            source.SpreadsheetId,
            location.Name,
            cancellationToken);
        var profileRows = await _sheets.ReadListingsAsync(
            access.AccessToken,
            profile.SpreadsheetId,
            main.Name,
            cancellationToken);

        return new JobPipelineBannedMatchesDto(
            CollectMatches("source", sourceRows, bans),
            CollectMatches("profile", profileRows, bans));
    }

    private async Task<JobPipelineEntry> ResolveAsync(
        Guid userId,
        JobPipelineWriteRequest request,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var profile = await RequireCatalog(userId, request.ProfileId, JobCatalogKind.Profile, cancellationToken);
        var source = await RequireCatalog(userId, request.SourceId, JobCatalogKind.Source, cancellationToken);
        if (string.IsNullOrWhiteSpace(source.SpreadsheetId))
        {
            throw new ValidationFailedException("This source has no Google Sheet.");
        }

        var locations = await ListSourceLocations(userId, source, cancellationToken);
        var location = locations.FirstOrDefault(item => item.SheetId == request.LocationSheetId)
            ?? throw new NotFoundException("Source location was not found.");
        if (await _entries.ExistsAsync(
            userId,
            profile.Id,
            source.Id,
            location.SheetId,
            excludeId,
            cancellationToken))
        {
            throw new ConflictException("That profile and source location are already in the pipeline.");
        }

        return JobPipelineEntry.Create(userId, profile.Id, source.Id, location.SheetId, location.Name);
    }

    private async Task<IReadOnlyList<SourceLocationDto>> ListSourceLocations(
        Guid userId,
        JobCatalogItem source,
        CancellationToken cancellationToken)
    {
        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sheets = await _sheets.ListSheetsAsync(accessToken, source.SpreadsheetId, cancellationToken);
        return sheets.Select(sheet => new SourceLocationDto(sheet.SheetId, sheet.Name)).ToArray();
    }

    private async Task<JobPipelineEntry> RequireEntry(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        return await _entries.GetByIdAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException("Pipeline entry was not found.");
    }

    private async Task<JobCatalogItem> RequireCatalog(
        Guid userId,
        Guid id,
        JobCatalogKind kind,
        CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(userId, id, cancellationToken)
            ?? throw new NotFoundException(kind == JobCatalogKind.Profile ? "Profile was not found." : "Source was not found.");
        if (item.Kind != kind)
        {
            throw new NotFoundException(kind == JobCatalogKind.Profile ? "Profile was not found." : "Source was not found.");
        }

        return item;
    }

    private async Task<(string ProfileTitle, string SourceTitle, string LocationName)> DescribeAsync(
        JobPipelineEntry entry,
        CancellationToken cancellationToken)
    {
        var profile = await _items.GetByIdAsync(entry.UserId, entry.ProfileId, cancellationToken);
        var source = await _items.GetByIdAsync(entry.UserId, entry.SourceId, cancellationToken);
        return (
            profile?.Title ?? "Unknown profile",
            source?.Title ?? "Unknown source",
            entry.LocationName);
    }

    private static SpreadsheetSheet RequireMainProfileSheet(
        IReadOnlyList<SpreadsheetSheet> sheets,
        string profileTitle)
    {
        var mainName = JobCatalogRules.SheetTabName(profileTitle);
        return sheets.FirstOrDefault(sheet => sheet.Name == mainName)
            ?? throw new ValidationFailedException("This profile has no main Google Sheet tab.");
    }

    private static JobPipelineEntryDto ToDto(JobPipelineEntry entry)
    {
        return new JobPipelineEntryDto(
            entry.Id,
            entry.ProfileId,
            entry.SourceId,
            entry.LocationSheetId,
            entry.LocationName,
            entry.CreatedAt);
    }

    private static JobPipelineBannedCompanyDto ToBannedDto(JobPipelineBannedCompany item)
    {
        return new JobPipelineBannedCompanyDto(item.Id, item.CompanyName, item.CreatedAt);
    }

    private static IReadOnlyList<JobPipelineBannedMatchDto> CollectMatches(
        string sheet,
        IReadOnlyList<JobListingRow> rows,
        IReadOnlyList<JobPipelineBannedCompany> bans)
    {
        var matches = new List<JobPipelineBannedMatchDto>();
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

            matches.Add(new JobPipelineBannedMatchDto(sheet, row.CompanyName, row.Position, row.Link, ban));
        }

        return matches;
    }

    private static string? MatchingBan(string companyName, IReadOnlyList<JobPipelineBannedCompany> bans)
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

    private static string ListingKey(JobListingRow listing)
    {
        return string.Join(
            '\u001f',
            listing.CompanyName.Trim().ToLowerInvariant(),
            listing.Position.Trim().ToLowerInvariant(),
            listing.Link.Trim().ToLowerInvariant());
    }
}
