using System.Collections.Concurrent;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Application.Microsoft;
using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed class MailCheckService
{
    private const int BatchSize = 1;
    private const int MaxAlreadySeenPages = 80;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RunLocks = new();
    private static readonly ConcurrentDictionary<(Guid UserId, Guid ConnectionId), string?> ScanCursors = new();

    private static readonly HashSet<string> RecommendedModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-4.1-mini",
        "gpt-4.1"
    };

    private readonly IMailCheckSettingsRepository _settings;
    private readonly IMailCheckProcessedMessageRepository _processed;
    private readonly IMailCheckScanStateRepository _scanStates;
    private readonly IMailConnectionRepository _mailConnections;
    private readonly MailAccessTokenService _mailTokens;
    private readonly IMailMailboxGateway _mailboxes;
    private readonly IMicrosoftOAuthGateway _microsoftOAuth;
    private readonly IGoogleTokenProtector _protector;
    private readonly IOpenAiGateway _openAi;

    public MailCheckService(
        IMailCheckSettingsRepository settings,
        IMailCheckProcessedMessageRepository processed,
        IMailCheckScanStateRepository scanStates,
        IMailConnectionRepository mailConnections,
        MailAccessTokenService mailTokens,
        IMailMailboxGateway mailboxes,
        IMicrosoftOAuthGateway microsoftOAuth,
        IGoogleTokenProtector protector,
        IOpenAiGateway openAi)
    {
        _settings = settings;
        _processed = processed;
        _scanStates = scanStates;
        _mailConnections = mailConnections;
        _mailTokens = mailTokens;
        _mailboxes = mailboxes;
        _microsoftOAuth = microsoftOAuth;
        _protector = protector;
        _openAi = openAi;
    }

    public async Task<MailCheckSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        return await ToSettingsDtoAsync(settings, userId, cancellationToken);
    }

    public async Task<MailCheckSettingsDto> UpdateSettingsAsync(
        Guid userId,
        MailCheckSettingsWriteRequest request,
        CancellationToken cancellationToken)
    {
        var model = (request.Model ?? string.Empty).Trim();
        if (model.Length is 0 or > 128)
        {
            throw new ValidationFailedException("Choose a model.");
        }

        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        settings.SetModel(model);
        if (request.ClearApiKey)
        {
            settings.ClearApiKey();
        }
        else if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            var key = request.ApiKey.Trim();
            if (key.Length < 20)
            {
                throw new ValidationFailedException("OpenAI API key looks too short.");
            }

            settings.SetApiKeyProtected(_protector.Protect(key));
        }

        if (request.ClassifierPrompt is not null)
        {
            var prompt = request.ClassifierPrompt.Trim();
            if (prompt.Length is 0 or > 8000)
            {
                throw new ValidationFailedException("Classifier prompt must be between 1 and 8000 characters.");
            }

            settings.SetClassifierPrompt(prompt);
        }

        if (request.LabelActions is not null)
        {
            var rules = ParseLabelActionsRequest(request.LabelActions);
            settings.SetLabelActionsJson(MailCheckLabelActionRules.Serialize(rules));
        }

        if (request.NeedActionLabels is not null)
        {
            var labels = ParseNeedActionLabelsRequest(request.NeedActionLabels);
            settings.SetNeedActionLabelsJson(MailCheckNeedActionLabels.Serialize(labels));
        }

        if (request.AutoCheckEnabled is bool autoCheckEnabled)
        {
            settings.SetAutoCheckEnabled(autoCheckEnabled);
        }

        await _settings.SaveChangesAsync(cancellationToken);
        if (settings.HasApiKey)
        {
            var connections = await _mailConnections.ListByUserIdAsync(userId, cancellationToken);
            foreach (var connection in connections)
            {
                var access = await _mailTokens.GetAccessAsync(userId, connection.Id, cancellationToken);
                await _mailboxes.Resolve(access.Provider).EnsureLabelsAsync(access.AccessToken, MailCheckLabelCatalog.All, cancellationToken);
            }
        }

        return await ToSettingsDtoAsync(settings, userId, cancellationToken);
    }

    public async Task<MailCheckModelsDto> ListModelsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var apiKey = await RequireApiKeyAsync(userId, cancellationToken);
        var listed = await _openAi.ListModelsAsync(apiKey, cancellationToken);
        var models = listed
            .Select(item => new MailCheckModelDto(item.Id, RecommendedModels.Contains(item.Id)))
            .OrderByDescending(item => item.Recommended)
            .ThenBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new MailCheckModelsDto(models);
    }

    public async Task<MailCheckRunDto> RunAsync(
        Guid userId,
        MailCheckRunRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        if (!request.Force && !settings.AutoCheckEnabled)
        {
            return EmptyRun(false, settings.LastHasMore);
        }

        var cooldown = TimeSpan.FromSeconds(MailCheckAutoCheck.IntervalSeconds);
        if (!request.Force
            && settings.LastRunAt is { } last
            && DateTimeOffset.UtcNow - last < cooldown
            && !settings.LastHasMore)
        {
            return EmptyRun(false, settings.LastHasMore);
        }

        var gate = RunLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        if (!await gate.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            return EmptyRun(true, settings.LastHasMore);
        }

        try
        {
            if (request.ResetCursor)
            {
                ClearCursors(userId, request.MailboxId);
                await _scanStates.ResetAsync(userId, request.MailboxId, cancellationToken);
                await _scanStates.SaveChangesAsync(cancellationToken);
            }

            return await RunLockedAsync(userId, settings, request.MailboxId, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<MailCheckInboxDto> GetInboxAsync(
        Guid userId,
        string? labelSlug,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        var pinLabels = MailCheckLabelActionRules.PinLabels(MailCheckLabelActionRules.Resolve(settings));
        var connections = await RequireConnectionsAsync(userId, null, cancellationToken);
        var filter = MailCheckLabelCatalog.ParseSlug(labelSlug);
        var items = new List<MailCheckInboxItemDto>();
        foreach (var connection in connections)
        {
            var access = await _mailTokens.GetAccessAsync(userId, connection.Id, cancellationToken);
            var mailbox = _mailboxes.Resolve(access.Provider);
            var labels = await mailbox.EnsureLabelsAsync(access.AccessToken, pinLabels, cancellationToken);
            var listed = await mailbox.ListLabeledAsync(access.AccessToken, labels, filter, cancellationToken);
            var providerName = MailConnectionService.ToProviderName(connection.Provider);
            items.AddRange(listed.Select(item => new MailCheckInboxItemDto(
                item.Id,
                item.ThreadId,
                item.Subject,
                item.From,
                item.Date,
                item.Snippet,
                MailCheckLabelCatalog.NameFor(item.Label),
                MailCheckLabelCatalog.SlugFor(item.Label),
                item.Starred,
                connection.Id,
                connection.Email,
                providerName)));
        }

        return new MailCheckInboxDto(
            items
                .OrderByDescending(item => item.Date)
                .ToList());
    }

    public async Task<MailCheckInboxDto> GetNeedActionInboxAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        var labelRules = MailCheckLabelActionRules.Resolve(settings);
        var pinLabels = MailCheckLabelActionRules.PinLabels(labelRules);
        var wanted = MailCheckNeedActionLabels.Resolve(settings)
            .Where(pinLabels.Contains)
            .ToList();
        if (wanted.Count == 0)
        {
            return new MailCheckInboxDto([]);
        }

        var connections = await RequireConnectionsAsync(userId, null, cancellationToken);
        var items = new List<MailCheckInboxItemDto>();
        foreach (var connection in connections)
        {
            var access = await _mailTokens.GetAccessAsync(userId, connection.Id, cancellationToken);
            var mailbox = _mailboxes.Resolve(access.Provider);
            var labels = await mailbox.EnsureLabelsAsync(access.AccessToken, pinLabels, cancellationToken);
            var providerName = MailConnectionService.ToProviderName(connection.Provider);
            foreach (var label in wanted)
            {
                var listed = await mailbox.ListLabeledAsync(access.AccessToken, labels, label, cancellationToken);
                items.AddRange(listed.Select(item => new MailCheckInboxItemDto(
                    item.Id,
                    item.ThreadId,
                    item.Subject,
                    item.From,
                    item.Date,
                    item.Snippet,
                    MailCheckLabelCatalog.NameFor(item.Label),
                    MailCheckLabelCatalog.SlugFor(item.Label),
                    item.Starred,
                    connection.Id,
                    connection.Email,
                    providerName)));
            }
        }

        return new MailCheckInboxDto(
            items
                .OrderByDescending(item => item.Date)
                .ToList());
    }

    private async Task<MailCheckRunDto> RunLockedAsync(
        Guid userId,
        MailCheckSettings settings,
        Guid? mailboxId,
        CancellationToken cancellationToken)
    {
        if (!settings.HasApiKey)
        {
            throw new ValidationFailedException("Save an OpenAI API key on Mail Check Settings.");
        }

        var connections = await RequireConnectionsAsync(userId, mailboxId, cancellationToken);
        var apiKey = _protector.Unprotect(settings.ApiKeyProtected!);
        var classifierPrompt = ResolveClassifierPrompt(settings);
        var labelRules = MailCheckLabelActionRules.Resolve(settings);
        var items = new List<MailCheckRunItemDto>();
        var labeled = 0;
        var trashed = 0;
        var skipped = 0;
        var errors = 0;
        var processed = 0;
        var scanned = 0;
        var alreadySeen = 0;
        var hasMore = false;
        Guid? activeMailboxId = null;
        string? activeMailboxEmail = null;
        string? activeMailboxProvider = null;

        try
        {
            for (var index = 0; index < connections.Count; index++)
            {
                if (processed >= BatchSize)
                {
                    hasMore = true;
                    break;
                }

                var connection = connections[index];
                var providerName = MailConnectionService.ToProviderName(connection.Provider);
                activeMailboxId = connection.Id;
                activeMailboxEmail = connection.Email;
                activeMailboxProvider = providerName;
                var access = await _mailTokens.GetAccessAsync(userId, connection.Id, cancellationToken);
                var mailbox = _mailboxes.Resolve(access.Provider);
                var labels = await mailbox.EnsureLabelsAsync(access.AccessToken, MailCheckLabelCatalog.All, cancellationToken);
                var ourLabelIds = labels.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var cursorKey = (userId, connection.Id);
                ScanCursors.TryGetValue(cursorKey, out var pageToken);
                var alreadySeenPages = 0;
                var mailboxHasMore = false;

                while (processed < BatchSize)
                {
                    var pageTokenUsed = pageToken;
                    var batch = await mailbox.ListCandidatesAsync(access.AccessToken, pageTokenUsed, cancellationToken);
                    if (batch.Messages.Count == 0)
                    {
                        ScanCursors.TryRemove(cursorKey, out _);
                        await MarkMailboxCaughtUpAsync(userId, connection.Id, cancellationToken);
                        break;
                    }

                    scanned += batch.Messages.Count;
                    var existing = await _processed.FindExistingAsync(
                        connection.Id,
                        batch.Messages.Select(item => item.Id).ToList(),
                        cancellationToken);
                    var fresh = batch.Messages.Where(item => !existing.Contains(item.Id)).ToList();
                    alreadySeen += batch.Messages.Count - fresh.Count;

                    if (fresh.Count == 0)
                    {
                        if (string.IsNullOrWhiteSpace(batch.NextPageToken))
                        {
                            ScanCursors.TryRemove(cursorKey, out _);
                            await MarkMailboxCaughtUpAsync(userId, connection.Id, cancellationToken);
                            break;
                        }

                        pageToken = batch.NextPageToken;
                        ScanCursors[cursorKey] = pageToken;
                        alreadySeenPages++;
                        if (alreadySeenPages >= MaxAlreadySeenPages)
                        {
                            mailboxHasMore = true;
                            break;
                        }

                        continue;
                    }

                    alreadySeenPages = 0;
                    var finishedPage = true;
                    foreach (var candidate in fresh)
                    {
                        if (processed >= BatchSize)
                        {
                            finishedPage = false;
                            mailboxHasMore = true;
                            break;
                        }

                        try
                        {
                            var message = await mailbox.GetMessageAsync(access.AccessToken, candidate.Id, cancellationToken);
                            if (message.LabelIds.Any(ourLabelIds.Contains))
                            {
                                await RecordMessageScanAsync(userId, connection.Id, message, cancellationToken);
                                await _processed.AddAsync(
                                    MailCheckProcessedMessage.Create(
                                        userId,
                                        connection.Id,
                                        message.Id,
                                        MailCheckLabel.Other),
                                    cancellationToken);
                                items.Add(new MailCheckRunItemDto(
                                    message.Id,
                                    message.Subject,
                                    message.From,
                                    "keep",
                                    string.Empty,
                                    "Already checked",
                                    connection.Id,
                                    connection.Email,
                                    providerName));
                                skipped++;
                                processed++;
                                continue;
                            }

                            var classification = await _openAi.ClassifyAsync(
                                apiKey,
                                settings.Model,
                                classifierPrompt,
                                FormatMail(message),
                                cancellationToken);
                            await RecordMessageScanAsync(userId, connection.Id, message, cancellationToken);
                            await ApplyAsync(
                                userId,
                                connection,
                                providerName,
                                mailbox,
                                access.AccessToken,
                                message,
                                labels,
                                labelRules,
                                classification,
                                items,
                                cancellationToken);
                            var mailboxAction = labelRules[classification.Label];
                            if (mailboxAction == MailCheckMailboxAction.Trash)
                            {
                                trashed++;
                            }
                            else if (mailboxAction == MailCheckMailboxAction.Keep)
                            {
                                skipped++;
                            }
                            else
                            {
                                labeled++;
                            }

                            processed++;
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            errors++;
                            items.Add(new MailCheckRunItemDto(
                                candidate.Id,
                                string.Empty,
                                string.Empty,
                                "error",
                                exception.Message,
                                string.Empty,
                                connection.Id,
                                connection.Email,
                                providerName));
                        }
                    }

                    if (!finishedPage)
                    {
                        ScanCursors[cursorKey] = pageTokenUsed;
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(batch.NextPageToken))
                    {
                        ScanCursors.TryRemove(cursorKey, out _);
                        await MarkMailboxCaughtUpAsync(userId, connection.Id, cancellationToken);
                        break;
                    }

                    pageToken = batch.NextPageToken;
                    ScanCursors[cursorKey] = pageToken;
                }

                if (mailboxHasMore
                    || ScanCursors.ContainsKey(cursorKey)
                    || (processed >= BatchSize && index < connections.Count - 1))
                {
                    hasMore = true;
                }
            }

            settings.RecordRun(labeled, trashed, skipped, processed, errors, hasMore, string.Empty);
        }
        catch (Exception exception) when (exception is ValidationFailedException or GoogleOAuthException or MicrosoftOAuthException)
        {
            settings.RecordRun(labeled, trashed, skipped, processed, errors + 1, hasMore, exception.Message);
            await _processed.SaveChangesAsync(cancellationToken);
            await _scanStates.SaveChangesAsync(cancellationToken);
            await _settings.SaveChangesAsync(cancellationToken);
            throw;
        }

        await _processed.SaveChangesAsync(cancellationToken);
        await _scanStates.SaveChangesAsync(cancellationToken);
        await _settings.SaveChangesAsync(cancellationToken);
        return new MailCheckRunDto(
            false,
            processed,
            labeled,
            trashed,
            skipped,
            errors,
            hasMore,
            scanned,
            alreadySeen,
            activeMailboxId,
            activeMailboxEmail,
            activeMailboxProvider,
            items);
    }

    private async Task ApplyAsync(
        Guid userId,
        MailConnection connection,
        string providerName,
        IMailMailbox mailbox,
        string token,
        MailMessageContent message,
        IReadOnlyDictionary<MailCheckLabel, string> labels,
        IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> labelRules,
        MailCheckClassification classification,
        List<MailCheckRunItemDto> items,
        CancellationToken cancellationToken)
    {
        if (!labels.TryGetValue(classification.Label, out var labelId))
        {
            throw new ValidationFailedException("Mail Check labels are missing.");
        }

        var mailboxAction = labelRules[classification.Label];
        var star = mailboxAction == MailCheckMailboxAction.Pin
            && string.Equals(providerName, "gmail", StringComparison.OrdinalIgnoreCase);

        await mailbox.ApplyLabelAsync(token, message.Id, labelId, message.LabelIds, star, cancellationToken);

        if (mailboxAction == MailCheckMailboxAction.Trash)
        {
            await mailbox.TrashAsync(token, message.Id, cancellationToken);
        }

        await _processed.AddAsync(
            MailCheckProcessedMessage.Create(userId, connection.Id, message.Id, classification.Label),
            cancellationToken);
        items.Add(new MailCheckRunItemDto(
            message.Id,
            message.Subject,
            message.From,
            MailCheckLabelActionRules.ActionSlug(mailboxAction),
            string.Empty,
            MailCheckLabelCatalog.NameFor(classification.Label),
            connection.Id,
            connection.Email,
            providerName));
    }

    private async Task<IReadOnlyList<MailConnection>> RequireConnectionsAsync(
        Guid userId,
        Guid? mailboxId,
        CancellationToken cancellationToken)
    {
        var connections = await _mailConnections.ListByUserIdAsync(userId, cancellationToken);
        if (mailboxId is Guid id)
        {
            connections = connections.Where(connection => connection.Id == id).ToList();
            if (connections.Count == 0)
            {
                throw new ValidationFailedException("That mailbox is not connected.");
            }

            return connections;
        }

        if (connections.Count == 0)
        {
            throw new ValidationFailedException("Connect a mailbox on Mail Check Settings first.");
        }

        return connections;
    }

    private static void ClearCursors(Guid userId, Guid? mailboxId)
    {
        foreach (var key in ScanCursors.Keys.Where(key => key.UserId == userId).ToList())
        {
            if (mailboxId is Guid id && key.ConnectionId != id)
            {
                continue;
            }

            ScanCursors.TryRemove(key, out _);
        }
    }

    private async Task<MailCheckSettings> GetOrCreateSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await _settings.GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = MailCheckSettings.Create(userId);
        await _settings.AddAsync(created, cancellationToken);
        await _settings.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<string> RequireApiKeyAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        if (!settings.HasApiKey)
        {
            throw new ValidationFailedException("Save an OpenAI API key on Mail Check Settings.");
        }

        return _protector.Unprotect(settings.ApiKeyProtected!);
    }

    private async Task<MailCheckSettingsDto> ToSettingsDtoAsync(
        MailCheckSettings settings,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var connections = await _mailConnections.ListByUserIdAsync(userId, cancellationToken);
        var outlookAvailable = await _microsoftOAuth.IsConfiguredAsync(cancellationToken);
        var labelRules = MailCheckLabelActionRules.Resolve(settings);
        var needAction = MailCheckNeedActionLabels.Resolve(settings);
        var scanStates = await _scanStates.ListByConnectionIdsAsync(
            userId,
            connections.Select(connection => connection.Id).ToList(),
            cancellationToken);
        var mailboxes = connections
            .Select(connection =>
            {
                scanStates.TryGetValue(connection.Id, out var scan);
                return new MailMailboxItemDto(
                    connection.Id,
                    MailConnectionService.ToProviderName(connection.Provider),
                    connection.Email,
                    connection.ConnectedAt,
                    scan?.CheckedUntilAt,
                    scan?.LastScanAt,
                    scan?.ScanCaughtUp ?? false);
            })
            .ToList();
        return new MailCheckSettingsDto(
            settings.HasApiKey,
            settings.Model,
            ResolveClassifierPrompt(settings),
            MailCheckClassifierPrompt.Default,
            MailCheckLabelActionRules.ToSlugMap(labelRules),
            MailCheckLabelActionRules.ToSlugMap(MailCheckLabelActionRules.Parse(MailCheckSettings.DefaultLabelActionsJson)),
            MailCheckNeedActionLabels.ToSlugList(needAction),
            MailCheckNeedActionLabels.ToSlugList(MailCheckNeedActionLabels.Default),
            settings.AutoCheckEnabled,
            MailCheckAutoCheck.IntervalSeconds,
            settings.LastRunAt,
            settings.LastError,
            settings.LastLabeled,
            settings.LastTrashed,
            settings.LastSkipped,
            settings.LastProcessed,
            settings.LastErrors,
            settings.LastHasMore,
            settings.TotalLabeled,
            settings.TotalTrashed,
            mailboxes,
            outlookAvailable);
    }

    private async Task RecordMessageScanAsync(
        Guid userId,
        Guid connectionId,
        MailMessageContent message,
        CancellationToken cancellationToken)
    {
        var state = await _scanStates.GetOrCreateAsync(userId, connectionId, cancellationToken);
        if (TryParseMessageDate(message.Date, out var parsed))
        {
            state.RecordMessageChecked(parsed);
            return;
        }

        state.TouchScan();
    }

    private async Task MarkMailboxCaughtUpAsync(
        Guid userId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var state = await _scanStates.GetOrCreateAsync(userId, connectionId, cancellationToken);
        state.MarkCaughtUp();
    }

    private static bool TryParseMessageDate(string raw, out DateTimeOffset parsed)
    {
        return DateTimeOffset.TryParse(raw, out parsed);
    }

    private static string ResolveClassifierPrompt(MailCheckSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.ClassifierPrompt)
            ? MailCheckClassifierPrompt.Default
            : settings.ClassifierPrompt.Trim();
    }

    private static IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> ParseLabelActionsRequest(
        IReadOnlyDictionary<string, string> labelActions)
    {
        var rules = MailCheckLabelActionRules.Parse(MailCheckSettings.DefaultLabelActionsJson);
        var merged = new Dictionary<MailCheckLabel, MailCheckMailboxAction>(rules);
        foreach (var label in MailCheckLabelCatalog.All)
        {
            var slug = MailCheckLabelCatalog.SlugFor(label);
            if (!labelActions.TryGetValue(slug, out var actionRaw))
            {
                throw new ValidationFailedException($"Choose an action for {MailCheckLabelCatalog.NameFor(label)}.");
            }

            var action = MailCheckLabelActionRules.ParseActionSlug(actionRaw);
            if (action is null)
            {
                throw new ValidationFailedException($"Choose pin, trash, or keep for {MailCheckLabelCatalog.NameFor(label)}.");
            }

            merged[label] = action.Value;
        }

        return merged;
    }

    private static IReadOnlyList<MailCheckLabel> ParseNeedActionLabelsRequest(IReadOnlyList<string> needActionLabels)
    {
        if (needActionLabels.Count == 0)
        {
            throw new ValidationFailedException("Choose at least one label for Need action.");
        }

        var labels = new List<MailCheckLabel>();
        foreach (var slug in needActionLabels)
        {
            var label = MailCheckLabelCatalog.ParseSlug(slug);
            if (label is null)
            {
                throw new ValidationFailedException("Need action includes an unknown label.");
            }

            if (!labels.Contains(label.Value))
            {
                labels.Add(label.Value);
            }
        }

        return labels;
    }

    private static MailCheckRunDto EmptyRun(bool busy, bool hasMore)
    {
        return new MailCheckRunDto(busy, 0, 0, 0, 0, 0, hasMore, 0, 0, null, null, null, []);
    }

    private static string FormatMail(MailMessageContent message)
    {
        return $"From: {message.From}\nDate: {message.Date}\nSubject: {message.Subject}\n\n{message.Body}";
    }
}
