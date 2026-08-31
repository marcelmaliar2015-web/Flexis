namespace Flexis.Application.Google;

public enum OAuthConnectTarget
{
    JobApplication = 0,
    MailCheck = 1,
}

public sealed record GoogleOAuthPending(
    Guid UserId,
    string CodeVerifier,
    string ReturnUrl,
    OAuthConnectTarget Target = OAuthConnectTarget.JobApplication,
    string? Scopes = null);

public sealed record GoogleOAuthTokenSet(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string Scope);

public sealed record GoogleUserInfo(string Subject, string Email);
