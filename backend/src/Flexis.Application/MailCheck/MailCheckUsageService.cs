using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed class MailCheckUsageService
{
    private const int HistoryHours = 24 * 14;

    private readonly IMailCheckUsageHourRepository _hours;

    public MailCheckUsageService(IMailCheckUsageHourRepository hours)
    {
        _hours = hours;
    }

    public async Task RecordClassifyAsync(
        Guid userId,
        string model,
        OpenAiTokenUsage usage,
        CancellationToken cancellationToken)
    {
        if (!usage.HasTokens)
        {
            return;
        }

        var cost = OpenAiTokenPricing.EstimateUsd(model, usage.PromptTokens, usage.CompletionTokens);
        var now = DateTimeOffset.UtcNow;
        var existing = await _hours.GetByUserAndHourAsync(userId, now, cancellationToken);
        if (existing is null)
        {
            existing = MailCheckUsageHour.Create(userId, now);
            await _hours.AddAsync(existing, cancellationToken);
        }

        existing.AddCall(model, usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens, cost);
        await _hours.SaveChangesAsync(cancellationToken);
    }

    public async Task<MailCheckUsageDto> GetUsageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var recent = await _hours.ListRecentAsync(userId, HistoryHours, cancellationToken);
        var history = recent
            .OrderBy(item => item.CapturedHour)
            .Select(MapHour)
            .ToList();
        var lifetime = Sum(recent);
        var todayKey = DateOnly.FromDateTime(DateTime.UtcNow);
        var today = Sum(recent.Where(item => item.CapturedOn == todayKey));
        return new MailCheckUsageDto(lifetime, today, history);
    }

    private static MailCheckUsageHourDto MapHour(MailCheckUsageHour hour)
    {
        return new MailCheckUsageHourDto(
            hour.CapturedOn.ToString("yyyy-MM-dd"),
            hour.CapturedHour.ToString("o"),
            hour.UpdatedAt.ToString("o"),
            hour.CallCount,
            hour.PromptTokens,
            hour.CompletionTokens,
            hour.TotalTokens,
            hour.EstimatedCostUsd,
            hour.LastModel);
    }

    private static MailCheckUsageTotalsDto Sum(IEnumerable<MailCheckUsageHour> hours)
    {
        var callCount = 0;
        var prompt = 0;
        var completion = 0;
        var total = 0;
        decimal cost = 0;
        foreach (var hour in hours)
        {
            callCount += hour.CallCount;
            prompt += hour.PromptTokens;
            completion += hour.CompletionTokens;
            total += hour.TotalTokens;
            cost += hour.EstimatedCostUsd;
        }

        return new MailCheckUsageTotalsDto(callCount, prompt, completion, total, decimal.Round(cost, 8));
    }
}
