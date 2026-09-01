namespace Flexis.Application.MailCheck;

public sealed record MailCheckSettingsDto(
    bool HasApiKey,
    string Model,
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

public sealed record MailCheckSettingsWriteRequest(string? ApiKey, bool ClearApiKey, string Model);

public sealed record MailCheckModelDto(string Id, bool Recommended);

public sealed record MailCheckModelsDto(IReadOnlyList<MailCheckModelDto> Models);

public sealed record MailCheckRunRequest(bool Force);

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

public sealed record MailCheckRunDto(
    bool Busy,
    int Processed,
    int Labeled,
    int Trashed,
    int Skipped,
    int Errors,
    bool HasMore,
    IReadOnlyList<MailCheckRunItemDto> Items);

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
