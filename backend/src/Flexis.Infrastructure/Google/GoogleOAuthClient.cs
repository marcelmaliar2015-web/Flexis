using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flexis.Application.Common;
using Flexis.Application.Google;
using Microsoft.Extensions.Options;

namespace Flexis.Infrastructure.Google;

internal sealed class GoogleOAuthClient : IGoogleOAuthGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly GoogleOAuthSettings _settings;
    private readonly IGoogleClientCredentialStore _clients;

    public GoogleOAuthClient(
        HttpClient http,
        IOptions<GoogleOAuthSettings> options,
        IGoogleClientCredentialStore clients)
    {
        _http = http;
        _settings = options.Value;
        _clients = clients;
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.RedirectUri))
        {
            return false;
        }

        return await _clients.GetAsync(cancellationToken) is not null;
    }

    public async Task<string> CreateAuthorizationUrlAsync(
        string state,
        string codeChallenge,
        CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(cancellationToken);
        var query = new Dictionary<string, string>
        {
            ["client_id"] = client.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = GoogleWorkspaceScopes.Request,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        return "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join(
            '&',
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    public async Task<GoogleOAuthTokenSet> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(cancellationToken);
        using var response = await _http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = client.ClientId,
                ["client_secret"] = client.ClientSecret,
                ["redirect_uri"] = _settings.RedirectUri,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = codeVerifier
            }),
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GoogleOAuthException(ReadGoogleError(payload, "Google token exchange failed."));
        }

        var token = JsonSerializer.Deserialize<GoogleTokenResponse>(payload, JsonOptions)
            ?? throw new GoogleOAuthException("Google token exchange returned an empty payload.");
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new GoogleOAuthException("Google token exchange did not return an access token.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn > 0 ? token.ExpiresIn : 3500);
        return new GoogleOAuthTokenSet(
            token.AccessToken,
            token.RefreshToken ?? string.Empty,
            expiresAt,
            token.Scope ?? GoogleWorkspaceScopes.Request);
    }

    public async Task<GoogleOAuthTokenSet> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(cancellationToken);
        using var response = await _http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = client.ClientId,
                ["client_secret"] = client.ClientSecret,
                ["grant_type"] = "refresh_token"
            }),
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GoogleOAuthException(ReadGoogleError(payload, "Google token refresh failed."));
        }

        var token = JsonSerializer.Deserialize<GoogleTokenResponse>(payload, JsonOptions)
            ?? throw new GoogleOAuthException("Google token refresh returned an empty payload.");
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new GoogleOAuthException("Google token refresh did not return an access token.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn > 0 ? token.ExpiresIn : 3500);
        return new GoogleOAuthTokenSet(
            token.AccessToken,
            token.RefreshToken ?? string.Empty,
            expiresAt,
            token.Scope ?? GoogleWorkspaceScopes.Request);
    }

    public async Task<GoogleUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GoogleOAuthException(ReadGoogleError(payload, "Google user info failed."));
        }

        var profile = JsonSerializer.Deserialize<GoogleUserInfoResponse>(payload, JsonOptions)
            ?? throw new GoogleOAuthException("Google user info returned an empty payload.");
        if (string.IsNullOrWhiteSpace(profile.Sub) || string.IsNullOrWhiteSpace(profile.Email))
        {
            throw new GoogleOAuthException("Google user info did not include an email.");
        }

        return new GoogleUserInfo(profile.Sub, profile.Email);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsync(
            "https://oauth2.googleapis.com/revoke",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = refreshToken
            }),
            cancellationToken);
        _ = response;
    }

    private async Task<GoogleClientPair> RequireClientAsync(CancellationToken cancellationToken)
    {
        return await _clients.GetAsync(cancellationToken)
            ?? throw new ValidationFailedException("An admin must save the Google Cloud client in Settings.");
    }

    private static string ReadGoogleError(string payload, string fallback)
    {
        try
        {
            var error = JsonSerializer.Deserialize<GoogleErrorResponse>(payload, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.ErrorDescription))
            {
                return error.ErrorDescription;
            }

            if (!string.IsNullOrWhiteSpace(error?.Error))
            {
                return error.Error;
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private sealed class GoogleUserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    private sealed class GoogleErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
