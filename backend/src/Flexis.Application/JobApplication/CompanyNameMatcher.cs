using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Flexis.Application.JobApplication;

internal static class CompanyNameMatcher
{
    private static readonly HashSet<string> LeadingWords = new(StringComparer.Ordinal) { "the" };

    private static readonly HashSet<string> CommonSubdomains = new(StringComparer.Ordinal)
    {
        "www", "careers", "career", "jobs", "job", "apply", "app", "portal", "workday",
        "greenhouse", "lever", "boards", "ats", "recruiting", "recruitment"
    };

    private static readonly string[] DomainSuffixPhrases =
    [
        "com", "net", "org", "io", "ai", "co", "so", "me", "ly", "tv", "fm", "xyz", "app", "dev",
        "cloud", "tech", "software", "systems", "global", "biz", "info", "edu", "gov",
        "us", "uk", "ca", "de", "fr", "jp", "kr", "cn", "in", "au", "sg", "hk", "tw", "nl", "se",
        "no", "fi", "dk", "es", "it", "ch", "br", "mx", "za", "ae"
    ];

    private static readonly string[] LegalSuffixPhrases =
    [
        "inc", "incorporated", "llc", "l l c", "limited liability company", "ltd", "limited",
        "corp", "corporation", "co", "company", "companies", "plc", "p l c", "lp", "l p", "llp",
        "l l p", "lllp", "pc", "p c", "pa", "p a", "pte", "pte ltd", "private limited", "pvt",
        "pvt ltd", "pty", "pty ltd", "sdn bhd", "bhd", "gmbh", "ag", "kg", "kgaa", "ug", "sa",
        "s a", "sas", "sasu", "sarl", "s a r l", "bv", "b v", "nv", "n v", "oy", "ab", "as", "asa",
        "aps", "a s", "kk", "k k", "kabushiki kaisha", "srl", "s r l", "spa", "s p a", "sro",
        "s r o", "sp z o o", "z o o", "ltda", "eireli", "ooo", "oao", "zao", "jsc", "cjsc", "ulc",
        "uc"
    ];

    private static readonly string[] GenericDescriptorSuffixPhrases =
    [
        "group", "groups", "holding", "holdings", "solution", "solutions", "lab", "labs",
        "laboratory", "laboratories", "technology", "technologies", "tech", "software", "system",
        "systems", "service", "services", "consulting", "consultants", "consultancy", "digital",
        "cloud", "platform", "platforms", "network", "networks", "ai", "artificial intelligence",
        "machine learning", "ml", "analytics", "data", "research", "science", "sciences", "media",
        "marketing", "communications", "communication", "health", "healthcare", "medical", "pharma",
        "biotech", "bio", "financial", "finance", "bank", "banking", "global", "international",
        "worldwide", "studio", "studios", "agency", "academy", "institute", "institutes",
        "engineering", "manufacturing", "industrial", "industries", "online", "mobile", "internet",
        "partners", "partner", "ventures", "venture", "capital", "management", "investment",
        "investments", "enterprise", "enterprises"
    ];

    private static readonly string[] LocationSuffixPhrases =
    [
        "usa", "us", "u s", "america", "americas", "north america", "united states", "uk", "u k",
        "united kingdom", "canada", "europe", "emea", "apac", "asia", "japan", "korea", "china",
        "india", "singapore", "australia", "germany", "france", "spain", "italy", "netherlands",
        "sweden", "norway", "denmark", "finland", "brazil", "mexico"
    ];

    public static bool IsMatch(string listingCompany, string bannedCompany)
    {
        var listingKeys = CreateMatchKeys(listingCompany);
        var bannedKeys = CreateMatchKeys(bannedCompany);
        if (listingKeys.Count == 0 || bannedKeys.Count == 0)
        {
            return false;
        }

        return listingKeys.Overlaps(bannedKeys);
    }

    public static string MatchKey(string companyName)
    {
        var keys = CreateMatchKeys(companyName);
        if (keys.Count == 0)
        {
            return string.Empty;
        }

        var canonical = keys
            .Where(IsUsableKey)
            .OrderByDescending(key => key.Contains(' ', StringComparison.Ordinal) ? 1 : 0)
            .ThenByDescending(key => key.Length)
            .First();

        return canonical.Length <= 200 ? canonical : canonical[..200];
    }

    private static HashSet<string> CreateMatchKeys(string raw)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var normalizedText = NormalizeCompanyText(raw);
        AddTokenVariants(keys, Tokenize(normalizedText));

        var domainTokens = ExtractDomainTokens(raw);
        if (domainTokens.Count > 0)
        {
            AddTokenVariants(keys, domainTokens);
        }

