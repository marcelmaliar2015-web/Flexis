namespace Flexis.Infrastructure.Security;

public sealed class AuthSeedSettings
{
    public const string SectionName = "Auth:Seed";

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string UserPassword { get; set; } = string.Empty;

    public string ViewerEmail { get; set; } = string.Empty;

    public string ViewerPassword { get; set; } = string.Empty;
}
