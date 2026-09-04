namespace Flexis.Application.MailCheck;

public static class OpenAiTokenPricing
{
    private static readonly Rate DefaultRate = new(0.15m, 0.60m);

    private static readonly (string Prefix, Rate Rate)[] Rates =
    [
        ("gpt-5.6-luna", new Rate(0.20m, 1.20m)),
        ("gpt-5.6-terra", new Rate(2.00m, 12.00m)),
        ("gpt-5.6-sol", new Rate(4.00m, 20.00m)),
        ("gpt-5.4-nano", new Rate(0.20m, 1.25m)),
        ("gpt-5.4-mini", new Rate(0.75m, 4.50m)),
        ("gpt-5.4", new Rate(2.50m, 15.00m)),
        ("gpt-5-nano", new Rate(0.05m, 0.40m)),
        ("gpt-5-mini", new Rate(0.25m, 2.00m)),
        ("gpt-5", new Rate(1.25m, 10.00m)),
        ("gpt-4.1-nano", new Rate(0.10m, 0.40m)),
        ("gpt-4.1-mini", new Rate(0.40m, 1.60m)),
        ("gpt-4.1", new Rate(2.00m, 8.00m)),
        ("gpt-4o-mini", new Rate(0.15m, 0.60m)),
        ("gpt-4o", new Rate(2.50m, 10.00m)),
        ("o4-mini", new Rate(1.10m, 4.40m)),
        ("o3-mini", new Rate(1.10m, 4.40m)),
        ("o3", new Rate(2.00m, 8.00m)),
        ("o1-mini", new Rate(1.10m, 4.40m)),
        ("o1", new Rate(15.00m, 60.00m))
    ];

    public static decimal EstimateUsd(string model, int promptTokens, int completionTokens)
    {
        var rate = RateFor(model);
        var input = Math.Max(0, promptTokens) / 1_000_000m * rate.InputPerMillion;
        var output = Math.Max(0, completionTokens) / 1_000_000m * rate.OutputPerMillion;
        return decimal.Round(input + output, 8, MidpointRounding.AwayFromZero);
    }

    private static Rate RateFor(string model)
    {
        var name = model.Trim().ToLowerInvariant();
        string? bestPrefix = null;
        Rate? bestRate = null;
        foreach (var entry in Rates)
        {
            if (!name.StartsWith(entry.Prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (bestPrefix is null || entry.Prefix.Length > bestPrefix.Length)
            {
                bestPrefix = entry.Prefix;
                bestRate = entry.Rate;
            }
        }

        return bestRate ?? DefaultRate;
    }

    private readonly record struct Rate(decimal InputPerMillion, decimal OutputPerMillion);
}
