namespace Flexis.Domain.Users;

public sealed class User
{
    private User()
    {
        Email = string.Empty;
        DisplayName = string.Empty;
        PasswordHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    public UserRole Role { get; private set; }

    public string PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(
        string email,
        string displayName,
        UserRole role,
        string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = role,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }
}
