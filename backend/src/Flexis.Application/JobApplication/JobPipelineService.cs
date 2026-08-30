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

    public JobPipelineService(
        IJobPipelineRepository entries,
        IJobCatalogRepository items,
        GoogleAccessTokenService tokens,
        IGoogleSheetsWorkspace sheets,
        GoogleDriveLayoutService driveLayout)
    {
        _entries = entries;
        _items = items;
        _tokens = tokens;
        _sheets = sheets;
        _driveLayout = driveLayout;
    }

    public async Task<JobPipelineBoardDto> GetBoardAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
            await _driveLayout.EnsureAsync(userId, accessToken, cancellationToken);
        }
        catch (ValidationFailedException)
        {
        }

        var profiles = await _items.ListAsync(userId, JobCatalogKind.Profile, cancellationToken);
        var sources = await _items.ListAsync(userId, JobCatalogKind.Source, cancellationToken);
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
        await _entries.AddAsync(resolved, cancellationToken);
        await _entries.SaveChangesAsync(cancellationToken);
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
        return ToDto(entry);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var entry = await RequireEntry(userId, id, cancellationToken);
        _entries.Remove(entry);
        await _entries.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobPipelineUpdateResultDto> ApplyAsync(
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

        var accessToken = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var sourceSheets = await _sheets.ListSheetsAsync(accessToken, source.SpreadsheetId, cancellationToken);
        var location = sourceSheets.FirstOrDefault(sheet => sheet.SheetId == entry.LocationSheetId)
            ?? throw new NotFoundException("Source location was not found.");
        var profileSheets = await _sheets.ListSheetsAsync(accessToken, profile.SpreadsheetId, cancellationToken);
        if (profileSheets.Count == 0)
        {
            throw new ValidationFailedException("This profile has no Google Sheet tab.");
        }

        var incoming = await _sheets.ReadListingsAsync(
            accessToken,
            source.SpreadsheetId,
            location.Name,
            cancellationToken);
        var existing = await _sheets.ReadListingsAsync(
            accessToken,
            profile.SpreadsheetId,
            profileSheets[0].Name,
            cancellationToken);
        var seen = new HashSet<string>(existing.Select(ListingKey), StringComparer.Ordinal);
        var fresh = new List<JobListingRow>();
        var skipped = 0;
        foreach (var listing in incoming)
        {
            if (listing.IsEmpty)
            {
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
                accessToken,
                profile.SpreadsheetId,
                profileSheets[0].Name,
                fresh,
                cancellationToken);
        }

        return new JobPipelineUpdateResultDto(fresh.Count, skipped);
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

    private static string ListingKey(JobListingRow listing)
    {
        return string.Join(
            '\u001f',
            listing.CompanyName.Trim().ToLowerInvariant(),
            listing.Position.Trim().ToLowerInvariant(),
            listing.Link.Trim().ToLowerInvariant());
    }
}
