using System.Diagnostics;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Application.Microsoft;
using Flexis.Domain.MailCheck;
using Microsoft.Extensions.Logging;

namespace Flexis.Application.MailCheck;

public sealed class MailCheckService
{
    private const int BatchSize = MailCheckAutoCheck.RunBatchSize;
    private const int MaxCandidatePagesPerRun = 64;

    private static readonly HashSet<string> RecommendedModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-4.1-mini",
        "gpt-4.1"
    };

    private readonly IMailCheckSettingsRepository _settings;
    private readonly IMailCheckProcessedMessageRepository _processed;
    private readonly IMailCheckActionLogRepository _actionLogs;
    private readonly IMailCheckScanStateRepository _scanStates;
    private readonly IMailConnectionRepository _mailConnections;
    private readonly MailAccessTokenService _mailTokens;
    private readonly IMailMailboxGateway _mailboxes;
    private readonly IMicrosoftOAuthGateway _microsoftOAuth;
    private readonly IGoogleTokenProtector _protector;
    private readonly IOpenAiGateway _openAi;
    private readonly MailCheckUsageService _usage;
    private readonly MailCheckRunProgressStore _runProgress;
    private readonly MailCheckRunCoordinator _runCoordinator;
    private readonly ILogger<MailCheckService> _logger;

    public MailCheckService(
        IMailCheckSettingsRepository settings,
        IMailCheckProcessedMessageRepository processed,
        IMailCheckActionLogRepository actionLogs,
        IMailCheckScanStateRepository scanStates,
        IMailConnectionRepository mailConnections,
        MailAccessTokenService mailTokens,
        IMailMailboxGateway mailboxes,
        IMicrosoftOAuthGateway microsoftOAuth,
        IGoogleTokenProtector protector,
        IOpenAiGateway openAi,
        MailCheckUsageService usage,
        MailCheckRunProgressStore runProgress,
        MailCheckRunCoordinator runCoordinator,
        ILogger<MailCheckService> logger)
    {
        _settings = settings;
        _processed = processed;
        _actionLogs = actionLogs;
        _scanStates = scanStates;
        _mailConnections = mailConnections;
        _mailTokens = mailTokens;
        _mailboxes = mailboxes;
        _microsoftOAuth = microsoftOAuth;
        _protector = protector;
        _openAi = openAi;
        _usage = usage;
        _runProgress = runProgress;
        _runCoordinator = runCoordinator;
        _logger = logger;
    }

    public MailCheckRunProgressDto GetRunProgress(Guid userId)
    {
        var progress = _runProgress.Get(userId);
        var lockStatus = _runCoordinator.GetStatus(userId);
        if (lockStatus.WaitingForLock)
        {
            var blocker = lockStatus.ActiveRunKind == "auto"
                ? "Auto-check is finishing its current server round."
                : lockStatus.ActiveRunKind == "manual"
                    ? "Another manual check is finishing its current server round."
                    : "Another Mail Check run is finishing.";
            return progress with
            {
                Active = true,
                Stage = "lock",
                Message = lockStatus.WaitingRequestKind == "manual"
                    ? $"Waiting for server lock. {blocker}"
                    : $"Waiting for server lock. {blocker}",
                WaitingForLock = true,
                ActiveRunKind = lockStatus.ActiveRunKind,
                WaitingRequestKind = lockStatus.WaitingRequestKind,
            };
        }

        return progress with
        {
            Active = progress.Active || lockStatus.RunActive,
            ActiveRunKind = lockStatus.ActiveRunKind ?? progress.ActiveRunKind,
        };
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

        var manual = request.Force;
        MailCheckRunHandle? handle = null;
        var progressGeneration = 0;
        try
        {
            var (acquired, lockMs) = await _runCoordinator.TryAcquireAsync(userId, manual, cancellationToken);
            handle = acquired;
            if (handle is null)
            {
                var status = _runCoordinator.GetStatus(userId);
                _logger.LogWarning(
                    "MailCheck lock timeout user={UserId} manual={Manual} activeRun={ActiveRun} waitedMs={LockMs}",
                    userId,
                    manual,
                    status.ActiveRunKind ?? "unknown",
                    lockMs);
                return EmptyRun(true, settings.LastHasMore);
            }

            progressGeneration = handle.ProgressGeneration;
            if (request.ResetCursor)
            {
                await _scanStates.ResetAsync(userId, request.MailboxId, cancellationToken);
                await _scanStates.SaveChangesAsync(cancellationToken);
            }

            return await RunLockedAsync(
                userId,
                settings,
                request.MailboxId,
                manual,
                lockMs,
                progressGeneration,
                handle.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "MailCheck run cancelled user={UserId} manual={Manual}",
                userId,
                manual);
            return EmptyRun(false, settings.LastHasMore);
        }
        finally
        {
            if (progressGeneration > 0)
            {
                _runProgress.End(userId, progressGeneration);
            }

            handle?.Dispose();
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
            try
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
            catch (Exception exception) when (exception is GoogleOAuthException or MicrosoftOAuthException)
            {
                _logger.LogWarning(
                    exception,
                    "MailCheck inbox skipped user={UserId} mailbox={Mailbox}",
                    userId,
                    connection.Email);
            }
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
            try
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
            catch (Exception exception) when (exception is GoogleOAuthException or MicrosoftOAuthException)
            {
                _logger.LogWarning(
                    exception,
                    "MailCheck need-action skipped user={UserId} mailbox={Mailbox}",
                    userId,
                    connection.Email);
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
        bool manual,
        int lockMs,
        int progressGeneration,
        CancellationToken cancellationToken)
    {
        if (!settings.HasApiKey)
        {
            throw new ValidationFailedException("Save an OpenAI API key on Mail Check Settings.");
        }

        var connections = await RequireConnectionsAsync(userId, mailboxId, cancellationToken);
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
        var timing = new MailCheckRunTiming();
        timing.AddLockMs(lockMs);
        var runId = Guid.NewGuid();
        var source = manual ? "manual" : "auto";
        var runClock = Stopwatch.StartNew();
        _runProgress.Begin(
            userId,
            progressGeneration,
            lockMs > 0 ? $"Acquired server lock after {lockMs} ms" : "Acquired server lock");

        void Report(string stage, string message, string? mailboxEmail, int scanPage = 0)
        {
            ReportRunProgress(
                userId,
                progressGeneration,
                stage,
                message,
                mailboxEmail,
                processed,
                scanned,
                alreadySeen,
                scanPage);
        }

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
                var mailbox = _mailboxes.Resolve(connection.Provider);
                Report("token", $"Refreshing access token for {connection.Email}", connection.Email);
                var access = await timing.TrackTokenAsync(() =>
                    _mailTokens.GetAccessAsync(userId, connection.Id, cancellationToken));
                Report("labels", $"Ensuring Flexis labels on {connection.Email}", connection.Email);
                var labels = await timing.TrackLabelsAsync(() =>
                    mailbox.EnsureLabelsAsync(
                        access.AccessToken,
                        MailCheckLabelCatalog.All,
                        cancellationToken));
                var ourLabelIds = labels.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
                settings = await GetFreshSettingsAsync(userId, cancellationToken);
                var labelRules = MailCheckLabelActionRules.Resolve(settings);
                Report("apply", $"Enforcing trash actions on labeled mail in {connection.Email}", connection.Email);
                var enforced = await timing.TrackApplyAsync(() => EnforceTrashLabelsAsync(
                    userId,
                    connection,
                    providerName,
                    mailbox,
                    access.AccessToken,
                    labels,
                    labelRules,
                    items,
                    runId,
                    source,
                    cancellationToken));
                trashed += enforced;
                processed += enforced;
                if (processed >= BatchSize)
                {
                    hasMore = true;
                    break;
                }

                var mailboxHasMore = false;
                var mailboxCaughtUp = false;
                var skipMessageIds = new HashSet<string>(StringComparer.Ordinal);
                var handledSkips = 0;

                while (processed < BatchSize && handledSkips < MailCheckAutoCheck.MaxAlreadyHandledSkipsPerRun)
                {
                    settings = await GetFreshSettingsAsync(userId, cancellationToken);
                    var apiKey = _protector.Unprotect(settings.ApiKeyProtected!);
                    var classifierPrompt = ResolveClassifierPrompt(settings);
                    labelRules = MailCheckLabelActionRules.Resolve(settings);

                    Report("scan", $"Scanning inbox on {connection.Email} for unlabeled mail", connection.Email);
                    var search = await timing.TrackScanAsync(() => FindNewestFreshCandidateAsync(
                        mailbox,
                        access.AccessToken,
                        connection.Id,
                        skipMessageIds,
                        (scanPage, scannedSoFar) => Report(
                            "scan",
                            $"Scanning inbox page {scanPage} on {connection.Email} — checked {scannedSoFar} candidates",
                            connection.Email,
                            scanPage),
                        cancellationToken));
                    scanned += search.Scanned;
                    alreadySeen += search.AlreadySeen;

                    if (search.Candidate is null)
                    {
                        if (search.CaughtUp)
                        {
                            mailboxCaughtUp = true;
                            await MarkMailboxCaughtUpAsync(userId, connection.Id, cancellationToken);
                        }

                        if (search.HasMorePending)
                        {
                            mailboxHasMore = true;
                        }

                        break;
                    }

                    try
                    {
                        var candidate = search.Candidate;
                        var messageClock = Stopwatch.StartNew();
                        Report("fetch", $"Downloading mail from {connection.Email}", connection.Email);
                        MailMessageContent message;
                        try
                        {
                            message = await timing.TrackFetchAsync(() =>
                                mailbox.GetMessageAsync(access.AccessToken, candidate.Id, cancellationToken));
                        }
                        catch (Exception exception) when (MailCheckMailboxErrors.IsMissingMessage(exception))
                        {
                            await RecordMissingMessageAsync(userId, connection.Id, candidate.Id, cancellationToken);
                            skipMessageIds.Add(candidate.Id);
                            handledSkips++;
                            alreadySeen++;
                            continue;
                        }

                        var handledBeforeClassify = false;
                        var trashedBeforeClassify = false;
                        await timing.TrackApplyAsync(async () =>
                        {
                            (handledBeforeClassify, trashedBeforeClassify) = await TryHandleAlreadyCheckedAsync(
                                userId,
                                connection,
                                providerName,
                                mailbox,
                                access.AccessToken,
                                message,
                                labels,
                                labelRules,
                                ourLabelIds,
                                items,
                                runId,
                                source,
                                (int)messageClock.ElapsedMilliseconds,
                                cancellationToken);
                        });
                        if (handledBeforeClassify)
                        {
                            if (trashedBeforeClassify)
                            {
                                trashed++;
                            }

                            skipMessageIds.Add(message.Id);
                            handledSkips++;
                            alreadySeen++;
                            continue;
                        }

                        Report("classify", $"Classifying mail on {connection.Email} with OpenAI", connection.Email);
                        MailCheckClassification classification;
                        var classifyDetail = string.Empty;
                        try
                        {
                            classification = await timing.TrackClassifyAsync(() => _openAi.ClassifyAsync(
                                apiKey,
                                settings.Model,
                                classifierPrompt,
                                MailCheckMailText.FormatForClassification(message),
                                cancellationToken));
                            await _usage.RecordClassifyAsync(
                                userId,
                                settings.Model,
                                classification.Usage,
                                cancellationToken);
                        }
                        catch (ValidationFailedException classifyException)
                        {
                            _logger.LogWarning(
                                classifyException,
                                "MailCheck classify fallback user={UserId} mailbox={Mailbox} message={MessageId}",
                                userId,
                                connection.Email,
                                message.Id);
                            classification = new MailCheckClassification(
                                MailCheckLabel.Other,
                                OpenAiTokenUsage.Empty);
                            classifyDetail = $"Classifier fallback to Other: {classifyException.Message}";
                        }

                        try
                        {
                            message = await timing.TrackFetchAsync(() =>
                                mailbox.GetMessageAsync(access.AccessToken, candidate.Id, cancellationToken));
                        }
                        catch (Exception exception) when (MailCheckMailboxErrors.IsMissingMessage(exception))
                        {
                            await RecordMissingMessageAsync(userId, connection.Id, candidate.Id, cancellationToken);
                            skipMessageIds.Add(candidate.Id);
                            handledSkips++;
                            alreadySeen++;
                            continue;
                        }

                        var handledAfterClassify = false;
                        var trashedAfterClassify = false;
                        await timing.TrackApplyAsync(async () =>
                        {
                            (handledAfterClassify, trashedAfterClassify) = await TryHandleAlreadyCheckedAsync(
                                userId,
                                connection,
                                providerName,
                                mailbox,
                                access.AccessToken,
                                message,
                                labels,
                                labelRules,
                                ourLabelIds,
                                items,
                                runId,
                                source,
                                (int)messageClock.ElapsedMilliseconds,
                                cancellationToken);
                        });
                        if (handledAfterClassify)
                        {
                            if (trashedAfterClassify)
                            {
                                trashed++;
                            }

                            skipMessageIds.Add(message.Id);
                            handledSkips++;
                            alreadySeen++;
                            continue;
                        }

                        Report("apply", $"Applying label action on {connection.Email}", connection.Email);
                        await timing.TrackApplyAsync(async () =>
                        {
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
                                runId,
                                source,
                                (int)messageClock.ElapsedMilliseconds,
                                classifyDetail,
                                cancellationToken);
                        });
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
                        handledSkips = 0;
                        Report(
                            "apply",
                            $"Classified {processed} message{(processed == 1 ? "" : "s")} this round on {connection.Email}",
                            connection.Email);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        if (MailCheckMailboxErrors.IsMissingMessage(exception))
                        {
                            await RecordMissingMessageAsync(userId, connection.Id, search.Candidate.Id, cancellationToken);
                            skipMessageIds.Add(search.Candidate.Id);
                            handledSkips++;
                            alreadySeen++;
                            continue;
                        }

                        errors++;
                        await RecordMissingMessageAsync(userId, connection.Id, search.Candidate.Id, cancellationToken);
                        skipMessageIds.Add(search.Candidate.Id);
                        handledSkips++;
                        alreadySeen++;
                        items.Add(new MailCheckRunItemDto(
                            search.Candidate.Id,
                            string.Empty,
                            string.Empty,
                            "error",
                            exception.Message,
                            string.Empty,
                            connection.Id,
                            connection.Email,
                            providerName));
                        await AppendMessageLogAsync(
                            userId,
                            runId,
                            source,
                            connection.Id,
                            connection.Email,
                            providerName,
                            search.Candidate.Id,
                            string.Empty,
                            string.Empty,
                            "error",
                            string.Empty,
                            exception.Message,
                            0,
                            cancellationToken);
                        continue;
                    }
                }

                if (mailboxHasMore
                    || handledSkips >= MailCheckAutoCheck.MaxAlreadyHandledSkipsPerRun
                    || (!mailboxCaughtUp && processed < BatchSize && scanned > 0)
                    || (processed >= BatchSize && index < connections.Count - 1)
                    || (processed >= BatchSize && !mailboxCaughtUp))
                {
                    hasMore = true;
                }
            }

            settings.RecordRun(labeled, trashed, skipped, processed, errors, hasMore, string.Empty);
            var timingDto = timing.ToDto();
            await AppendRunSummaryLogAsync(
                userId,
                runId,
                source,
                processed,
                labeled,
                trashed,
                skipped,
                errors,
                scanned,
                alreadySeen,
                hasMore,
                timingDto,
                (int)runClock.ElapsedMilliseconds,
                activeMailboxEmail,
                activeMailboxProvider,
                cancellationToken);
        }
        catch (Exception exception) when (exception is ValidationFailedException or GoogleOAuthException or MicrosoftOAuthException)
        {
            settings.RecordRun(labeled, trashed, skipped, processed, errors + 1, hasMore, exception.Message);
            await AppendRunSummaryLogAsync(
                userId,
                runId,
                source,
                processed,
                labeled,
                trashed,
                skipped,
                errors + 1,
                scanned,
                alreadySeen,
                hasMore,
                timing.ToDto(),
                (int)runClock.ElapsedMilliseconds,
                activeMailboxEmail,
                activeMailboxProvider,
                cancellationToken,
                exception.Message);
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
            items,
            timing.ToDto());
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
        Guid runId,
        string source,
        int durationMs,
        string detail,
        CancellationToken cancellationToken)
    {
        if (!labels.TryGetValue(classification.Label, out var labelId))
        {
            throw new ValidationFailedException("Mail Check labels are missing.");
        }

        var mailboxAction = labelRules[classification.Label];
        var pin = mailboxAction == MailCheckMailboxAction.Pin;
        var isGmail = string.Equals(providerName, "gmail", StringComparison.OrdinalIgnoreCase);

        await mailbox.ApplyLabelAsync(token, message.Id, labelId, message.LabelIds, pin && isGmail, pin, cancellationToken);
        await mailbox.MarkAsReadAsync(token, message.Id, message.LabelIds, cancellationToken);

        if (mailboxAction == MailCheckMailboxAction.Trash && !MailCheckMessageState.IsTrashed(message))
        {
            await mailbox.TrashAsync(token, message.Id, cancellationToken);
        }

        await _processed.UpsertAsync(
            MailCheckProcessedMessage.Create(userId, connection.Id, message.Id, classification.Label),
            cancellationToken);
        var action = MailCheckLabelActionRules.ActionSlug(mailboxAction);
        var labelName = MailCheckLabelCatalog.NameFor(classification.Label);
        items.Add(new MailCheckRunItemDto(
            message.Id,
            message.Subject,
            message.From,
            action,
            detail,
            labelName,
            connection.Id,
            connection.Email,
            providerName));
        await AppendMessageLogAsync(
            userId,
            runId,
            source,
            connection.Id,
            connection.Email,
            providerName,
            message.Id,
            message.Subject,
            message.From,
            action,
            labelName,
            detail,
            durationMs,
            cancellationToken);
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

    private async Task<MailCheckSettings> GetFreshSettingsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(userId, cancellationToken);
        await _settings.ReloadAsync(settings, cancellationToken);
        return settings;
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
                    scan?.CheckedNewestAt,
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

    private static MailCheckLabel? ResolveLabelOnMessage(
        IReadOnlyList<string> messageLabelIds,
        IReadOnlyDictionary<MailCheckLabel, string> labels)
    {
        foreach (var (label, id) in labels)
        {
            if (messageLabelIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            {
                return label;
            }
        }

        return null;
    }

    private async Task RecordMissingMessageAsync(
        Guid userId,
        Guid connectionId,
        string messageId,
        CancellationToken cancellationToken)
    {
        await _processed.UpsertAsync(
            MailCheckProcessedMessage.Create(userId, connectionId, messageId, MailCheckLabel.Other),
            cancellationToken);
    }

    private async Task<(bool Handled, bool Trashed)> TryHandleAlreadyCheckedAsync(
        Guid userId,
        MailConnection connection,
        string providerName,
        IMailMailbox mailbox,
        string accessToken,
        MailMessageContent message,
        IReadOnlyDictionary<MailCheckLabel, string> labels,
        IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> labelRules,
        IReadOnlySet<string> ourLabelIds,
        List<MailCheckRunItemDto> items,
        Guid runId,
        string source,
        int durationMs,
        CancellationToken cancellationToken)
    {
        if (!message.LabelIds.Any(ourLabelIds.Contains))
        {
            return (false, false);
        }

        var existingLabel = ResolveLabelOnMessage(message.LabelIds, labels);
        MailCheckMailboxAction? action = existingLabel is MailCheckLabel matchedLabel
            ? labelRules[matchedLabel]
            : null;
        var trashedNow = false;
        if (existingLabel is MailCheckLabel labelMatch
            && action == MailCheckMailboxAction.Trash
            && !MailCheckMessageState.IsTrashed(message))
        {
            await mailbox.MarkAsReadAsync(accessToken, message.Id, message.LabelIds, cancellationToken);
            await mailbox.TrashAsync(accessToken, message.Id, cancellationToken);
            trashedNow = true;
        }
        else if (existingLabel is MailCheckLabel pinLabel
            && labelRules[pinLabel] == MailCheckMailboxAction.Pin
            && labels.TryGetValue(pinLabel, out var pinLabelId))
        {
            await mailbox.ApplyLabelAsync(
                accessToken,
                message.Id,
                pinLabelId,
                message.LabelIds,
                string.Equals(providerName, "gmail", StringComparison.OrdinalIgnoreCase),
                true,
                cancellationToken);
            await mailbox.MarkAsReadAsync(accessToken, message.Id, message.LabelIds, cancellationToken);
        }
        else
        {
            await mailbox.MarkAsReadAsync(accessToken, message.Id, message.LabelIds, cancellationToken);
        }

        await RecordMessageScanAsync(userId, connection.Id, message, cancellationToken);
        await _processed.UpsertAsync(
            MailCheckProcessedMessage.Create(
                userId,
                connection.Id,
                message.Id,
                existingLabel ?? MailCheckLabel.Other),
            cancellationToken);
        var actionSlug = trashedNow ? "trash" : "already_checked";
        var labelName = existingLabel is MailCheckLabel named ? MailCheckLabelCatalog.NameFor(named) : "Already labeled";
        items.Add(new MailCheckRunItemDto(
            message.Id,
            message.Subject,
            message.From,
            actionSlug,
            string.Empty,
            labelName,
            connection.Id,
            connection.Email,
            providerName));
        await AppendMessageLogAsync(
            userId,
            runId,
            source,
            connection.Id,
            connection.Email,
            providerName,
            message.Id,
            message.Subject,
            message.From,
            actionSlug,
            labelName,
            trashedNow ? "Already labeled; trash action enforced" : "Already labeled; skipped classification",
            durationMs,
            cancellationToken);
        return (true, trashedNow);
    }

    private sealed record FreshCandidateSearch(
        MailMessageRef? Candidate,
        int Scanned,
        int AlreadySeen,
        bool CaughtUp,
        bool HasMorePending);

    private async Task<FreshCandidateSearch> FindNewestFreshCandidateAsync(
        IMailMailbox mailbox,
        string accessToken,
        Guid connectionId,
        IReadOnlySet<string> excludeMessageIds,
        Action<int, int> reportScanProgress,
        CancellationToken cancellationToken)
    {
        var scanned = 0;
        var alreadySeen = 0;
        var pagesWalked = 0;
        string? pageToken = null;

        while (pagesWalked < MaxCandidatePagesPerRun)
        {
            reportScanProgress(pagesWalked + 1, scanned);
            var batch = await mailbox.ListCandidatesAsync(accessToken, pageToken, cancellationToken);
            if (batch.Messages.Count == 0)
            {
                if (string.IsNullOrWhiteSpace(batch.NextPageToken))
                {
                    return new FreshCandidateSearch(null, scanned, alreadySeen, true, false);
                }

                pageToken = batch.NextPageToken;
                pagesWalked++;
                continue;
            }

            scanned += batch.Messages.Count;
            pagesWalked++;
            var existing = await _processed.FindExistingAsync(
                connectionId,
                batch.Messages.Select(item => item.Id).ToList(),
                cancellationToken);

            foreach (var candidate in batch.Messages)
            {
                if (excludeMessageIds.Contains(candidate.Id) || existing.Contains(candidate.Id))
                {
                    alreadySeen++;
                    continue;
                }

                return new FreshCandidateSearch(candidate, scanned, alreadySeen, false, false);
            }

            if (string.IsNullOrWhiteSpace(batch.NextPageToken))
            {
                return new FreshCandidateSearch(null, scanned, alreadySeen, true, false);
            }

            pageToken = batch.NextPageToken;
        }

        return new FreshCandidateSearch(null, scanned, alreadySeen, false, true);
    }

    private static MailCheckRunDto EmptyRun(bool busy, bool hasMore)
    {
        return new MailCheckRunDto(busy, 0, 0, 0, 0, 0, hasMore, 0, 0, null, null, null, [], new MailCheckRunTimingDto(0, 0, 0, 0, 0, 0, 0, 0));
    }

    private async Task<int> EnforceTrashLabelsAsync(
        Guid userId,
        MailConnection connection,
        string providerName,
        IMailMailbox mailbox,
        string accessToken,
        IReadOnlyDictionary<MailCheckLabel, string> labels,
        IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> labelRules,
        List<MailCheckRunItemDto> items,
        Guid runId,
        string source,
        CancellationToken cancellationToken)
    {
        var trashLabels = MailCheckLabelActionRules.TrashLabels(labelRules);
        if (trashLabels.Count == 0)
        {
            return 0;
        }

        var remaining = BatchSize;
        var enforced = 0;
        foreach (var label in trashLabels)
        {
            if (remaining <= 0)
            {
                break;
            }

            var listed = await mailbox.ListLabeledAsync(accessToken, labels, label, cancellationToken);
            foreach (var item in listed)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var messageClock = Stopwatch.StartNew();
                MailMessageContent message;
                try
                {
                    message = await mailbox.GetMessageAsync(accessToken, item.Id, cancellationToken);
                }
                catch (Exception exception) when (MailCheckMailboxErrors.IsMissingMessage(exception))
                {
                    await RecordMissingMessageAsync(userId, connection.Id, item.Id, cancellationToken);
                    continue;
                }

                if (MailCheckMessageState.IsTrashed(message))
                {
                    continue;
                }

                await mailbox.MarkAsReadAsync(accessToken, message.Id, message.LabelIds, cancellationToken);
                await mailbox.TrashAsync(accessToken, message.Id, cancellationToken);
                await _processed.UpsertAsync(
                    MailCheckProcessedMessage.Create(userId, connection.Id, message.Id, label),
                    cancellationToken);
                var labelName = MailCheckLabelCatalog.NameFor(label);
                items.Add(new MailCheckRunItemDto(
                    message.Id,
                    message.Subject,
                    message.From,
                    "trash",
                    "Enforced trash label action",
                    labelName,
                    connection.Id,
                    connection.Email,
                    providerName));
                await AppendMessageLogAsync(
                    userId,
                    runId,
                    source,
                    connection.Id,
                    connection.Email,
                    providerName,
                    message.Id,
                    message.Subject,
                    message.From,
                    "trash",
                    labelName,
                    "Enforced trash label action",
                    (int)messageClock.ElapsedMilliseconds,
                    cancellationToken);
                enforced++;
                remaining--;
            }
        }

        return enforced;
    }

    private Task AppendMessageLogAsync(
        Guid userId,
        Guid runId,
        string source,
        Guid mailConnectionId,
        string mailboxEmail,
        string mailboxProvider,
        string messageId,
        string subject,
        string fromAddress,
        string action,
        string label,
        string detail,
        int durationMs,
        CancellationToken cancellationToken)
    {
        return _actionLogs.AddAsync(
            MailCheckActionLog.CreateMessage(
                userId,
                runId,
                source,
                mailConnectionId,
                mailboxEmail,
                mailboxProvider,
                messageId,
                subject,
                fromAddress,
                action,
                label,
                detail,
                durationMs),
            cancellationToken);
    }

    private Task AppendRunSummaryLogAsync(
        Guid userId,
        Guid runId,
        string source,
        int processed,
        int labeled,
        int trashed,
        int skipped,
        int errors,
        int scanned,
        int alreadySeen,
        bool hasMore,
        MailCheckRunTimingDto timing,
        int durationMs,
        string? mailboxEmail,
        string? mailboxProvider,
        CancellationToken cancellationToken,
        string? error = null)
    {
        var detail = string.IsNullOrWhiteSpace(error)
            ? $"Processed {processed}, labeled {labeled}, trashed {trashed}, skipped {skipped}, errors {errors}, scanned {scanned}, already seen {alreadySeen}, hasMore {hasMore}. Timing lock {timing.LockMs} ms, token {timing.TokenMs} ms, labels {timing.LabelsMs} ms, scan {timing.ScanMs} ms, fetch {timing.FetchMs} ms, classify {timing.ClassifyMs} ms, apply {timing.ApplyMs} ms, total {timing.TotalMs} ms."
            : $"Run failed: {error}. Processed {processed}, errors {errors + 0}, scanned {scanned}.";
        return _actionLogs.AddAsync(
            MailCheckActionLog.CreateRunSummary(
                userId,
                runId,
                source,
                "run_completed",
                detail,
                durationMs,
                mailboxEmail,
                mailboxProvider),
            cancellationToken);
    }

    private void ReportRunProgress(
        Guid userId,
        int generation,
        string stage,
        string message,
        string? mailboxEmail,
        int processed,
        int scanned,
        int alreadySeen,
        int scanPage)
    {
        _runProgress.Report(userId, generation, stage, message, mailboxEmail, processed, scanned, alreadySeen, scanPage);
        _logger.LogInformation(
            "MailCheck stage={Stage} user={UserId} mailbox={Mailbox} processed={Processed} scanned={Scanned} alreadySeen={AlreadySeen} scanPage={ScanPage} message={Message}",
            stage,
            userId,
            mailboxEmail ?? string.Empty,
            processed,
            scanned,
            alreadySeen,
            scanPage,
            message);
    }
}
