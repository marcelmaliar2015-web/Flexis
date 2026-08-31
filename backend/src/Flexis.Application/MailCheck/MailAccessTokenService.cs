using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed class MailAccessTokenService
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly IMailConnectionRepository _connections;
    private readonly IGoogleTokenProtector _protector;
    private readonly IGoogleOAuthGateway _oauth;

    public MailAccessTokenService(
        IMailConnectionRepository connections,
        IGoogleTokenProtector protector,
        IGoogleOAuthGateway oauth)
    {
        _connections = connections;
        _protector = protector;
        _oauth = oauth;
    }

    public async Task<string> GetGmailAccessTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ValidationFailedException("Connect a mailbox on Mail Check Settings first.");
        if (connection.Provider != MailProvider.Gmail)
        {
            throw new ValidationFailedException("Mail Check is connected to a mailbox this build cannot use yet.");
        }

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
