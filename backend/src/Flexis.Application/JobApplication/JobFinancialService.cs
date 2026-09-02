using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobFinancialService
{
    private readonly IJobFinancialSettingsRepository _settings;
    private readonly IJobPipelineRepository _entries;
    private readonly IJobCatalogRepository _items;
    private readonly GoogleAccessTokenService _tokens;
    private readonly IGoogleSheetsWorkspace _sheets;
    private readonly JobApplicationActivity _activity;

    public JobFinancialService(
        IJobFinancialSettingsRepository settings,
        IJobPipelineRepository entries,
        IJobCatalogRepository items,
        GoogleAccessTokenService tokens,
        IGoogleSheetsWorkspace sheets,
        JobApplicationActivity activity)
    {
        _settings = settings;
        _entries = entries;
        _items = items;
        _tokens = tokens;
        _sheets = sheets;
        _activity = activity;
    }

    public async Task<JobFinancialSettings> GetOrCreateSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var existing = await _settings.GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = JobFinancialSettings.Create(userId);
        await _settings.AddAsync(created, cancellationToken);
        await _settings.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<JobFinancialBoardDto> GetBoardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var defaults = await GetOrCreateSettingsAsync(userId, cancellationToken);
        var stored = await _entries.ListAsync(userId, cancellationToken);
        var profiles = (await _items.ListAsync(userId, JobCatalogKind.Profile, cancellationToken))
            .ToDictionary(item => item.Id);
        var sources = (await _items.ListAsync(userId, JobCatalogKind.Source, cancellationToken))
            .ToDictionary(item => item.Id);

        GoogleSheetAccess? access = null;
        try
        {
            access = await _tokens.GetSheetAccessAsync(userId, cancellationToken);
        }
        catch (ValidationFailedException)
        {
        }
        catch (GoogleOAuthException)
        {
        }

        var dropdownDone = new HashSet<string>(StringComparer.Ordinal);
        var listingCache = new Dictionary<string, ProfileFinancialCounts>(StringComparer.Ordinal);
        var rows = new List<JobFinancialRowDto>(stored.Count);
        foreach (var entry in stored)
        {
            rows.Add(await ToRowAsync(
                entry,
                profiles,
                sources,
                access,
                dropdownDone,
                listingCache,
                cancellationToken));
        }

        return new JobFinancialBoardDto(
            new JobFinancialDefaultsDto(defaults.ApplyRate, defaults.BonusRate),
            rows,
            rows.Sum(row => row.Price),
            rows.Sum(row => row.Total),
            rows.Sum(row => row.Applied),
            rows.Sum(row => row.Interviews),
            rows.Sum(row => row.ArchivedPrice),
            rows.Sum(row => row.ArchivedTotal),
            rows.Sum(row => row.ArchivedApplied),
            rows.Sum(row => row.ArchivedInterviews),
            rows.Sum(row => row.LifetimePrice),
            rows.Sum(row => row.LifetimeTotal),
            rows.Sum(row => row.LifetimeApplied),
            rows.Sum(row => row.LifetimeInterviews));
    }

    public async Task<JobFinancialDefaultsDto> UpdateDefaultsAsync(
        Guid userId,
        JobFinancialRatesRequest request,
        CancellationToken cancellationToken)
    {
        var applyRate = JobFinancialRules.NormalizeRate(request.ApplyRate, "Apply rate");
        var bonusRate = JobFinancialRules.NormalizeRate(request.BonusRate, "Bonus rate");
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        settings.SetRates(applyRate, bonusRate);
        await _settings.SaveChangesAsync(cancellationToken);
        await _activity.WriteAsync(
            userId,
            "financial",
            "defaults",
            "Updated default apply and bonus rates",
            $"New pipeline rows will use apply rate {applyRate} and bonus rate {bonusRate}. Existing rows keep their own rates.",
            cancellationToken);
        return new JobFinancialDefaultsDto(applyRate, bonusRate);
    }

    public async Task<JobFinancialRowDto> UpdateRatesAsync(
        Guid userId,
        Guid entryId,
        JobFinancialRatesRequest request,
        CancellationToken cancellationToken)
    {
        var applyRate = JobFinancialRules.NormalizeRate(request.ApplyRate, "Apply rate");
        var bonusRate = JobFinancialRules.NormalizeRate(request.BonusRate, "Bonus rate");
        var entry = await _entries.GetByIdAsync(userId, entryId, cancellationToken)
            ?? throw new NotFoundException("Pipeline entry was not found.");
        entry.SetRates(applyRate, bonusRate);
        await _entries.SaveChangesAsync(cancellationToken);
        var profiles = (await _items.ListAsync(userId, JobCatalogKind.Profile, cancellationToken))
            .ToDictionary(item => item.Id);
        var sources = (await _items.ListAsync(userId, JobCatalogKind.Source, cancellationToken))
            .ToDictionary(item => item.Id);
        var profileTitle = profiles.TryGetValue(entry.ProfileId, out var profile)
            ? profile.Title
            : "Unknown profile";
        var sourceTitle = sources.TryGetValue(entry.SourceId, out var source)
            ? source.Title
            : "Unknown source";
        await _activity.WriteAsync(
            userId,
            "financial",
            "rates",
            $"Updated rates for {profileTitle}",
            $"{profileTitle} · {sourceTitle} · {entry.LocationName}. Apply rate set to {applyRate}. Bonus rate set to {bonusRate}.",
            cancellationToken);
        var board = await GetBoardAsync(userId, cancellationToken);
        return board.Rows.FirstOrDefault(row => row.EntryId == entryId)
            ?? throw new NotFoundException("Pipeline entry was not found.");
    }

    private sealed record ProfileFinancialCounts(
        int CurrentTotal,
        int CurrentApplied,
        int CurrentInterviews,
        int ArchivedTotal,
        int ArchivedApplied,
        int ArchivedInterviews);

    private async Task<JobFinancialRowDto> ToRowAsync(
        JobPipelineEntry entry,
        IReadOnlyDictionary<Guid, JobCatalogItem> profiles,
        IReadOnlyDictionary<Guid, JobCatalogItem> sources,
        GoogleSheetAccess? access,
        HashSet<string> dropdownDone,
        Dictionary<string, ProfileFinancialCounts> listingCache,
        CancellationToken cancellationToken)
    {
        var profileTitle = profiles.TryGetValue(entry.ProfileId, out var profile)
            ? profile.Title
            : "Unknown profile";
        var sourceTitle = sources.TryGetValue(entry.SourceId, out var source)
            ? source.Title
            : "Unknown source";
        var sourceLabel = $"{sourceTitle} · {entry.LocationName}";
        var currentTotal = 0;
        var currentApplied = 0;
        var currentInterviews = 0;
        var archivedTotal = 0;
        var archivedApplied = 0;
        var archivedInterviews = 0;
        if (access is not null
            && profile is not null
            && !string.IsNullOrWhiteSpace(profile.SpreadsheetId))
        {
            try
            {
                if (dropdownDone.Add(profile.SpreadsheetId))
                {
                    await _sheets.EnsureProfileStatusDropdownAsync(
                        access.AccessToken,
                        profile.SpreadsheetId,
                        cancellationToken);
                }

                var cacheKey = profile.SpreadsheetId;
                if (!listingCache.TryGetValue(cacheKey, out var counts))
                {
                    var sheets = await _sheets.ListSheetsAsync(
                        access.AccessToken,
                        profile.SpreadsheetId,
                        cancellationToken);
                    var main = sheets.FirstOrDefault(sheet => sheet.Name == JobCatalogRules.SheetTabName(profile.Title));
                    if (main is not null)
                    {
                        var listings = await _sheets.ReadProfileListingsAsync(
                            access.AccessToken,
                            profile.SpreadsheetId,
                            main.Name,
                            cancellationToken);
                        (currentTotal, currentApplied, currentInterviews) = JobFinancialRules.CountStatuses(listings);
                    }

                    foreach (var sheet in sheets.Where(item => JobSheetNames.IsArchiveTab(item.Name)))
                    {
                        try
                        {
                            var listings = await _sheets.ReadProfileListingsAsync(
                                access.AccessToken,
                                profile.SpreadsheetId,
                                sheet.Name,
                                cancellationToken);
                            var archived = JobFinancialRules.CountStatuses(listings);
                            archivedTotal += archived.Total;
                            archivedApplied += archived.Applied;
                            archivedInterviews += archived.Interviews;
                        }
                        catch (GoogleOAuthException)
                        {
                        }
                    }

                    counts = new ProfileFinancialCounts(
                        currentTotal,
                        currentApplied,
                        currentInterviews,
                        archivedTotal,
                        archivedApplied,
                        archivedInterviews);
                    listingCache[cacheKey] = counts;
                }

                currentTotal = counts.CurrentTotal;
                currentApplied = counts.CurrentApplied;
                currentInterviews = counts.CurrentInterviews;
                archivedTotal = counts.ArchivedTotal;
                archivedApplied = counts.ArchivedApplied;
                archivedInterviews = counts.ArchivedInterviews;
            }
            catch (GoogleOAuthException)
            {
            }
        }

        var archivedPrice = JobFinancialRules.Price(
            archivedApplied,
            archivedInterviews,
            entry.ApplyRate,
            entry.BonusRate);
        var lifetimeApplied = currentApplied + archivedApplied;
        var lifetimeInterviews = currentInterviews + archivedInterviews;

        return new JobFinancialRowDto(
            entry.Id,
            profileTitle,
            sourceLabel,
            currentTotal,
            currentApplied,
            currentInterviews,
            entry.ApplyRate,
            entry.BonusRate,
            JobFinancialRules.Price(currentApplied, currentInterviews, entry.ApplyRate, entry.BonusRate),
            archivedTotal,
            archivedApplied,
            archivedInterviews,
            archivedPrice,
            currentTotal + archivedTotal,
            lifetimeApplied,
            lifetimeInterviews,
            JobFinancialRules.Price(lifetimeApplied, lifetimeInterviews, entry.ApplyRate, entry.BonusRate));
    }
}
