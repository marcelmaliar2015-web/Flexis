using Flexis.Application.Common;

namespace Flexis.Application.Google;

public sealed class GoogleAccessTokenService
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly IGoogleConnectionRepository _connections;
    private readonly IGoogleTokenProtector _protector;
    private readonly IGoogleOAuthGateway _oauth;

    public GoogleAccessTokenService(
        IGoogleConnectionRepository connections,
        IGoogleTokenProtector protector,
        IGoogleOAuthGateway oauth)
    {
        _connections = connections;
        _protector = protector;
        _oauth = oauth;
    }

    public async Task<string> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ValidationFailedException("Connect Gmail first.");

        if (connection.AccessTokenExpiresAt > DateTimeOffset.UtcNow.Add(RefreshSkew))
        {
            return _protector.Unprotect(connection.AccessTokenProtected);
        }

        var refreshToken = _protector.Unprotect(connection.RefreshTokenProtected);
        var tokens = await _oauth.RefreshAsync(refreshToken, cancellationToken);
        connection.ReplaceAccessToken(_protector.Protect(tokens.AccessToken), tokens.AccessTokenExpiresAt);
        await _connections.SaveChangesAsync(cancellationToken);
        return tokens.AccessToken;
    }
}
