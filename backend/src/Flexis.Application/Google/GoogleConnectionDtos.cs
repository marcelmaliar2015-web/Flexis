namespace Flexis.Application.Google;

public sealed record GoogleConnectionStatusDto(
    bool Configured,
    bool Connected,
    string? GoogleEmail,
    DateTimeOffset? ConnectedAt,
    IReadOnlyList<string> Capabilities);

public sealed record GoogleConnectStartRequest(string ReturnUrl);

public sealed record GoogleConnectStartDto(string AuthorizationUrl);
