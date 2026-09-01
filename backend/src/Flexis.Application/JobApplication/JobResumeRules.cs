using Flexis.Application.Common;

namespace Flexis.Application.JobApplication;

public static class JobResumeRules
{
    public const int MinResumeStyle = 1;

    public const int MaxResumeStyle = 14;

    public const int MaxPromptLength = 20000;

    public const int MaxOwnerLength = 120;

    public const int MaxOwnerOptionLength = 120;

    public const int MaxOwnerOptions = 100;

    public static string NormalizePrompt(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length > MaxPromptLength)
        {
            throw new ValidationFailedException($"Prompt must be at most {MaxPromptLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeOwner(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length > MaxOwnerLength)
        {
            throw new ValidationFailedException($"Owner must be at most {MaxOwnerLength} characters.");
        }

        return trimmed;
    }

    public static int? NormalizeResumeStyle(int? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value < MinResumeStyle || value > MaxResumeStyle)
        {
            throw new ValidationFailedException($"Resume style must be between {MinResumeStyle} and {MaxResumeStyle}.");
        }

        return value;
    }

    public static IReadOnlyList<string> NormalizeOwnerOptions(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<string>(values.Count);
        foreach (var value in values)
        {
            var trimmed = value?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.Length > MaxOwnerOptionLength)
            {
                throw new ValidationFailedException($"Each owner option must be at most {MaxOwnerOptionLength} characters.");
            }

            if (!seen.Add(trimmed))
            {
                continue;
            }

            normalized.Add(trimmed);
            if (normalized.Count > MaxOwnerOptions)
            {
                throw new ValidationFailedException($"At most {MaxOwnerOptions} owner options are allowed.");
            }
        }

        return normalized;
    }
}
