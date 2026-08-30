namespace Flexis.Domain.Google;

public sealed class GoogleClientCredentials
{
    private GoogleClientCredentials()
    {
        ClientId = string.Empty;
        ClientSecretProtected = string.Empty;
    }

    public Guid Id { get; private set; }

    public string ClientId { get; private set; }

    public string ClientSecretProtected { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static GoogleClientCredentials Create(string clientId, string clientSecretProtected)
    {
        return new GoogleClientCredentials
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
