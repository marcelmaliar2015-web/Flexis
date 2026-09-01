using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flexis.Application.Common;
using Flexis.Application.Microsoft;
using Microsoft.Extensions.Options;

namespace Flexis.Infrastructure.Microsoft;

internal sealed class MicrosoftOAuthClient : IMicrosoftOAuthGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IMicrosoftClientCredentialStore _clients;
    private readonly MicrosoftOAuthSettings _settings;

    public MicrosoftOAuthClient(
        HttpClient http,
        IMicrosoftClientCredentialStore clients,
        IOptions<MicrosoftOAuthSettings> options)
    {
        _http = http;
        _clients = clients;
        _settings = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(120);
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(cancellationToken);
        return client is not null;
    }

    public async Task<string> CreateAuthorizationUrlAsync(
        string state,
        string codeChallenge,
        string scopes,
        CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(cancellationToken)
            ?? throw new MicrosoftOAuthException("Microsoft OAuth is not configured.");

        var tenant = string.IsNullOrWhiteSpace(_settings.TenantId) ? "common" : _settings.TenantId;
        var query = new Dictionary<string, string>
        {
            ["client_id"] = client.ClientId,
            ["redirect_uri"] = client.RedirectUri,
            ["response_type"] = "code",
            ["response_mode"] = "query",
            ["scope"] = scopes,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "consent",
        };

        var url = $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/oauth2/v2.0/authorize?"
            + string.Join(
                '&',
                query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return url;
    }

    public async Task<MicrosoftOAuthTokenSet> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(cancellationToken)
            ?? throw new MicrosoftOAuthException("Microsoft OAuth is not configured.");

        return await RequestTokensAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = client.RedirectUri,
                ["client_id"] = client.ClientId,
                ["client_secret"] = client.ClientSecret,
                ["code_verifier"] = codeVerifier,
            },
            cancellationToken);
    }

    public async Task<MicrosoftOAuthTokenSet> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var client = await RequireClientAsync(cancellationToken)
            ?? throw new MicrosoftOAuthException("Microsoft OAuth is not configured.");

        return await RequestTokensAsync(
            client,
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = client.ClientId,
                ["client_secret"] = client.ClientSecret,
            },
            cancellationToken);
    }

    public async Task<MicrosoftUserInfo> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/me?$select=id,mail,userPrincipalName");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new MicrosoftOAuthException(ReadError(payload, "Microsoft user info failed."));
        }

        var profile = JsonSerializer.Deserialize<GraphUser>(payload, JsonOptions)
            ?? throw new MicrosoftOAuthException("Microsoft user info returned an empty payload.");
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            throw new MicrosoftOAuthException("Microsoft user info did not include an id.");
        }

        var email = profile.Mail;
        if (string.IsNullOrWhiteSpace(email))
        {
            email = profile.UserPrincipalName;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new MicrosoftOAuthException("Microsoft user info did not include an email.");
        }

        return new MicrosoftUserInfo(profile.Id, email);
    }

    private async Task<MicrosoftOAuthTokenSet> RequestTokensAsync(
        MicrosoftOAuthClientPair client,
        Dictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        var tenant = string.IsNullOrWhiteSpace(_settings.TenantId) ? "common" : _settings.TenantId;
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(form),
        };
        using var response = await _http.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new MicrosoftOAuthException(ReadError(payload, "Microsoft token request failed."));
        }

        var tokens = JsonSerializer.Deserialize<TokenResponse>(payload, JsonOptions)
            ?? throw new MicrosoftOAuthException("Microsoft token request returned an empty payload.");
        if (string.IsNullOrWhiteSpace(tokens.AccessToken))
        {
            throw new MicrosoftOAuthException("Microsoft token request did not return an access token.");
        }

        var refresh = tokens.RefreshToken ?? string.Empty;
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn <= 0 ? 3600 : tokens.ExpiresIn);
        return new MicrosoftOAuthTokenSet(tokens.AccessToken, refresh, expiresAt, tokens.Scope ?? string.Empty);
    }

    private async Task<MicrosoftOAuthClientPair?> RequireClientAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.RedirectUri))
        {
            return null;
        }

        var pair = await _clients.GetAsync(cancellationToken);
        if (pair is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(pair.ClientId))
        {
            return null;
        }

        return new MicrosoftOAuthClientPair(pair.ClientId, pair.ClientSecret, _settings.RedirectUri);
    }

    private static string ReadError(string payload, string fallback)
    {
        try
        {
            var error = JsonSerializer.Deserialize<ErrorEnvelope>(payload, JsonOptions);
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

    private sealed record MicrosoftOAuthClientPair(string ClientId, string ClientSecret, string RedirectUri);

    private sealed class TokenResponse
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

    private sealed class GraphUser
    {
        public string? Id { get; set; }

        public string? Mail { get; set; }

        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; set; }
    }

    private sealed class ErrorEnvelope
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
