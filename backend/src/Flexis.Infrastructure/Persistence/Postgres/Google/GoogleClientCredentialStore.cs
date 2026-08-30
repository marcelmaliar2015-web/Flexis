using Flexis.Application.Common;
using Flexis.Application.Google;
using Flexis.Domain.Google;
using Flexis.Infrastructure.Google;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flexis.Infrastructure.Persistence.Postgres.Google;

internal sealed class GoogleClientCredentialStore : IGoogleClientCredentialStore
{
    private readonly FlexisDbContext _db;
    private readonly IGoogleTokenProtector _protector;
    private readonly GoogleOAuthSettings _options;

    public GoogleClientCredentialStore(
        FlexisDbContext db,
        IGoogleTokenProtector protector,
        IOptions<GoogleOAuthSettings> options)
    {
        _db = db;
        _protector = protector;
        _options = options.Value;
    }

    public async Task<GoogleClientPair?> GetAsync(CancellationToken cancellationToken)
    {
        var stored = await CurrentAsync(cancellationToken);
        if (stored is not null
            && !string.IsNullOrWhiteSpace(stored.ClientId)
            && !string.IsNullOrWhiteSpace(stored.ClientSecretProtected))
        {
            return new GoogleClientPair(stored.ClientId, _protector.Unprotect(stored.ClientSecretProtected));
        }

        if (!string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            return new GoogleClientPair(_options.ClientId, _options.ClientSecret);
        }

        return null;
    }

    public async Task<GoogleClientSettingsDto> GetPublicAsync(CancellationToken cancellationToken)
    {
        var pair = await GetAsync(cancellationToken);
        if (pair is null)
        {
            return new GoogleClientSettingsDto(string.Empty, false);
        }

        return new GoogleClientSettingsDto(pair.ClientId, true);
    }

    public async Task SaveAsync(SaveGoogleClientSettingsRequest request, CancellationToken cancellationToken)
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
            await _db.GoogleClientCredentials.AddAsync(
                GoogleClientCredentials.Create(clientId, protectedSecret),
                cancellationToken);
        }
        else
        {
            stored.Replace(clientId, protectedSecret);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private Task<GoogleClientCredentials?> CurrentAsync(CancellationToken cancellationToken)
    {
        return _db.GoogleClientCredentials.OrderBy(credentials => credentials.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
