namespace Flexis.Application.MailCheck;

public sealed record MailMailboxItemDto(
    Guid Id,
    string Provider,
    string Email,
    DateTimeOffset ConnectedAt,
    DateTimeOffset? CheckedNewestAt,
    DateTimeOffset? CheckedUntilAt,
    DateTimeOffset? LastScanAt,
    bool ScanCaughtUp);

public sealed record MailMailboxStatusDto(
    bool OutlookAvailable,
    IReadOnlyList<MailMailboxItemDto> Mailboxes);

public sealed record MailConnectStartRequest(string ReturnUrl);

public sealed record MailConnectStartDto(string AuthorizationUrl);
