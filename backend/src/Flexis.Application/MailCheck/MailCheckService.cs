using System.Collections.Concurrent;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Application.Microsoft;
using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed class MailCheckService
{
    private const int BatchSize = 20;
    private const int MaxListPages = 3;
    private static readonly TimeSpan AutoCooldown = TimeSpan.FromSeconds(60);
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> RunLocks = new();

    private static readonly HashSet<string> RecommendedModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-4.1-mini",
        "gpt-4.1"
    };

    private readonly IMailCheckSettingsRepository _settings;
    private readonly IMailCheckProcessedMessageRepository _processed;
    private readonly IMailConnectionRepository _mailConnections;
    private readonly MailAccessTokenService _mailTokens;
    private readonly IMailMailboxGateway _mailboxes;
    private readonly IMicrosoftOAuthGateway _microsoftOAuth;
    private readonly IGoogleTokenProtector _protector;
    private readonly IOpenAiGateway _openAi;

    public MailCheckService(
        IMailCheckSettingsRepository settings,
        IMailCheckProcessedMessageRepository processed,
        IMailConnectionRepository mailConnections,
        MailAccessTokenService mailTokens,
        IMailMailboxGateway mailboxes,
        IMicrosoftOAuthGateway microsoftOAuth,
        IGoogleTokenProtector protector,
        IOpenAiGateway openAi)
    {
        _settings = settings;
        _processed = processed;
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

        await _settings.SaveChangesAsync(cancellationToken);
        if (settings.HasApiKey)
        {
            var connection = await _mailConnections.GetByUserIdAsync(userId, cancellationToken);
            if (connection is not null)
            {
                var access = await _mailTokens.GetAccessAsync(userId, cancellationToken);
                await _mailboxes.Resolve(access.Provider).EnsureLabelsAsync(access.AccessToken, cancellationToken);
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
        if (!request.Force
            && settings.LastRunAt is { } last
            && DateTimeOffset.UtcNow - last < AutoCooldown
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
            return await RunLockedAsync(userId, settings, cancellationToken);
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
        var connection = await RequireConnectionAsync(userId, cancellationToken);
        var access = await _mailTokens.GetAccessAsync(userId, cancellationToken);
        var mailbox = _mailboxes.Resolve(access.Provider);
        var labels = await mailbox.EnsureLabelsAsync(access.AccessToken, cancellationToken);
        var filter = MailCheckLabels.KeepFromSlug(labelSlug);
        var listed = await mailbox.ListLabeledAsync(access.AccessToken, labels, filter, cancellationToken);
        var items = listed
            .Select(item => new MailCheckInboxItemDto(
                item.Id,
                item.ThreadId,
                item.Subject,
                item.From,
                item.Date,
                item.Snippet,
                MailCheckLabels.NameFor(item.Decision),
                MailCheckLabels.SlugFor(item.Decision),
                item.Starred))
            .ToList();
        return new MailCheckInboxDto(items);
    }

    private async Task<MailCheckRunDto> RunLockedAsync(
        Guid userId,
        MailCheckSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.HasApiKey)
        {
            throw new ValidationFailedException("Save an OpenAI API key on Mail Check Settings.");
        }

        await RequireConnectionAsync(userId, cancellationToken);
        var access = await _mailTokens.GetAccessAsync(userId, cancellationToken);
        var mailbox = _mailboxes.Resolve(access.Provider);
        var apiKey = _protector.Unprotect(settings.ApiKeyProtected!);
        var labels = await mailbox.EnsureLabelsAsync(access.AccessToken, cancellationToken);
        var ourLabelIds = labels.Values.ToHashSet(StringComparer.Ordinal);
        var items = new List<MailCheckRunItemDto>();
        var labeled = 0;
        var trashed = 0;
        var skipped = 0;
        var errors = 0;
        var processed = 0;
        var hasMore = false;
        string? pageToken = null;

        try
        {
            for (var page = 0; page < MaxListPages && processed < BatchSize; page++)
            {
                var batch = await mailbox.ListCandidatesAsync(access.AccessToken, pageToken, cancellationToken);
                pageToken = batch.NextPageToken;
                if (batch.Messages.Count == 0)
                {
                    break;
                }

                var existing = await _processed.FindExistingAsync(
                    userId,
                    batch.Messages.Select(item => item.Id).ToList(),
                    cancellationToken);
                var fresh = batch.Messages.Where(item => !existing.Contains(item.Id)).ToList();
                hasMore = pageToken is not null || fresh.Count > BatchSize - processed;
                foreach (var candidate in fresh)
                {
                    if (processed >= BatchSize)
                    {
                        hasMore = true;
                        break;
                    }

                    try
                    {
                        var message = await mailbox.GetMessageAsync(access.AccessToken, candidate.Id, cancellationToken);
                        if (message.LabelIds.Any(ourLabelIds.Contains))
                        {
                            await _processed.AddAsync(
                                MailCheckProcessedMessage.Create(userId, message.Id, MailCheckDecision.Skip),
                                cancellationToken);
                            skipped++;
                            processed++;
                            continue;
                        }

                        var classification = await _openAi.ClassifyAsync(
                            apiKey,
                            settings.Model,
                            FormatMail(message),
                            cancellationToken);
                        await ApplyAsync(
                            userId,
                            mailbox,
                            access.AccessToken,
                            message,
                            labels,
                            classification,
                            items,
                            cancellationToken);
                        if (classification.Decision == MailCheckDecision.Discard)
                        {
                            trashed++;
                        }
                        else if (classification.Decision == MailCheckDecision.Skip)
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
                            string.Empty));
                    }
                }

                if (processed >= BatchSize || pageToken is null)
                {
                    break;
                }
            }

            settings.RecordRun(labeled, trashed, skipped, processed, errors, hasMore, string.Empty);
        }
        catch (Exception exception) when (exception is ValidationFailedException or GoogleOAuthException or MicrosoftOAuthException)
        {
            settings.RecordRun(labeled, trashed, skipped, processed, errors + 1, hasMore, exception.Message);
            await _processed.SaveChangesAsync(cancellationToken);
            await _settings.SaveChangesAsync(cancellationToken);
            throw;
        }

        await _processed.SaveChangesAsync(cancellationToken);
        await _settings.SaveChangesAsync(cancellationToken);
        return new MailCheckRunDto(false, processed, labeled, trashed, skipped, errors, hasMore, items);
    }

    private async Task ApplyAsync(
        Guid userId,
        IMailMailbox mailbox,
        string token,
        MailMessageContent message,
        IReadOnlyDictionary<MailCheckDecision, string> labels,
        MailCheckClassification classification,
        List<MailCheckRunItemDto> items,
        CancellationToken cancellationToken)
    {
        if (classification.Decision == MailCheckDecision.Discard)
        {
            await mailbox.TrashAsync(token, message.Id, cancellationToken);
        }
        else if (classification.Decision != MailCheckDecision.Skip)
        {
            if (!labels.TryGetValue(classification.Decision, out var labelId))
            {
                throw new ValidationFailedException("Mail Check labels are missing.");
            }

            await mailbox.ApplyLabelAndPinAsync(token, message.Id, labelId, message.LabelIds, cancellationToken);
        }

        await _processed.AddAsync(
            MailCheckProcessedMessage.Create(userId, message.Id, classification.Decision),
            cancellationToken);
        items.Add(new MailCheckRunItemDto(
            message.Id,
            message.Subject,
            message.From,
            MailCheckLabels.SlugFor(classification.Decision),
            classification.Reason,
            MailCheckLabels.NameFor(classification.Decision)));
    }

    private async Task<MailConnection> RequireConnectionAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _mailConnections.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ValidationFailedException("Connect a mailbox on Mail Check Settings first.");
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
        var connection = await _mailConnections.GetByUserIdAsync(userId, cancellationToken);
        var outlookAvailable = await _microsoftOAuth.IsConfiguredAsync(cancellationToken);
        return new MailCheckSettingsDto(
            settings.HasApiKey,
            settings.Model,
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
            connection is not null,
            connection?.Email,
            connection is null ? null : connection.Provider == MailProvider.Gmail ? "gmail" : "outlook",
            outlookAvailable);
    }

    private static MailCheckRunDto EmptyRun(bool busy, bool hasMore)
    {
        return new MailCheckRunDto(busy, 0, 0, 0, 0, 0, hasMore, []);
    }

    private static string FormatMail(MailMessageContent message)
    {
        return $"From: {message.From}\nDate: {message.Date}\nSubject: {message.Subject}\n\n{message.Body}";
    }
}
