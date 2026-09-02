namespace Flexis.Application.MailCheck;

public sealed record MailCheckSettingsDto(
    bool HasApiKey,
    string Model,
    string ClassifierPrompt,
    string DefaultClassifierPrompt,
    IReadOnlyDictionary<string, string> LabelActions,
    IReadOnlyDictionary<string, string> DefaultLabelActions,
    IReadOnlyList<string> NeedActionLabels,
    IReadOnlyList<string> DefaultNeedActionLabels,
    bool AutoCheckEnabled,
    int AutoCheckIntervalSeconds,
    DateTimeOffset? LastRunAt,
    string LastError,
    int LastLabeled,
    int LastTrashed,
    int LastSkipped,
    int LastProcessed,
    int LastErrors,
    bool LastHasMore,
    int TotalLabeled,
    int TotalTrashed,
    IReadOnlyList<MailMailboxItemDto> Mailboxes,
    bool OutlookAvailable);

public sealed record MailCheckSettingsWriteRequest(
    string? ApiKey,
    bool ClearApiKey,
    string Model,
    string? ClassifierPrompt,
    IReadOnlyDictionary<string, string>? LabelActions,
    IReadOnlyList<string>? NeedActionLabels,
    bool? AutoCheckEnabled);

public sealed record MailCheckModelDto(string Id, bool Recommended);

public sealed record MailCheckModelsDto(IReadOnlyList<MailCheckModelDto> Models);

public sealed record MailCheckRunRequest(bool Force, Guid? MailboxId, bool ResetCursor);

public sealed record MailCheckRunItemDto(
    string MessageId,
    string Subject,
    string From,
    string Action,
    string Reason,
    string Label,
    Guid MailboxId,
    string MailboxEmail,
    string MailboxProvider);

public sealed record MailCheckRunTimingDto(
    int TotalMs,
    int LockMs,
    int TokenMs,
    int LabelsMs,
    int ScanMs,
    int FetchMs,
    int ClassifyMs,
    int ApplyMs);

public sealed record MailCheckRunProgressDto(
    bool Active,
    string Stage,
    string Message,
    string? MailboxEmail,
    int Processed,
    int Scanned,
    int AlreadySeen,
    int ScanPage,
    long ElapsedMs,
    bool WaitingForLock,
    string? ActiveRunKind,
    string? WaitingRequestKind);

public sealed record MailCheckRunDto(
    bool Busy,
    int Processed,
    int Labeled,
    int Trashed,
    int Skipped,
    int Errors,
    bool HasMore,
    int Scanned,
    int AlreadySeen,
    Guid? MailboxId,
    string? MailboxEmail,
    string? MailboxProvider,
    IReadOnlyList<MailCheckRunItemDto> Items,
    MailCheckRunTimingDto Timing);

public sealed record MailCheckInboxItemDto(
    string Id,
    string ThreadId,
    string Subject,
    string From,
    string Date,
    string Snippet,
    string Label,
    string LabelSlug,
    bool Starred,
    Guid MailboxId,
    string MailboxEmail,
    string MailboxProvider);

public sealed record MailCheckInboxDto(IReadOnlyList<MailCheckInboxItemDto> Items);
