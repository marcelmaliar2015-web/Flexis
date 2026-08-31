using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Application.Microsoft;
using Flexis.Domain.Microsoft;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flexis.Infrastructure.Persistence.Postgres.Microsoft;

internal sealed class MicrosoftClientCredentialStore : IMicrosoftClientCredentialStore
{
    private readonly FlexisDbContext _db;
    private readonly IGoogleTokenProtector _protector;
    private readonly MicrosoftOAuthSettings _options;

    public MicrosoftClientCredentialStore(
        FlexisDbContext db,
        IGoogleTokenProtector protector,
        IOptions<MicrosoftOAuthSettings> options)
    {
        _db = db;
        _protector = protector;
        _options = options.Value;
    }

    public async Task<MicrosoftClientPair?> GetAsync(CancellationToken cancellationToken)
    {
        var stored = await CurrentAsync(cancellationToken);
        if (stored is not null
            && !string.IsNullOrWhiteSpace(stored.ClientId)
            && !string.IsNullOrWhiteSpace(stored.ClientSecretProtected))
        {
            return new MicrosoftClientPair(stored.ClientId, _protector.Unprotect(stored.ClientSecretProtected));
        }

        if (!string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            return new MicrosoftClientPair(_options.ClientId, _options.ClientSecret);
        }

        return null;
    }

    public async Task<MicrosoftClientSettingsDto> GetPublicAsync(CancellationToken cancellationToken)
    {
        var pair = await GetAsync(cancellationToken);
        if (pair is null)
        {
            return new MicrosoftClientSettingsDto(string.Empty, false);
        }

        return new MicrosoftClientSettingsDto(pair.ClientId, true);
    }

    public async Task SaveAsync(SaveMicrosoftClientSettingsRequest request, CancellationToken cancellationToken)
    {
        var clientId = request.ClientId?.Trim() ?? string.Empty;
        if (clientId.Length is 0 or > 256)
        {
            throw new ValidationFailedException("Client ID is required and must be at most 256 characters.");
        }

        var stored = await CurrentAsync(cancellationToken);
        var secret = request.ClientSecret?.Trim() ?? string.Empty;
        if (secret.Length == 0)
        {
            if (stored is not null)
            {
                stored.Replace(clientId, stored.ClientSecretProtected);
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                throw new ValidationFailedException("Client secret is required.");
            }

            secret = _options.ClientSecret;
        }

        var protectedSecret = _protector.Protect(secret);
        if (stored is null)
        {
            await _db.MicrosoftClientCredentials.AddAsync(
                MicrosoftClientCredentials.Create(clientId, protectedSecret),
                cancellationToken);
        }
        else
        {
            stored.Replace(clientId, protectedSecret);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private Task<MicrosoftClientCredentials?> CurrentAsync(CancellationToken cancellationToken)
    {
        return _db.MicrosoftClientCredentials.OrderBy(credentials => credentials.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
