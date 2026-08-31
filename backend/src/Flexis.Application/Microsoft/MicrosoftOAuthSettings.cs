namespace Flexis.Application.Microsoft;

public sealed class MicrosoftOAuthSettings
{
    public const string SectionName = "Microsoft";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string TenantId { get; set; } = "common";
}
