namespace Flexis.Application.MailCheck;

public sealed record MailMailboxStatusDto(
    bool Connected,
    string? Provider,
    string? Email,
    DateTimeOffset? ConnectedAt,
    bool OutlookAvailable);

public sealed record MailConnectStartRequest(string ReturnUrl);

public sealed record MailConnectStartDto(string AuthorizationUrl);
