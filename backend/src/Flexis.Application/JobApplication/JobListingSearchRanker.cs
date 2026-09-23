using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Flexis.Application.JobApplication;

internal static class JobListingSearchRanker
{
    private const double MinScore = 28d;

    private static readonly Regex TokenSplit = new(@"[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static double? Score(
        string profile,
        string companyName,
        string position,
        string link,
        string jd,
        string status,
        string? queryProfile,
        string? queryCompany,
        string? queryPosition,
        string? queryLink,
        string? queryJd,
        string? queryStatus)
    {
        if (!string.IsNullOrWhiteSpace(queryProfile)
            && !string.Equals(profile.Trim(), queryProfile.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(queryStatus)
            && !string.Equals(status.Trim(), queryStatus.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var weights = new List<(double Weight, double Score)>();
        AddField(weights, 2.4, queryCompany, companyName, FieldKind.Company);
        AddField(weights, 2.2, queryPosition, position, FieldKind.ShortText);
        AddField(weights, 1.8, queryLink, link, FieldKind.Link);
        AddField(weights, 1.6, queryJd, jd, FieldKind.LongText);

        if (weights.Count == 0)
        {
            return 100d;
        }

        var totalWeight = weights.Sum(item => item.Weight);
        var score = weights.Sum(item => item.Weight * item.Score) / totalWeight;
        return score >= MinScore ? Math.Round(score, 1) : null;
    }

    private static void AddField(
        List<(double Weight, double Score)> weights,
        double weight,
        string? query,
        string value,
        FieldKind kind)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        weights.Add((weight, FieldScore(query, value, kind)));
    }

    private static double FieldScore(string query, string value, FieldKind kind)
    {
        var q = Normalize(query);
        var v = Normalize(value);
        if (q.Length == 0)
        {
            return 0;
        }

        if (v.Length == 0)
        {
            return 0;
        }

        if (string.Equals(q, v, StringComparison.Ordinal))
        {
            return 100;
        }

        if (v.Contains(q, StringComparison.Ordinal) || q.Contains(v, StringComparison.Ordinal))
        {
            var shorter = Math.Min(q.Length, v.Length);
            var longer = Math.Max(q.Length, v.Length);
            return Clamp(78 + 22d * shorter / longer);
        }

        var tokenScore = TokenCoverageScore(q, v);
        var editScore = kind == FieldKind.LongText
            ? PhraseWindowScore(q, v)
            : SimilarityRatio(q, v) * 100;

        if (kind == FieldKind.Company)
        {
            var companyBoost = CompanyNameMatcher.IsMatch(value, query) ? 92d : 0d;
            return Clamp(Math.Max(Math.Max(tokenScore, editScore), companyBoost));
        }

        if (kind == FieldKind.Link)
        {
            var linkQ = NormalizeLink(query);
            var linkV = NormalizeLink(value);
            if (linkQ.Length > 0 && linkV.Length > 0)
            {
                if (linkV.Contains(linkQ, StringComparison.Ordinal) || linkQ.Contains(linkV, StringComparison.Ordinal))
                {
                    return 96;
                }

                return Clamp(Math.Max(tokenScore, SimilarityRatio(linkQ, linkV) * 100));
            }
        }

        return Clamp(Math.Max(tokenScore, editScore));
    }

    private static double TokenCoverageScore(string query, string value)
    {
        var queryTokens = Tokens(query);
        var valueTokens = Tokens(value);
        if (queryTokens.Count == 0 || valueTokens.Count == 0)
        {
            return 0;
        }

        var valueSet = valueTokens.ToHashSet(StringComparer.Ordinal);
        double hit = 0;
        foreach (var token in queryTokens)
        {
            if (valueSet.Contains(token))
            {
                hit += 1;
                continue;
            }

            var best = valueTokens.Max(candidate => SimilarityRatio(token, candidate));
            if (best >= 0.82)
            {
                hit += best;
            }
        }

        return Clamp(100d * hit / queryTokens.Count);
    }

    private static double PhraseWindowScore(string query, string value)
    {
        if (query.Length <= 24)
        {
            return SimilarityRatio(query, value) * 100;
        }

        var window = Math.Min(value.Length, Math.Max(query.Length + 12, query.Length));
        if (window > value.Length)
        {
            window = value.Length;
        }

        var best = 0d;
        var step = Math.Max(8, window / 6);
        for (var start = 0; start + window <= value.Length; start += step)
        {
            var end = Math.Min(value.Length, start + window);
            var slice = value[start..end];
            best = Math.Max(best, SimilarityRatio(query, slice));
            if (best >= 0.94)
            {
                break;
            }
        }

        return best * 100;
    }

    private static double SimilarityRatio(string left, string right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return 0;
        }

        if (string.Equals(left, right, StringComparison.Ordinal))
        {
            return 1;
        }

        var distance = Levenshtein(left, right);
        var maxLen = Math.Max(left.Length, right.Length);
        return 1d - (double)distance / maxLen;
    }

    private static int Levenshtein(string left, string right)
    {
        var n = left.Length;
        var m = right.Length;
        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        var prev = new int[m + 1];
        var curr = new int[m + 1];
        for (var j = 0; j <= m; j++)
        {
            prev[j] = j;
        }

        for (var i = 1; i <= n; i++)
        {
            curr[0] = i;
            var leftChar = left[i - 1];
            for (var j = 1; j <= m; j++)
            {
                var cost = leftChar == right[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[m];
    }

    private static List<string> Tokens(string value)
    {
        return TokenSplit.Split(value)
            .Where(token => token.Length > 1)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string Normalize(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormKD);
        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(ch);
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static string NormalizeLink(string value)
    {
        var normalized = Normalize(value)
            .Replace("https://", string.Empty, StringComparison.Ordinal)
            .Replace("http://", string.Empty, StringComparison.Ordinal)
            .Trim('/');
        return normalized;
    }

    private static double Clamp(double value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 100)
        {
            return 100;
        }

        return value;
    }

    private enum FieldKind
    {
        Company,
        ShortText,
        Link,
        LongText
    }
}
