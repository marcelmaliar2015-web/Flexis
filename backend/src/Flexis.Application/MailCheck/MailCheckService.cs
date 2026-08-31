using System.Collections.Concurrent;
using Flexis.Application.Common;
using Flexis.Application.Google;
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
    private readonly IGoogleConnectionRepository _connections;
    private readonly GoogleAccessTokenService _tokens;
    private readonly IGoogleTokenProtector _protector;
    private readonly IGmailMailbox _gmail;
    private readonly IOpenAiGateway _openAi;

    public MailCheckService(
        IMailCheckSettingsRepository settings,
        IMailCheckProcessedMessageRepository processed,
        IGoogleConnectionRepository connections,
        GoogleAccessTokenService tokens,
        IGoogleTokenProtector protector,
        IGmailMailbox gmail,
        IOpenAiGateway openAi)
    {
        _settings = settings;
        _processed = processed;
        _connections = connections;
        _tokens = tokens;
        _protector = protector;
        _gmail = gmail;
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
            var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
            if (connection is not null)
            {
                var token = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
                await _gmail.EnsureLabelsAsync(token, cancellationToken);
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
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
        if (connection is null)
        {
            throw new ValidationFailedException("Connect Gmail first.");
        }

        var token = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var labels = await _gmail.EnsureLabelsAsync(token, cancellationToken);
        var filter = MailCheckLabels.KeepFromSlug(labelSlug);
        var listed = await _gmail.ListLabeledAsync(token, labels, filter, cancellationToken);
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

        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
        if (connection is null)
        {
            throw new ValidationFailedException("Connect Gmail first.");
        }

        var token = await _tokens.GetAccessTokenAsync(userId, cancellationToken);
        var apiKey = _protector.Unprotect(settings.ApiKeyProtected!);
        var labels = await _gmail.EnsureLabelsAsync(token, cancellationToken);
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
                var batch = await _gmail.ListCandidatesAsync(token, pageToken, cancellationToken);
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
                        var message = await _gmail.GetMessageAsync(token, candidate.Id, cancellationToken);
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
                            token,
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
        catch (Exception exception) when (exception is ValidationFailedException or GoogleOAuthException)
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
        string token,
        GmailMessageContent message,
        IReadOnlyDictionary<MailCheckDecision, string> labels,
        MailCheckClassification classification,
        List<MailCheckRunItemDto> items,
        CancellationToken cancellationToken)
    {
        if (classification.Decision == MailCheckDecision.Discard)
        {
            await _gmail.TrashAsync(token, message.Id, cancellationToken);
        }
        else if (classification.Decision != MailCheckDecision.Skip)
        {
            if (!labels.TryGetValue(classification.Decision, out var labelId))
            {
                throw new ValidationFailedException("Mail Check labels are missing.");
            }

            await _gmail.ApplyLabelAndPinAsync(token, message.Id, labelId, message.LabelIds, cancellationToken);
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
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
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
            connection?.GoogleEmail);
    }

    private static MailCheckRunDto EmptyRun(bool busy, bool hasMore)
    {
        return new MailCheckRunDto(busy, 0, 0, 0, 0, 0, hasMore, []);
    }

    private static string FormatMail(GmailMessageContent message)
    {
        return $"From: {message.From}\nDate: {message.Date}\nSubject: {message.Subject}\n\n{message.Body}";
    }
}
