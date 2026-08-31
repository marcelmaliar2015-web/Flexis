namespace Flexis.Application.Microsoft;

public sealed record MicrosoftClientPair(string ClientId, string ClientSecret);

public sealed record MicrosoftClientSettingsDto(string ClientId, bool HasSecret);

public sealed record SaveMicrosoftClientSettingsRequest(string ClientId, string? ClientSecret);

public interface IMicrosoftClientCredentialStore
{
    Task<MicrosoftClientPair?> GetAsync(CancellationToken cancellationToken);

    Task<MicrosoftClientSettingsDto> GetPublicAsync(CancellationToken cancellationToken);

    Task SaveAsync(SaveMicrosoftClientSettingsRequest request, CancellationToken cancellationToken);
}
