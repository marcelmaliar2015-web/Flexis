namespace Flexis.Application.Google;

public sealed record GoogleClientPair(string ClientId, string ClientSecret);

public sealed record GoogleClientSettingsDto(string ClientId, bool HasSecret);

public sealed record SaveGoogleClientSettingsRequest(string ClientId, string? ClientSecret);

public interface IGoogleClientCredentialStore
{
    Task<GoogleClientPair?> GetAsync(CancellationToken cancellationToken);

    Task<GoogleClientSettingsDto> GetPublicAsync(CancellationToken cancellationToken);

    Task SaveAsync(SaveGoogleClientSettingsRequest request, CancellationToken cancellationToken);
}
