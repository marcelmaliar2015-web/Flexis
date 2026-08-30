namespace Flexis.Application.Google;

public sealed record GoogleOAuthPending(Guid UserId, string CodeVerifier, string ReturnUrl);

public sealed record GoogleOAuthTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string Scope);

public sealed record GoogleUserInfo(string Subject, string Email);
