namespace Flexis.Application.Common;

public static class GoogleOAuthErrors
{
    public static bool IsCredentialFailure(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("invalid authentication", StringComparison.OrdinalIgnoreCase)
            || message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase)
            || message.Contains("expired or revoked", StringComparison.OrdinalIgnoreCase);
    }

    public static string FriendlyMessage(string message)
    {
        return IsCredentialFailure(message)
            ? "Gmail authorization expired. Reconnect Gmail on Job Application Settings."
            : message;
    }
}
