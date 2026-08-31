namespace Flexis.Application.Microsoft;

public interface IMicrosoftOAuthGateway
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

    Task<string> CreateAuthorizationUrlAsync(
        string state,
        string codeChallenge,
        string scopes,
        CancellationToken cancellationToken);

    Task<MicrosoftOAuthTokenSet> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken);

    Task<MicrosoftOAuthTokenSet> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task<MicrosoftUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken);
}
