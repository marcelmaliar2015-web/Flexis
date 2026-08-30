using System.Net.Mail;
using Flexis.Application.Common;

namespace Flexis.Application.Users;

internal static class UserRules
{
    public static string NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 256)
        {
            throw new ValidationFailedException("Email is required and must be at most 256 characters.");
        }

        try
        {
            _ = new MailAddress(trimmed);
        }
        catch (FormatException)
        {
            throw new ValidationFailedException("Email is not valid.");
        }

        return trimmed.ToLowerInvariant();
    }

    public static string NormalizeDisplayName(string? displayName)
    {
        var trimmed = displayName?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > 100)
        {
            throw new ValidationFailedException("Display name is required and must be at most 100 characters.");
        }

        return trimmed;
    }

    public static void EnsurePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ValidationFailedException("Password must be at least 8 characters.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new ValidationFailedException("Password must include a letter and a digit.");
        }
    }
}
