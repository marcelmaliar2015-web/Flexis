using System.Security.Cryptography;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed class MailConnectionService
{
    private static readonly TimeSpan OAuthStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly Uri MailCheckPath = new("/mail-check", UriKind.Relative);

    private readonly IGoogleOAuthGateway _oauth;
    private readonly IGoogleOAuthStateStore _states;
    private readonly IGoogleTokenProtector _protector;
    private readonly IMailConnectionRepository _connections;
    private readonly IFrontendOrigins _frontendOrigins;

    public MailConnectionService(
        IGoogleOAuthGateway oauth,
        IGoogleOAuthStateStore states,
        IGoogleTokenProtector protector,
        IMailConnectionRepository connections,
        IFrontendOrigins frontendOrigins)
    {
        _oauth = oauth;
        _states = states;
        _protector = protector;
        _connections = connections;
        _frontendOrigins = frontendOrigins;
    }

    public async Task<MailMailboxStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
        return new MailMailboxStatusDto(
            connection is not null,
            connection is null ? null : ToProviderName(connection.Provider),
            connection?.Email,
            connection?.ConnectedAt,
            OutlookAvailable: false);
    }

    public async Task<MailConnectStartDto> StartGmailConnectAsync(
        Guid userId,
        MailConnectStartRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _oauth.IsConfiguredAsync(cancellationToken))
        {
            throw new ValidationFailedException("An admin must save the Google Cloud client in Settings.");
        }

        var returnUrl = NormalizeReturnUrl(request.ReturnUrl);
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var (verifier, challenge) = CreatePkce();
        _states.Save(
            state,
            new GoogleOAuthPending(
                userId,
                verifier,
                returnUrl,
                OAuthConnectTarget.MailCheck,
                MailGmailScopes.Request),
            OAuthStateLifetime);
        return new MailConnectStartDto(
            await _oauth.CreateAuthorizationUrlAsync(state, challenge, MailGmailScopes.Request, cancellationToken));
    }

    public async Task CompleteGmailConnectAsync(
        Guid userId,
        GoogleOAuthTokenSet tokens,
        GoogleUserInfo profile,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            throw new GoogleOAuthException("Google did not return a refresh token.");
        }

        var refreshProtected = _protector.Protect(tokens.RefreshToken);
        var accessProtected = _protector.Protect(tokens.AccessToken);
        var existing = await _connections.GetByUserIdAsync(userId, cancellationToken);
        if (existing is null)
        {
            await _connections.AddAsync(
                MailConnection.CreateGmail(
                    userId,
                    profile.Subject,
                    profile.Email,
                    refreshProtected,
                    accessProtected,
                    tokens.AccessTokenExpiresAt,
                    tokens.Scope),
                cancellationToken);
        }
        else
        {
            existing.ReplaceGmailCredentials(
                profile.Subject,
                profile.Email,
                refreshProtected,
                accessProtected,
                tokens.AccessTokenExpiresAt,
                tokens.Scope);
        }

        await _connections.SaveChangesAsync(cancellationToken);
    }

    public async Task DisconnectAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
        if (connection is null)
        {
            return;
        }

        if (connection.Provider == MailProvider.Gmail)
        {
            var refreshToken = _protector.Unprotect(connection.RefreshTokenProtected);
            await _oauth.RevokeAsync(refreshToken, cancellationToken);
        }

        _connections.Remove(connection);
        await _connections.SaveChangesAsync(cancellationToken);
    }

    public static string AppendMailboxResult(string returnUrl, string result)
    {
        return $"{returnUrl}?mailbox={Uri.EscapeDataString(result)}";
    }

    public static string MailCheckFallbackReturnUrl(IReadOnlyList<string> origins)
    {
        var origin = origins.Count > 0 ? origins[0] : "http://127.0.0.1:5173";
        return origin + MailCheckPath.OriginalString;
    }

    private static string ToProviderName(MailProvider provider)
    {
        return provider switch
        {
            MailProvider.Gmail => "gmail",
            MailProvider.Outlook => "outlook",
            _ => "unknown",
        };
    }

    private string NormalizeReturnUrl(string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.AbsolutePath != MailCheckPath.OriginalString
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ValidationFailedException("Return URL is not allowed.");
        }

        var origin = uri.GetLeftPart(UriPartial.Authority);
        if (!_frontendOrigins.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationFailedException("Return URL is not allowed.");
        }

        return origin + MailCheckPath.OriginalString;
    }

    private static (string Verifier, string Challenge) CreatePkce()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
