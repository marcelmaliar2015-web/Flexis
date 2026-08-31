using Flexis.Application.Common;
using Flexis.Application.Google;

namespace Flexis.Application.JobApplication;

internal static class JobFinancialRules
{
    public static decimal NormalizeRate(decimal rate, string field)
    {
        if (rate < 0 || rate > 10000)
        {
            throw new ValidationFailedException($"{field} must be between 0 and 10000.");
        }

        return decimal.Round(rate, 4, MidpointRounding.AwayFromZero);
    }

    public static decimal Price(int applied, int interviews, decimal applyRate, decimal bonusRate)
    {
        return decimal.Round(
            (applied * applyRate) + (interviews * bonusRate),
            2,
            MidpointRounding.AwayFromZero);
    }

    public static (int Total, int Applied, int Interviews) CountStatuses(IReadOnlyList<JobListingRow> rows)
    {
        var total = 0;
        var applied = 0;
        var interviews = 0;
        foreach (var row in rows)
        {
            if (row.IsEmpty)
            {
                continue;
            }

            total++;
            if (string.Equals(row.Status.Trim(), "Interview", StringComparison.OrdinalIgnoreCase))
            {
                interviews++;
                continue;
            }

            if (string.Equals(row.Status.Trim(), "Applied", StringComparison.OrdinalIgnoreCase))
            {
                applied++;
            }
        }

        return (total, applied, interviews);
    }
}
