namespace Flexis.Domain.Google;

public sealed class GoogleConnection
{
    private GoogleConnection()
    {
        GoogleSubject = string.Empty;
        GoogleEmail = string.Empty;
        RefreshTokenProtected = string.Empty;
        AccessTokenProtected = string.Empty;
        GrantedScopes = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string GoogleSubject { get; private set; }

    public string GoogleEmail { get; private set; }

    public string RefreshTokenProtected { get; private set; }

    public string AccessTokenProtected { get; private set; }

    public DateTimeOffset AccessTokenExpiresAt { get; private set; }

    public string GrantedScopes { get; private set; }

    public DateTimeOffset ConnectedAt { get; private set; }

    public static GoogleConnection Create(
        Guid userId,
        string googleSubject,
        string googleEmail,
        string refreshTokenProtected,
        string accessTokenProtected,
        DateTimeOffset accessTokenExpiresAt,
        string grantedScopes)
    {
        return new GoogleConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GoogleSubject = googleSubject,
            GoogleEmail = googleEmail,
            RefreshTokenProtected = refreshTokenProtected,
            AccessTokenProtected = accessTokenProtected,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            GrantedScopes = grantedScopes,
            ConnectedAt = DateTimeOffset.UtcNow
        };
    }

    public void ReplaceCredentials(
        string googleSubject,
        string googleEmail,
        string refreshTokenProtected,
        string accessTokenProtected,
        DateTimeOffset accessTokenExpiresAt,
        string grantedScopes)
    {
        GoogleSubject = googleSubject;
        GoogleEmail = googleEmail;
        RefreshTokenProtected = refreshTokenProtected;
        AccessTokenProtected = accessTokenProtected;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        GrantedScopes = grantedScopes;
        ConnectedAt = DateTimeOffset.UtcNow;
    }
}
