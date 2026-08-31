using System.Globalization;
using System.Text;

namespace Flexis.Application.JobApplication;

internal static class CompanyNameMatcher
{
    private static readonly HashSet<string> FillerTokens = new(StringComparer.Ordinal)
    {
        "and", "the"
    };

    private static readonly HashSet<string> TrailingNoise = new(StringComparer.Ordinal)
    {
        "ltd", "limited", "llc", "inc", "incorporated", "corp", "corporation", "company", "co",
        "plc", "gmbh", "ag", "nv", "bv", "sa", "sas", "sarl", "srl", "spa", "oy", "ab", "as",
        "kft", "ltda", "pte", "pty", "kk", "llp", "lp", "pc", "pllc", "se", "kg", "ohg", "eurl",
        "sl", "cv", "pt", "sdn", "bhd", "jsc", "ulc", "unlimited", "proprietary",
        "holdings", "holding", "group", "partners", "partner",
        "us", "usa", "uk", "uae", "eu", "emea", "apac", "anz", "latam", "na", "nz",
        "com", "net", "org", "io", "app", "dev", "cloud"
    };

    public static bool IsMatch(string listingCompany, string bannedCompany)
    {
        var listing = DistinctiveTokens(listingCompany);
        var banned = DistinctiveTokens(bannedCompany);
        if (listing.Count == 0 || banned.Count == 0)
        {
            return false;
        }

        if (SameTokens(listing, banned))
        {
            return true;
        }

        var listingCompact = string.Concat(listing);
        var bannedCompact = string.Concat(banned);
        return listingCompact.Length >= 4
            && bannedCompact.Length >= 4
            && listingCompact == bannedCompact;
    }

    public static string MatchKey(string companyName)
    {
        var tokens = DistinctiveTokens(companyName);
        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(' ', tokens.OrderBy(token => token, StringComparer.Ordinal));
    }

    private static bool SameTokens(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        return left.OrderBy(token => token, StringComparer.Ordinal)
            .SequenceEqual(right.OrderBy(token => token, StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> DistinctiveTokens(string value)
    {
        var folded = Fold(value);
        if (folded.Length == 0)
        {
            return [];
        }

        var tokens = folded
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !FillerTokens.Contains(token))
            .ToList();
        while (tokens.Count > 0 && TrailingNoise.Contains(tokens[^1]))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return MergeInitials(tokens);
    }

    private static List<string> MergeInitials(List<string> tokens)
    {
        var merged = new List<string>();
        var index = 0;
        while (index < tokens.Count)
        {
            if (tokens[index].Length != 1)
            {
                merged.Add(tokens[index]);
                index++;
                continue;
            }

            var combined = tokens[index];
            index++;
            while (index < tokens.Count && tokens[index].Length == 1)
            {
                combined += tokens[index];
                index++;
            }

            merged.Add(combined);
        }

        return merged;
    }

    private static string Fold(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is '&' or '+')
            {
                builder.Append(" and ");
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                continue;
            }

            builder.Append(' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
