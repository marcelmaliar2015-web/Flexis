using Flexis.Application.Google;
using Microsoft.Extensions.Options;

namespace Flexis.Infrastructure.Google;

internal sealed class ConfigurationFrontendOrigins : IFrontendOrigins
{
    public ConfigurationFrontendOrigins(IOptions<FrontendOriginSettings> options)
    {
        Origins = options.Value.Origins;
    }

    public IReadOnlyList<string> Origins { get; }
}

public sealed class FrontendOriginSettings
{
    public const string SectionName = "Frontend";

    public string[] Origins { get; set; } = [];
}
