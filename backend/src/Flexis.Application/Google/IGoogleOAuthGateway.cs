namespace Flexis.Application.Google;

public interface IGoogleOAuthGateway
{
    bool IsConfigured { get; }

    string CreateAuthorizationUrl(string state, string codeChallenge);

    Task<GoogleOAuthTokenSet> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken);

    Task<GoogleUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
