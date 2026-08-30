using Flexis.Application.Common;

namespace Flexis.Application.JobApplication;

internal static class JobCatalogRules
{
    public const string DefaultSourceLocation = "US";

    public static string NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 200)
        {
            throw new ValidationFailedException("Title is required and must be at most 200 characters.");
        }

        EnsureSheetSafeName(trimmed, "Title");
        return trimmed;
    }

    public static string NormalizeLocationName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 100)
        {
            throw new ValidationFailedException("Location is required and must be at most 100 characters.");
        }

        EnsureSheetSafeName(trimmed, "Location");
        return trimmed;
    }

    public static string SheetTabName(string title)
    {
        return title.Length <= 100 ? title : title[..100].TrimEnd();
    }

    private static void EnsureSheetSafeName(string value, string field)
    {
        if (value.IndexOfAny([':', '\\', '/', '?', '*', '[', ']']) >= 0)
        {
            throw new ValidationFailedException($"{field} cannot contain : \\ / ? * [ ].");
        }
    }
}
