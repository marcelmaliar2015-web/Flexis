using Flexis.Application.Common;

namespace Flexis.Application.JobApplication;

internal static class JobCatalogRules
{
    public static string NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 200)
        {
            throw new ValidationFailedException("Title is required and must be at most 200 characters.");
        }

        return trimmed;
    }

    public static string NormalizeUrl(string? url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 2048)
        {
            throw new ValidationFailedException("URL is required and must be at most 2048 characters.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationFailedException("URL must be an http or https address.");
        }

        return trimmed;
    }
}
