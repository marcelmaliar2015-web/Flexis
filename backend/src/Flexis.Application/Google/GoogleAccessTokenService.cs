using Flexis.Application.Common;

namespace Flexis.Application.Google;

public sealed record GoogleSheetAccess(string AccessToken, string OwnerEmail);

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
        var access = await GetSheetAccessAsync(userId, cancellationToken);
        return access.AccessToken;
    }

    public async Task<GoogleSheetAccess> GetSheetAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ValidationFailedException("Connect Gmail first.");

        if (connection.AccessTokenExpiresAt > DateTimeOffset.UtcNow.Add(RefreshSkew))
        {
            return new GoogleSheetAccess(
                _protector.Unprotect(connection.AccessTokenProtected),
                connection.GoogleEmail);
        }

        var refreshToken = _protector.Unprotect(connection.RefreshTokenProtected);
        var tokens = await _oauth.RefreshAsync(refreshToken, cancellationToken);
        connection.ReplaceAccessToken(_protector.Protect(tokens.AccessToken), tokens.AccessTokenExpiresAt);
        await _connections.SaveChangesAsync(cancellationToken);
        return new GoogleSheetAccess(tokens.AccessToken, connection.GoogleEmail);
    }
}
