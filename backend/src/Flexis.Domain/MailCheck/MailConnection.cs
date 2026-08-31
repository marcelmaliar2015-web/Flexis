namespace Flexis.Domain.MailCheck;

public sealed class MailConnection
{
    private MailConnection()
    {
        ExternalSubject = string.Empty;
        Email = string.Empty;
        RefreshTokenProtected = string.Empty;
        AccessTokenProtected = string.Empty;
        GrantedScopes = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public MailProvider Provider { get; private set; }

    public string ExternalSubject { get; private set; }

    public string Email { get; private set; }

    public string RefreshTokenProtected { get; private set; }

    public string AccessTokenProtected { get; private set; }

    public DateTimeOffset AccessTokenExpiresAt { get; private set; }

    public string GrantedScopes { get; private set; }

    public DateTimeOffset ConnectedAt { get; private set; }

    public static MailConnection CreateGmail(
        Guid userId,
        string externalSubject,
        string email,
        string refreshTokenProtected,
        string accessTokenProtected,
        DateTimeOffset accessTokenExpiresAt,
        string grantedScopes)
    {
        return new MailConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = MailProvider.Gmail,
            ExternalSubject = externalSubject,
            Email = email,
            RefreshTokenProtected = refreshTokenProtected,
            AccessTokenProtected = accessTokenProtected,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            GrantedScopes = grantedScopes,
            ConnectedAt = DateTimeOffset.UtcNow,
        };
    }

    public void ReplaceGmailCredentials(
        string externalSubject,
        string email,
        string refreshTokenProtected,
        string accessTokenProtected,
        DateTimeOffset accessTokenExpiresAt,
        string grantedScopes)
    {
        Provider = MailProvider.Gmail;
        ExternalSubject = externalSubject;
        Email = email;
        RefreshTokenProtected = refreshTokenProtected;
        AccessTokenProtected = accessTokenProtected;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        GrantedScopes = grantedScopes;
        ConnectedAt = DateTimeOffset.UtcNow;
    }

    public void ReplaceAccessToken(string accessTokenProtected, DateTimeOffset accessTokenExpiresAt)
    {
        AccessTokenProtected = accessTokenProtected;
        AccessTokenExpiresAt = accessTokenExpiresAt;
    }
}
