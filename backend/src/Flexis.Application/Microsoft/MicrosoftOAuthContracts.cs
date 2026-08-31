namespace Flexis.Application.Microsoft;

public sealed record MicrosoftOAuthPending(Guid UserId, string CodeVerifier, string ReturnUrl);

public sealed record MicrosoftOAuthTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string Scope);

public sealed record MicrosoftUserInfo(string Subject, string Email);
