using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobFinancialService
{
    public const int HistoryHours = 24 * 14;
    public const int StatisticsHistoryHours = 24 * 93;

    private readonly IJobFinancialSettingsRepository _settings;
    private readonly IJobFinancialSnapshotRepository _snapshots;
    private readonly IJobProfileStatisticsSnapshotRepository _profileSnapshots;
    private readonly IJobPipelineRepository _entries;
    private readonly IJobCatalogRepository _items;
    private readonly GoogleAccessTokenService _tokens;
    private readonly IGoogleSheetsWorkspace _sheets;
    private readonly JobApplicationActivity _activity;

    public JobFinancialService(
        IJobFinancialSettingsRepository settings,
        IJobFinancialSnapshotRepository snapshots,
        IJobProfileStatisticsSnapshotRepository profileSnapshots,
        IJobPipelineRepository entries,
        IJobCatalogRepository items,
        GoogleAccessTokenService tokens,
        IGoogleSheetsWorkspace sheets,
        JobApplicationActivity activity)
    {
        _settings = settings;
        _snapshots = snapshots;
        _profileSnapshots = profileSnapshots;
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
        var board = await BuildBoardAsync(userId, cancellationToken);
        await CaptureSnapshotsAsync(userId, board, cancellationToken);
        var history = await ListHistoryAsync(userId, cancellationToken);
        return board with { History = history };
    }

    public async Task<JobStatisticsBoardDto> GetStatisticsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var board = await BuildBoardAsync(userId, cancellationToken);
        await CaptureSnapshotsAsync(userId, board, cancellationToken);
        var profiles = DeduplicateProfiles(board.Rows);
        var history = await ListProfileHistoryAsync(userId, cancellationToken);
        return new JobStatisticsBoardDto(
            profiles,
            history,
            profiles.Sum(item => item.Applied),
            profiles.Sum(item => item.Interviews),
            profiles.Sum(item => item.Unapplied),
            profiles.Sum(item => item.Total),
            profiles.Sum(item => item.Price));
    }

    public async Task<IReadOnlyList<JobFinancialSnapshotDto>> GetHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await ListHistoryAsync(userId, cancellationToken);
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

    private async Task<JobFinancialBoardDto> BuildBoardAsync(
        Guid userId,
        CancellationToken cancellationToken)
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
            rows.Sum(row => row.LifetimeInterviews),
            []);
    }

    private sealed record ProfileFinancialCounts(
        int CurrentTotal,
        int CurrentApplied,
        int CurrentInterviews,
        int CurrentUnapplied,
        int ArchivedTotal,
        int ArchivedApplied,
        int ArchivedInterviews,
        int ArchivedUnapplied);

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
        var currentUnapplied = 0;
        var archivedTotal = 0;
        var archivedApplied = 0;
        var archivedInterviews = 0;
        var archivedUnapplied = 0;
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
                        (currentTotal, currentApplied, currentInterviews, currentUnapplied) =
                            JobFinancialRules.CountStatuses(listings);
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
                            archivedUnapplied += archived.Unapplied;
                        }
                        catch (GoogleOAuthException)
                        {
                        }
                    }

                    counts = new ProfileFinancialCounts(
                        currentTotal,
                        currentApplied,
                        currentInterviews,
                        currentUnapplied,
                        archivedTotal,
                        archivedApplied,
                        archivedInterviews,
                        archivedUnapplied);
                    listingCache[cacheKey] = counts;
                }

                currentTotal = counts.CurrentTotal;
                currentApplied = counts.CurrentApplied;
                currentInterviews = counts.CurrentInterviews;
                currentUnapplied = counts.CurrentUnapplied;
                archivedTotal = counts.ArchivedTotal;
                archivedApplied = counts.ArchivedApplied;
                archivedInterviews = counts.ArchivedInterviews;
                archivedUnapplied = counts.ArchivedUnapplied;
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
        var lifetimeUnapplied = currentUnapplied + archivedUnapplied;

        return new JobFinancialRowDto(
            entry.Id,
            entry.ProfileId,
            profileTitle,
            profile?.Url ?? string.Empty,
            sourceLabel,
            currentTotal,
            currentApplied,
            currentInterviews,
            currentUnapplied,
            entry.ApplyRate,
            entry.BonusRate,
            JobFinancialRules.Price(currentApplied, currentInterviews, entry.ApplyRate, entry.BonusRate),
            archivedTotal,
            archivedApplied,
            archivedInterviews,
            archivedUnapplied,
            archivedPrice,
            currentTotal + archivedTotal,
            lifetimeApplied,
            lifetimeInterviews,
            lifetimeUnapplied,
            JobFinancialRules.Price(lifetimeApplied, lifetimeInterviews, entry.ApplyRate, entry.BonusRate));
    }

    private async Task CaptureSnapshotsAsync(
        Guid userId,
        JobFinancialBoardDto board,
        CancellationToken cancellationToken)
    {
        var capturedHour = JobFinancialSnapshot.TruncateToHour(DateTimeOffset.UtcNow);
        var existing = await _snapshots.GetByUserAndHourAsync(userId, capturedHour, cancellationToken);
        if (existing is null)
        {
            await _snapshots.AddAsync(
                JobFinancialSnapshot.Create(
                    userId,
                    capturedHour,
                    board.AllPrice,
                    board.AllTotal,
                    board.AllApplied,
                    board.AllInterviews,
                    board.ArchivedAllPrice,
                    board.ArchivedAllTotal,
                    board.ArchivedAllApplied,
                    board.ArchivedAllInterviews,
                    board.LifetimeAllPrice,
                    board.LifetimeAllTotal,
                    board.LifetimeAllApplied,
                    board.LifetimeAllInterviews),
                cancellationToken);
        }
        else
        {
            existing.Replace(
                board.AllPrice,
                board.AllTotal,
                board.AllApplied,
                board.AllInterviews,
                board.ArchivedAllPrice,
                board.ArchivedAllTotal,
                board.ArchivedAllApplied,
                board.ArchivedAllInterviews,
                board.LifetimeAllPrice,
                board.LifetimeAllTotal,
                board.LifetimeAllApplied,
                board.LifetimeAllInterviews);
        }

        await _snapshots.SaveChangesAsync(cancellationToken);

        foreach (var profile in DeduplicateProfiles(board.Rows))
        {
            var profileExisting = await _profileSnapshots.GetByUserProfileAndHourAsync(
                userId,
                profile.ProfileId,
                capturedHour,
                cancellationToken);
            if (profileExisting is null)
            {
                await _profileSnapshots.AddAsync(
                    JobProfileStatisticsSnapshot.Create(
                        userId,
                        profile.ProfileId,
                        profile.ProfileTitle,
                        capturedHour,
                        profile.Applied,
                        profile.Interviews,
                        profile.Unapplied,
                        profile.Total,
                        profile.Price),
                    cancellationToken);
            }
            else
            {
                profileExisting.Replace(
                    profile.ProfileTitle,
                    profile.Applied,
                    profile.Interviews,
                    profile.Unapplied,
                    profile.Total,
                    profile.Price);
            }
        }

        await _profileSnapshots.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<JobStatisticsProfileDto> DeduplicateProfiles(
        IReadOnlyList<JobFinancialRowDto> rows)
    {
        return rows
            .GroupBy(row => row.ProfileId)
            .Select(group =>
            {
                var first = group.First();
                return new JobStatisticsProfileDto(
                    first.ProfileId,
                    first.ProfileTitle,
                    first.ProfileUrl,
                    first.Applied,
                    first.Interviews,
                    first.Unapplied,
                    first.Total,
                    JobFinancialRules.Price(
                        first.Applied,
                        first.Interviews,
                        first.ApplyRate,
                        first.BonusRate),
                    first.ApplyRate,
                    first.BonusRate);
            })
            .OrderBy(item => item.ProfileTitle, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<JobFinancialSnapshotDto>> ListHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var snapshots = await _snapshots.ListRecentAsync(userId, HistoryHours, cancellationToken);
        return snapshots
            .OrderBy(item => item.CapturedHour)
            .Select(item => new JobFinancialSnapshotDto(
                item.CapturedOn,
                item.CapturedHour,
                item.CapturedAt,
                item.TodayPrice,
                item.TodayTotal,
                item.TodayApplied,
                item.TodayInterviews,
                item.ArchivedPrice,
                item.ArchivedTotal,
                item.ArchivedApplied,
                item.ArchivedInterviews,
                item.LifetimePrice,
                item.LifetimeTotal,
                item.LifetimeApplied,
                item.LifetimeInterviews))
            .ToArray();
    }

    private async Task<IReadOnlyList<JobStatisticsPointDto>> ListProfileHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var snapshots = await _profileSnapshots.ListRecentAsync(
            userId,
            StatisticsHistoryHours,
            cancellationToken);
        return snapshots
            .Select(item => new JobStatisticsPointDto(
                item.ProfileId,
                item.ProfileTitle,
                item.CapturedOn,
                item.CapturedHour,
                item.Applied,
                item.Interviews,
                item.Unapplied,
                item.Total,
                item.Price))
            .ToArray();
    }
}
