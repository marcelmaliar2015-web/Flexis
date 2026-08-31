namespace Flexis.Domain.Microsoft;

public sealed class MicrosoftClientCredentials
{
    private MicrosoftClientCredentials()
    {
        ClientId = string.Empty;
        ClientSecretProtected = string.Empty;
    }

    public Guid Id { get; private set; }

    public string ClientId { get; private set; }

    public string ClientSecretProtected { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static MicrosoftClientCredentials Create(string clientId, string clientSecretProtected)
    {
        return new MicrosoftClientCredentials
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ClientSecretProtected = clientSecretProtected,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Replace(string clientId, string clientSecretProtected)
    {
        ClientId = clientId;
        ClientSecretProtected = clientSecretProtected;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
