using System.Security.Cryptography;
using Flexis.Application.Common;
using Flexis.Domain.Google;

namespace Flexis.Application.Google;

public sealed class GoogleConnectionService
{
    private static readonly TimeSpan OAuthStateLifetime = TimeSpan.FromMinutes(10);
    private static readonly Uri JobApplicationPath = new("/job-application", UriKind.Relative);

    private readonly IGoogleOAuthGateway _oauth;
    private readonly IGoogleOAuthStateStore _states;
    private readonly IGoogleTokenProtector _protector;
    private readonly IGoogleConnectionRepository _connections;
    private readonly IFrontendOrigins _frontendOrigins;
    private readonly GoogleDriveLayoutService _driveLayout;

    public GoogleConnectionService(
        IGoogleOAuthGateway oauth,
        IGoogleOAuthStateStore states,
        IGoogleTokenProtector protector,
        IGoogleConnectionRepository connections,
        IFrontendOrigins frontendOrigins,
        GoogleDriveLayoutService driveLayout)
    {
        _oauth = oauth;
        _states = states;
        _protector = protector;
        _connections = connections;
        _frontendOrigins = frontendOrigins;
        _driveLayout = driveLayout;
    }

    public async Task<GoogleConnectionStatusDto> GetStatusAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
        return new GoogleConnectionStatusDto(
            await _oauth.IsConfiguredAsync(cancellationToken),
            connection is not null,
            connection?.GoogleEmail,
            connection?.ConnectedAt,
            GoogleWorkspaceScopes.Capabilities);
    }

    public async Task<GoogleConnectStartDto> StartConnectAsync(
        Guid userId,
        GoogleConnectStartRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _oauth.IsConfiguredAsync(cancellationToken))
        {
            throw new ValidationFailedException("An admin must save the Google Cloud client in Settings.");
        }

        var returnUrl = NormalizeReturnUrl(request.ReturnUrl);
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var (verifier, challenge) = CreatePkce();
        _states.Save(state, new GoogleOAuthPending(userId, verifier, returnUrl), OAuthStateLifetime);
        return new GoogleConnectStartDto(await _oauth.CreateAuthorizationUrlAsync(state, challenge, cancellationToken));
    }

    public async Task<string> CompleteCallbackAsync(
        string? code,
        string? state,
        string? error,
        CancellationToken cancellationToken)
    {
        var fallback = FallbackReturnUrl();
        if (string.IsNullOrWhiteSpace(state) || !_states.TryTake(state, out var pending) || pending is null)
        {
            return AppendResult(fallback, "error");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            var result = string.Equals(error, "access_denied", StringComparison.Ordinal) ? "denied" : "error";
            return AppendResult(pending.ReturnUrl, result);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return AppendResult(pending.ReturnUrl, "error");
        }

        try
        {
            var tokens = await _oauth.ExchangeCodeAsync(code, pending.CodeVerifier, cancellationToken);
            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                throw new GoogleOAuthException("Google did not return a refresh token.");
            }

            var profile = await _oauth.GetUserInfoAsync(tokens.AccessToken, cancellationToken);
            var existing = await _connections.GetByUserIdAsync(pending.UserId, cancellationToken);
            var refreshProtected = _protector.Protect(tokens.RefreshToken);
            var accessProtected = _protector.Protect(tokens.AccessToken);
            if (existing is null)
            {
                await _connections.AddAsync(
                    GoogleConnection.Create(
                        pending.UserId,
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
                existing.ReplaceCredentials(
                    profile.Subject,
                    profile.Email,
                    refreshProtected,
                    accessProtected,
                    tokens.AccessTokenExpiresAt,
                    tokens.Scope);
            }

            await _connections.SaveChangesAsync(cancellationToken);
            await TryEnsureDriveLayoutAsync(pending.UserId, tokens.AccessToken, cancellationToken);
            return AppendResult(pending.ReturnUrl, "connected");
        }
        catch (GoogleOAuthException)
        {
            return AppendResult(pending.ReturnUrl, "error");
        }
    }

    public async Task DisconnectAsync(Guid userId, CancellationToken cancellationToken)
    {
        var connection = await _connections.GetByUserIdAsync(userId, cancellationToken);
        if (connection is null)
        {
            return;
        }

        var refreshToken = _protector.Unprotect(connection.RefreshTokenProtected);
        await _oauth.RevokeAsync(refreshToken, cancellationToken);
        _connections.Remove(connection);
        await _connections.SaveChangesAsync(cancellationToken);
    }

    private async Task TryEnsureDriveLayoutAsync(
        Guid userId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            await _driveLayout.EnsureAsync(userId, accessToken, cancellationToken);
        }
        catch (GoogleOAuthException)
        {
        }
        catch (ValidationFailedException)
        {
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }
    }

    private string NormalizeReturnUrl(string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.AbsolutePath != JobApplicationPath.OriginalString
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

        return origin + JobApplicationPath.OriginalString;
    }

    private string FallbackReturnUrl()
    {
        var origin = _frontendOrigins.Origins.Count > 0
            ? _frontendOrigins.Origins[0]
            : "http://127.0.0.1:5173";
        return origin + JobApplicationPath.OriginalString;
    }

    private static string AppendResult(string returnUrl, string result)
    {
        return $"{returnUrl}?google={Uri.EscapeDataString(result)}";
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
