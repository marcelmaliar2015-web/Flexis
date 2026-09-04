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

    public static string ListingKey(JobListingRow listing)
    {
        return string.Join(
            '\u001f',
            listing.CompanyName.Trim().ToLowerInvariant(),
            listing.Position.Trim().ToLowerInvariant(),
            NormalizeLink(listing.Link));
    }

    public static string CellContent(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        if (trimmed.StartsWith("=HYPERLINK(", StringComparison.OrdinalIgnoreCase))
        {
            var start = trimmed.IndexOf('"');
            var end = start >= 0 ? trimmed.IndexOf('"', start + 1) : -1;
            if (start >= 0 && end > start)
            {
                return trimmed.Substring(start + 1, end - start - 1)
                    .Replace("\"\"", "\"", StringComparison.Ordinal)
                    .Trim();
            }

            return string.Empty;
        }

        return trimmed;
    }

    public static bool IsReady(JobListingRow listing)
    {
        return CellContent(listing.Download).Length > 0;
    }

    public static string NormalizeLink(string link)
    {
        var trimmed = CellContent(link);
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.AbsoluteUri.TrimEnd('/').ToLowerInvariant();
        }

        if (Uri.TryCreate($"https://{trimmed}", UriKind.Absolute, out var prefixed)
            && (prefixed.Host.Contains('.', StringComparison.Ordinal) || prefixed.Host.Contains(':', StringComparison.Ordinal)))
        {
            return prefixed.AbsoluteUri.TrimEnd('/').ToLowerInvariant();
        }

        return trimmed.TrimEnd('/').ToLowerInvariant();
    }

    public static bool IsTrackedStatus(string status)
    {
        return string.Equals(status, "Applied", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Interview", StringComparison.OrdinalIgnoreCase);
    }

    public static (int Total, int Ready, int NotReady, int Applied, int Interviews, int Unapplied) CountStatuses(
        IReadOnlyList<JobListingRow> rows,
        IReadOnlySet<string>? listingKeys = null)
    {
        var total = 0;
        var ready = 0;
        var notReady = 0;
        var applied = 0;
        var interviews = 0;
        var unapplied = 0;
        foreach (var row in rows)
        {
            if (row.IsEmpty)
            {
                continue;
            }

            if (listingKeys is not null && !listingKeys.Contains(ListingKey(row)))
            {
                continue;
            }

            total++;
            if (!IsReady(row))
            {
                notReady++;
                continue;
            }

            ready++;
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

        return (total, ready, notReady, applied, interviews, unapplied);
    }
}
