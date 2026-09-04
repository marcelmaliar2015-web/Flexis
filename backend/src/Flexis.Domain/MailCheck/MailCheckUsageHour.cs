namespace Flexis.Domain.MailCheck;

public sealed class MailCheckUsageHour
{
    private MailCheckUsageHour()
    {
        LastModel = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public DateOnly CapturedOn { get; private set; }

    public DateTimeOffset CapturedHour { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public int CallCount { get; private set; }

    public int PromptTokens { get; private set; }

    public int CompletionTokens { get; private set; }

    public int TotalTokens { get; private set; }

    public decimal EstimatedCostUsd { get; private set; }

    public string LastModel { get; private set; }

    public static DateTimeOffset TruncateToHour(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    public static MailCheckUsageHour Create(Guid userId, DateTimeOffset capturedHour)
    {
        var hour = TruncateToHour(capturedHour);
        return new MailCheckUsageHour
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CapturedOn = DateOnly.FromDateTime(hour.UtcDateTime),
            CapturedHour = hour,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastModel = string.Empty
        };
    }

    public void AddCall(
        string model,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        decimal estimatedCostUsd)
    {
        CallCount += 1;
        PromptTokens += Math.Max(0, promptTokens);
        CompletionTokens += Math.Max(0, completionTokens);
        TotalTokens += Math.Max(0, totalTokens > 0 ? totalTokens : promptTokens + completionTokens);
        EstimatedCostUsd += estimatedCostUsd < 0 ? 0 : estimatedCostUsd;
        if (!string.IsNullOrWhiteSpace(model))
        {
            LastModel = model.Trim();
        }

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