        return keys;
    }

    private static void AddTokenVariants(HashSet<string> keys, IReadOnlyList<string> originalTokens)
    {
        var tokens = CleanTokens(originalTokens);
        if (tokens.Count == 0)
        {
            return;
        }

        AddKeysFromTokens(keys, tokens);

        tokens = StripLeadingWords(tokens);
        AddKeysFromTokens(keys, tokens);

        var stripped = tokens.ToList();
        stripped = StripSuffixPhrasesRepeated(stripped, LegalSuffixPhrases);
        AddKeysFromTokens(keys, stripped);

        stripped = StripSuffixPhrasesRepeated(stripped, DomainSuffixPhrases);
        AddKeysFromTokens(keys, stripped);

        stripped = StripSuffixPhrasesRepeated(stripped, GenericDescriptorSuffixPhrases);
        AddKeysFromTokens(keys, stripped);

        stripped = StripSuffixPhrasesRepeated(stripped, LocationSuffixPhrases);
        AddKeysFromTokens(keys, stripped);

        var noAnd = stripped.Where(token => token != "and").ToList();
        AddKeysFromTokens(keys, noAnd);
        AddAcronymKey(keys, stripped);
    }

    private static void AddKeysFromTokens(HashSet<string> keys, IReadOnlyList<string> tokens)
    {
        var cleaned = CleanTokens(tokens);
        if (cleaned.Count == 0)
        {
            return;
        }

        var spaced = string.Join(' ', cleaned);
        var compact = string.Join(string.Empty, cleaned);
        if (IsUsableKey(spaced))
        {
            keys.Add(spaced);
        }

        if (IsUsableKey(compact))
        {
            keys.Add(compact);
        }
    }

    private static void AddAcronymKey(HashSet<string> keys, IReadOnlyList<string> tokens)
    {
        var cleaned = CleanTokens(tokens);
        if (cleaned.Count is < 2 or > 6)
        {
            return;
        }

        var acronym = string.Concat(cleaned.Where(token => token.Length > 1).Select(token => token[0]));
        if (IsUsableKey(acronym))
        {
            keys.Add(acronym);
        }
    }

    private static string NormalizeCompanyText(string value)
    {
        var text = value ?? string.Empty;
        text = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character);
        }

        text = builder.ToString()
            .Replace('\u00A0', ' ')
            .Replace("\u200B", string.Empty, StringComparison.Ordinal)
            .Replace("\u200C", string.Empty, StringComparison.Ordinal)
            .Replace("\u200D", string.Empty, StringComparison.Ordinal)
            .Replace("\uFEFF", string.Empty, StringComparison.Ordinal)
            .Replace("&", " and ", StringComparison.Ordinal)
            .Replace("+", " plus ", StringComparison.Ordinal)
            .Replace("@", " at ", StringComparison.Ordinal)
            .ToLowerInvariant();

        text = Regex.Replace(text, @"\([^)]*\)", " ");
        text = Regex.Replace(text, @"\[[^\]]*\]", " ");
        text = Regex.Replace(text, @"[.,'’""`~!?:;|\\/_\-–—]+", " ");
        text = Regex.Replace(text, @"[^a-z0-9\s]", " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static List<string> Tokenize(string normalizedText)
    {
        return CleanTokens(normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<string> CleanTokens(IEnumerable<string> tokens)
    {
        return tokens
            .Select(token => token.Trim().ToLowerInvariant())
            .Where(token => token.Length > 0)
            .ToList();
    }

    private static List<string> ExtractDomainTokens(string value)
    {
        var text = (value ?? string.Empty).Trim().ToLowerInvariant();
        text = Regex.Replace(text, @"^https?://", string.Empty);
        text = Regex.Replace(text, @"^www\.", string.Empty);
        text = text.Split(['/', '?', '#'], StringSplitOptions.RemoveEmptyEntries)[0];

        if (!text.Contains('.', StringComparison.Ordinal))
        {
            return [];
        }

        var parts = text
            .Split('.')
            .Select(NormalizeCompanyText)
            .Where(part => part.Length > 0)
            .ToList();

        var withoutSubdomains = parts.Where(part => !CommonSubdomains.Contains(part)).ToList();
        return CleanTokens(StripSuffixPhrasesRepeated(withoutSubdomains, DomainSuffixPhrases));
    }

    private static List<string> StripLeadingWords(IReadOnlyList<string> tokens)
    {
        var output = tokens.ToList();
        while (output.Count > 0 && LeadingWords.Contains(output[0]))
        {
            output.RemoveAt(0);
        }

        return output;
    }

    private static List<string> StripSuffixPhrasesRepeated(
        IReadOnlyList<string> tokens,
        IReadOnlyList<string> suffixPhrases)
    {
        var output = tokens.ToList();
        var changed = true;
        while (changed && output.Count > 0)
        {
            changed = false;
            foreach (var phrase in suffixPhrases)
            {
                var phraseTokens = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (phraseTokens.Length > output.Count)
                {
                    continue;
                }

                var tail = string.Join(' ', output.Skip(output.Count - phraseTokens.Length));
                if (!string.Equals(tail, phrase, StringComparison.Ordinal))
                {
                    continue;
                }

                output = output.Take(output.Count - phraseTokens.Length).ToList();
                changed = true;
                break;
            }
        }

        return output;
    }

    private static bool IsUsableKey(string key)
    {
        key = key.Trim();
        if (key.Length < 2)
        {
            return false;
        }

        return !key.All(char.IsDigit);
    }
}
