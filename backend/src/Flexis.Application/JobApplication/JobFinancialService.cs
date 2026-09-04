using System.Collections.Concurrent;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobFinancialService
{
    public const int HistoryHours = 24 * 14;
    public const int StatisticsHistoryHours = 24 * 93;
    private static readonly TimeSpan BoardCacheTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DropdownCheckTtl = TimeSpan.FromHours(12);
    private static readonly ConcurrentDictionary<Guid, (DateTimeOffset At, JobFinancialBoardDto Board)> BoardCache = new();
    private static readonly ConcurrentDictionary<string, DateTimeOffset> DropdownCheckedAt = new(StringComparer.Ordinal);

    private readonly IJobFinancialSettingsRepository _settings;
    private readonly IJobFinancialSnapshotRepository _snapshots;
    private readonly IJobProfileStatisticsSnapshotRepository _profileSnapshots;
    private readonly IJobListingCopyRepository _copies;
    private readonly IJobListingStatusRepository _statuses;
    private readonly IJobPipelineRepository _entries;
    private readonly IJobCatalogRepository _items;
    private readonly GoogleAccessTokenService _tokens;
    private readonly IGoogleSheetsWorkspace _sheets;
    private readonly JobApplicationActivity _activity;

    public JobFinancialService(
        IJobFinancialSettingsRepository settings,
        IJobFinancialSnapshotRepository snapshots,
        IJobProfileStatisticsSnapshotRepository profileSnapshots,
        IJobListingCopyRepository copies,
        IJobListingStatusRepository statuses,
        IJobPipelineRepository entries,
        IJobCatalogRepository items,
        GoogleAccessTokenService tokens,
        IGoogleSheetsWorkspace sheets,
        JobApplicationActivity activity)
    {
        _settings = settings;
        _snapshots = snapshots;
        _profileSnapshots = profileSnapshots;
        _copies = copies;
        _statuses = statuses;
        _entries = entries;
        _items = items;
        _tokens = tokens;
        _sheets = sheets;
        _activity = activity;
    }

    public static void InvalidateBoardCache(Guid userId)
    {
        BoardCache.TryRemove(userId, out _);
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
        var history = await ListProfileHistoryAsync(userId, profiles, cancellationToken);
        return new JobStatisticsBoardDto(
            profiles,
            history,
            profiles.Sum(item => item.Applied),
            profiles.Sum(item => item.Interviews),
            profiles.Sum(item => item.Unapplied),
            profiles.Sum(item => item.Ready),
            profiles.Sum(item => item.NotReady),
            profiles.Sum(item => item.Total),
            profiles.Sum(item => item.Price),
            profiles.Sum(item => item.TodayApplied),
            profiles.Sum(item => item.TodayInterviews),
            profiles.Sum(item => item.TodayUnapplied),
            profiles.Sum(item => item.TodayReady),
            profiles.Sum(item => item.TodayNotReady),
            profiles.Sum(item => item.TodayTotal),
            profiles.Sum(item => item.TodayPrice));
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
        if (BoardCache.TryGetValue(userId, out var cached)
            && DateTimeOffset.UtcNow - cached.At < BoardCacheTtl)
        {
            return cached.Board;
        }

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
        var listingCache = new Dictionary<Guid, ProfileFinancialCounts>();
        var rows = new List<JobFinancialRowDto>(stored.Count);
        foreach (var entry in stored)
        {
            rows.Add(await ToRowAsync(
                userId,
                entry,
                profiles,
                sources,
                access,
                dropdownDone,
                listingCache,
                cancellationToken));
        }

        await _statuses.SaveChangesAsync(cancellationToken);

        var board = new JobFinancialBoardDto(
            new JobFinancialDefaultsDto(defaults.ApplyRate, defaults.BonusRate),
            rows,
            rows.Sum(row => row.Price),
            rows.Sum(row => row.Total),
            rows.Sum(row => row.Ready),
            rows.Sum(row => row.NotReady),
            rows.Sum(row => row.Applied),
            rows.Sum(row => row.Interviews),
            rows.Sum(row => row.Unapplied),
            rows.Sum(row => row.TodayPrice),
            rows.Sum(row => row.TodayTotal),
            rows.Sum(row => row.TodayReady),
            rows.Sum(row => row.TodayNotReady),
            rows.Sum(row => row.TodayApplied),
            rows.Sum(row => row.TodayInterviews),
            rows.Sum(row => row.TodayUnapplied),
            rows.Sum(row => row.ArchivedPrice),
            rows.Sum(row => row.ArchivedTotal),
            rows.Sum(row => row.ArchivedReady),
            rows.Sum(row => row.ArchivedNotReady),
            rows.Sum(row => row.ArchivedApplied),
            rows.Sum(row => row.ArchivedInterviews),
            rows.Sum(row => row.ArchivedUnapplied),
            rows.Sum(row => row.LifetimePrice),
            rows.Sum(row => row.LifetimeTotal),
            rows.Sum(row => row.LifetimeReady),
            rows.Sum(row => row.LifetimeNotReady),
            rows.Sum(row => row.LifetimeApplied),
            rows.Sum(row => row.LifetimeInterviews),
            rows.Sum(row => row.LifetimeUnapplied),
            []);
        BoardCache[userId] = (DateTimeOffset.UtcNow, board);
        return board;
    }

    private sealed record ProfileFinancialCounts(
        int CurrentTotal,
        int CurrentReady,
        int CurrentNotReady,
        int CurrentApplied,
        int CurrentInterviews,
        int CurrentUnapplied,
        int TodayTotal,
        int TodayReady,
        int TodayNotReady,
        int TodayApplied,
        int TodayInterviews,
        int TodayUnapplied,
        int ArchivedTotal,
        int ArchivedReady,
        int ArchivedNotReady,
        int ArchivedApplied,
        int ArchivedInterviews,
        int ArchivedUnapplied);

    private async Task<JobFinancialRowDto> ToRowAsync(
        Guid userId,
        JobPipelineEntry entry,
        IReadOnlyDictionary<Guid, JobCatalogItem> profiles,
        IReadOnlyDictionary<Guid, JobCatalogItem> sources,
        GoogleSheetAccess? access,
        HashSet<string> dropdownDone,
        Dictionary<Guid, ProfileFinancialCounts> listingCache,
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
        var currentReady = 0;
        var currentNotReady = 0;
        var currentApplied = 0;
        var currentInterviews = 0;
        var currentUnapplied = 0;
        var todayTotal = 0;
        var todayReady = 0;
        var todayNotReady = 0;
        var todayApplied = 0;
        var todayInterviews = 0;
        var todayUnapplied = 0;
        var archivedTotal = 0;
        var archivedReady = 0;
        var archivedNotReady = 0;
        var archivedApplied = 0;
        var archivedInterviews = 0;
        var archivedUnapplied = 0;
        if (access is not null
            && profile is not null
            && !string.IsNullOrWhiteSpace(profile.SpreadsheetId))
        {
            try
            {
                if (dropdownDone.Add(profile.SpreadsheetId)
                    && ShouldEnsureStatusDropdown(profile.SpreadsheetId))
                {
                    await _sheets.EnsureProfileStatusDropdownAsync(
                        access.AccessToken,
                        profile.SpreadsheetId,
                        cancellationToken);
                    DropdownCheckedAt[profile.SpreadsheetId] = DateTimeOffset.UtcNow;
                }

                if (!listingCache.TryGetValue(entry.ProfileId, out var counts))
                {
                    var sheets = await _sheets.ListSheetsAsync(
                        access.AccessToken,
                        profile.SpreadsheetId,
                        cancellationToken);
                    var main = sheets.FirstOrDefault(sheet => sheet.Name == JobCatalogRules.SheetTabName(profile.Title));
                    IReadOnlyList<JobListingRow> mainListings = [];
                    if (main is not null)
                    {
                        mainListings = await _sheets.ReadProfileListingsAsync(
                            access.AccessToken,
                            profile.SpreadsheetId,
                            main.Name,
                            cancellationToken);
                        (currentTotal, currentReady, currentNotReady, currentApplied, currentInterviews, currentUnapplied) =
                            JobFinancialRules.CountStatuses(mainListings);
                        await SyncStatusEventsAsync(
                            userId,
                            entry.ProfileId,
                            mainListings,
                            cancellationToken);
                    }

                    var latestBatch = await _copies.GetLatestByProfileAsync(
                        userId,
                        entry.ProfileId,
                        cancellationToken);
                    if (latestBatch is not null && latestBatch.Items.Count > 0)
                    {
                        var todayKeys = latestBatch.Items
                            .Select(item => item.ListingKey)
                            .ToHashSet(StringComparer.Ordinal);
                        (todayTotal, todayReady, todayNotReady, todayApplied, todayInterviews, todayUnapplied) =
                            JobFinancialRules.CountStatuses(mainListings, todayKeys);
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
                            archivedReady += archived.Ready;
                            archivedNotReady += archived.NotReady;
                            archivedApplied += archived.Applied;
                            archivedInterviews += archived.Interviews;
                            archivedUnapplied += archived.Unapplied;
                        }
                        catch (GoogleOAuthException exception) when (IsSheetsQuotaExceeded(exception.Message))
                        {
                            throw;
                        }
                        catch (GoogleOAuthException)
                        {
                        }
                    }

                    counts = new ProfileFinancialCounts(
                        currentTotal,
                        currentReady,
                        currentNotReady,
                        currentApplied,
                        currentInterviews,
                        currentUnapplied,
                        todayTotal,
                        todayReady,
                        todayNotReady,
                        todayApplied,
                        todayInterviews,
                        todayUnapplied,
                        archivedTotal,
                        archivedReady,
                        archivedNotReady,
                        archivedApplied,
                        archivedInterviews,
                        archivedUnapplied);
                    listingCache[entry.ProfileId] = counts;
                }

                currentTotal = counts.CurrentTotal;
                currentReady = counts.CurrentReady;
                currentNotReady = counts.CurrentNotReady;
                currentApplied = counts.CurrentApplied;
                currentInterviews = counts.CurrentInterviews;
                currentUnapplied = counts.CurrentUnapplied;
                todayTotal = counts.TodayTotal;
                todayReady = counts.TodayReady;
                todayNotReady = counts.TodayNotReady;
                todayApplied = counts.TodayApplied;
                todayInterviews = counts.TodayInterviews;
                todayUnapplied = counts.TodayUnapplied;
                archivedTotal = counts.ArchivedTotal;
                archivedReady = counts.ArchivedReady;
                archivedNotReady = counts.ArchivedNotReady;
                archivedApplied = counts.ArchivedApplied;
                archivedInterviews = counts.ArchivedInterviews;
                archivedUnapplied = counts.ArchivedUnapplied;
            }
            catch (GoogleOAuthException exception) when (IsSheetsQuotaExceeded(exception.Message))
            {
                InvalidateBoardCache(userId);
                throw;
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
        var lifetimeReady = currentReady + archivedReady;
        var lifetimeNotReady = currentNotReady + archivedNotReady;

        return new JobFinancialRowDto(
            entry.Id,
            entry.ProfileId,
            profileTitle,
            profile?.Url ?? string.Empty,
            sourceLabel,
            currentTotal,
            currentReady,
            currentNotReady,
            currentApplied,
            currentInterviews,
            currentUnapplied,
            entry.ApplyRate,
            entry.BonusRate,
            JobFinancialRules.Price(currentApplied, currentInterviews, entry.ApplyRate, entry.BonusRate),
            todayTotal,
            todayReady,
            todayNotReady,
            todayApplied,
            todayInterviews,
            todayUnapplied,
            JobFinancialRules.Price(todayApplied, todayInterviews, entry.ApplyRate, entry.BonusRate),
            archivedTotal,
            archivedReady,
            archivedNotReady,
            archivedApplied,
            archivedInterviews,
            archivedUnapplied,
            archivedPrice,
            currentTotal + archivedTotal,
            lifetimeReady,
            lifetimeNotReady,
            lifetimeApplied,
            lifetimeInterviews,
            lifetimeUnapplied,
            JobFinancialRules.Price(lifetimeApplied, lifetimeInterviews, entry.ApplyRate, entry.BonusRate));
    }

    private async Task SyncStatusEventsAsync(
        Guid userId,
        Guid profileId,
        IReadOnlyList<JobListingRow> listings,
        CancellationToken cancellationToken)
    {
        var states = await _statuses.ListStatesAsync(userId, profileId, cancellationToken);
        var byKey = states.ToDictionary(item => item.ListingKey, StringComparer.Ordinal);
        var occurredAt = DateTimeOffset.UtcNow;
        foreach (var listing in listings)
        {
            if (listing.IsEmpty)
            {
                continue;
            }

            var key = JobFinancialRules.ListingKey(listing);
            var status = listing.Status.Trim();
            if (!byKey.TryGetValue(key, out var state))
            {
                var created = JobListingStatusState.Create(userId, profileId, key, status);
                await _statuses.AddStateAsync(created, cancellationToken);
                byKey[key] = created;
                continue;
            }

            if (string.Equals(state.Status, status, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            state.SetStatus(status);
            if (JobFinancialRules.IsTrackedStatus(status))
            {
                await _statuses.AddEventAsync(
                    JobListingStatusEvent.Create(userId, profileId, key, status, occurredAt),
                    cancellationToken);
            }
        }
    }

    private async Task CaptureSnapshotsAsync(
        Guid userId,
        JobFinancialBoardDto board,
        CancellationToken cancellationToken)
    {
        var profiles = DeduplicateProfiles(board.Rows);
        var todayPrice = profiles.Sum(item => item.TodayPrice);
        var todayTotal = profiles.Sum(item => item.TodayTotal);
        var todayApplied = profiles.Sum(item => item.TodayApplied);
        var todayInterviews = profiles.Sum(item => item.TodayInterviews);
        var mainPrice = profiles.Sum(item => item.Price);
        var mainTotal = profiles.Sum(item => item.Total);
        var mainApplied = profiles.Sum(item => item.Applied);
        var mainInterviews = profiles.Sum(item => item.Interviews);
        var uniqueRows = board.Rows
            .GroupBy(row => row.ProfileId)
            .Select(group => group.First())
            .ToArray();
        var archivedPrice = uniqueRows.Sum(row => row.ArchivedPrice);
        var archivedTotal = uniqueRows.Sum(row => row.ArchivedTotal);
        var archivedApplied = uniqueRows.Sum(row => row.ArchivedApplied);
        var archivedInterviews = uniqueRows.Sum(row => row.ArchivedInterviews);
        var lifetimePrice = uniqueRows.Sum(row => row.LifetimePrice);
        var lifetimeTotal = uniqueRows.Sum(row => row.LifetimeTotal);
        var lifetimeApplied = uniqueRows.Sum(row => row.LifetimeApplied);
        var lifetimeInterviews = uniqueRows.Sum(row => row.LifetimeInterviews);
        var capturedHour = JobFinancialSnapshot.TruncateToHour(DateTimeOffset.UtcNow);
        var existing = await _snapshots.GetByUserAndHourAsync(userId, capturedHour, cancellationToken);
        if (existing is null)
        {
            await _snapshots.AddAsync(
                JobFinancialSnapshot.Create(
                    userId,
                    capturedHour,
                    todayPrice,
                    todayTotal,
                    todayApplied,
                    todayInterviews,
                    mainPrice,
                    mainTotal,
                    mainApplied,
                    mainInterviews,
                    archivedPrice,
                    archivedTotal,
                    archivedApplied,
                    archivedInterviews,
                    lifetimePrice,
                    lifetimeTotal,
                    lifetimeApplied,
                    lifetimeInterviews),
                cancellationToken);
        }
        else
        {
            existing.Replace(
                todayPrice,
                todayTotal,
                todayApplied,
                todayInterviews,
                mainPrice,
                mainTotal,
                mainApplied,
                mainInterviews,
                archivedPrice,
                archivedTotal,
                archivedApplied,
                archivedInterviews,
                lifetimePrice,
                lifetimeTotal,
                lifetimeApplied,
                lifetimeInterviews);
        }

        await _snapshots.SaveChangesAsync(cancellationToken);

        foreach (var profile in profiles)
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
                        profile.TodayApplied,
                        profile.TodayInterviews,
                        profile.TodayUnapplied,
                        profile.TodayTotal,
                        profile.TodayPrice),
                    cancellationToken);
            }
            else
            {
                profileExisting.Replace(
                    profile.ProfileTitle,
                    profile.TodayApplied,
                    profile.TodayInterviews,
                    profile.TodayUnapplied,
                    profile.TodayTotal,
                    profile.TodayPrice);
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
                    first.Ready,
                    first.NotReady,
                    first.Total,
                    JobFinancialRules.Price(
                        first.Applied,
                        first.Interviews,
                        first.ApplyRate,
                        first.BonusRate),
                    first.TodayApplied,
                    first.TodayInterviews,
                    first.TodayUnapplied,
                    first.TodayReady,
                    first.TodayNotReady,
                    first.TodayTotal,
                    first.TodayPrice,
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
                item.MainPrice,
                item.MainTotal,
                item.MainApplied,
                item.MainInterviews,
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
        IReadOnlyList<JobStatisticsProfileDto> profiles,
        CancellationToken cancellationToken)
    {
        var since = JobFinancialSnapshot.TruncateToHour(DateTimeOffset.UtcNow)
            .AddHours(-(StatisticsHistoryHours - 1));
        var events = await _statuses.ListEventsSinceAsync(userId, since, cancellationToken);
        var rates = profiles.ToDictionary(
            item => item.ProfileId,
            item => (item.ApplyRate, item.BonusRate, item.ProfileTitle));
        var unappliedByHour = (await _profileSnapshots.ListRecentAsync(
                userId,
                StatisticsHistoryHours,
                cancellationToken))
            .GroupBy(item => (item.ProfileId, item.CapturedHour))
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(item => item.CapturedAt).First().Unapplied);

        var buckets = new Dictionary<(Guid ProfileId, DateTimeOffset Hour), (int Applied, int Interviews)>();
        foreach (var statusEvent in events)
        {
            if (!JobFinancialRules.IsTrackedStatus(statusEvent.Status))
            {
                continue;
            }

            var hour = JobFinancialSnapshot.TruncateToHour(statusEvent.OccurredAt);
            var key = (statusEvent.ProfileId, hour);
            buckets.TryGetValue(key, out var counts);
            if (string.Equals(statusEvent.Status, "Interview", StringComparison.OrdinalIgnoreCase))
            {
                counts.Interviews++;
            }
            else
            {
                counts.Applied++;
            }

            buckets[key] = counts;
        }

        return buckets
            .Select(pair =>
            {
                rates.TryGetValue(pair.Key.ProfileId, out var rate);
                var title = rate.ProfileTitle ?? "Unknown profile";
                var applyRate = rate.ApplyRate;
                var bonusRate = rate.BonusRate;
                unappliedByHour.TryGetValue(pair.Key, out var unapplied);
                return new JobStatisticsPointDto(
                    pair.Key.ProfileId,
                    title,
                    DateOnly.FromDateTime(pair.Key.Hour.UtcDateTime),
                    pair.Key.Hour,
                    pair.Value.Applied,
                    pair.Value.Interviews,
                    unapplied,
                    pair.Value.Applied + pair.Value.Interviews,
                    JobFinancialRules.Price(
                        pair.Value.Applied,
                        pair.Value.Interviews,
                        applyRate,
                        bonusRate));
            })
            .OrderBy(item => item.CapturedHour)
            .ThenBy(item => item.ProfileTitle, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool ShouldEnsureStatusDropdown(string spreadsheetId)
    {
        if (!DropdownCheckedAt.TryGetValue(spreadsheetId, out var checkedAt))
        {
            return true;
        }

        return DateTimeOffset.UtcNow - checkedAt >= DropdownCheckTtl;
    }

    private static bool IsSheetsQuotaExceeded(string message)
    {
        return message.Contains("Quota exceeded", StringComparison.OrdinalIgnoreCase)
            || message.Contains("RATE_LIMIT", StringComparison.OrdinalIgnoreCase)
            || message.Contains("rateLimitExceeded", StringComparison.OrdinalIgnoreCase);
    }
}
