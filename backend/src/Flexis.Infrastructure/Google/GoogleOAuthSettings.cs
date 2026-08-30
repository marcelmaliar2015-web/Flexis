namespace Flexis.Infrastructure.Google;

public sealed class GoogleOAuthSettings
{
    public const string SectionName = "Google";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string TokenProtectionKey { get; set; } = string.Empty;
}
