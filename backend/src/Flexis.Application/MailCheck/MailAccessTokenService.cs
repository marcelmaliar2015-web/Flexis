using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Application.Microsoft;
using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed class MailAccessTokenService
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private readonly IMailConnectionRepository _connections;
    private readonly IGoogleTokenProtector _protector;
    private readonly IGoogleOAuthGateway _googleOAuth;
    private readonly IMicrosoftOAuthGateway _microsoftOAuth;

    public MailAccessTokenService(
        IMailConnectionRepository connections,
        IGoogleTokenProtector protector,
        IGoogleOAuthGateway googleOAuth,
        IMicrosoftOAuthGateway microsoftOAuth)
    {
        _connections = connections;
        _protector = protector;
        _googleOAuth = googleOAuth;
        _microsoftOAuth = microsoftOAuth;
    }

    public async Task<(MailProvider Provider, string AccessToken)> GetAccessAsync(
        Guid userId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByIdForUserAsync(userId, connectionId, cancellationToken)
            ?? throw new ValidationFailedException("Connect a mailbox on Mail Check Settings first.");

        if (connection.AccessTokenExpiresAt > DateTimeOffset.UtcNow.Add(RefreshSkew))
        {
            return (connection.Provider, _protector.Unprotect(connection.AccessTokenProtected));
        }

        var refreshToken = _protector.Unprotect(connection.RefreshTokenProtected);
        var tokens = connection.Provider switch
        {
            MailProvider.Gmail => await RefreshGoogleAsync(refreshToken, cancellationToken),
            MailProvider.Outlook => await RefreshMicrosoftAsync(refreshToken, cancellationToken),
            _ => throw new ValidationFailedException("Mail Check is connected to a mailbox this build cannot use yet."),
        };
        connection.ReplaceAccessToken(_protector.Protect(tokens.AccessToken), tokens.AccessTokenExpiresAt);
        await _connections.SaveChangesAsync(cancellationToken);
        return (connection.Provider, tokens.AccessToken);
    }

    private async Task<(string AccessToken, DateTimeOffset AccessTokenExpiresAt)> RefreshGoogleAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokens = await _googleOAuth.RefreshAsync(refreshToken, cancellationToken);
        return (tokens.AccessToken, tokens.AccessTokenExpiresAt);
    }

    private async Task<(string AccessToken, DateTimeOffset AccessTokenExpiresAt)> RefreshMicrosoftAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokens = await _microsoftOAuth.RefreshAsync(refreshToken, cancellationToken);
        return (tokens.AccessToken, tokens.AccessTokenExpiresAt);
    }
}
