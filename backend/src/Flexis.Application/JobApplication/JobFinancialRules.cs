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

    public static (int Total, int Applied, int Interviews, int Unapplied) CountStatuses(
        IReadOnlyList<JobListingRow> rows)
    {
        var total = 0;
        var applied = 0;
        var interviews = 0;
        var unapplied = 0;
        foreach (var row in rows)
        {
            if (row.IsEmpty)
            {
                continue;
            }

            total++;
            var status = row.Status.Trim();
            if (status.Length == 0)
            {
                unapplied++;
                continue;
            }

            if (string.Equals(status, "Interview", StringComparison.OrdinalIgnoreCase))
            {
                interviews++;
                continue;
            }

            if (string.Equals(status, "Applied", StringComparison.OrdinalIgnoreCase))
            {
                applied++;
            }
        }

        return (total, applied, interviews, unapplied);
    }
}
