namespace Flexis.Application.Google;

public interface IGoogleOAuthGateway
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

    Task<string> CreateAuthorizationUrlAsync(string state, string codeChallenge, CancellationToken cancellationToken);

    Task<GoogleOAuthTokenSet> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken);

    Task<GoogleOAuthTokenSet> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task<GoogleUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
