using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public static class MailCheckMailboxNames
{
    public static string For(MailCheckLabel label)
    {
        return $"Flexis/{MailCheckLabelCatalog.NameFor(label)}";
    }

    public static IEnumerable<string> LookupNames(MailCheckLabel label)
    {
        yield return For(label);
        var legacy = MailCheckLabelCatalog.NameFor(label);
        if (!string.Equals(legacy, For(label), StringComparison.Ordinal))
        {
            yield return legacy;
        }
    }

    public static IEnumerable<string> AllLookupNames()
    {
        return MailCheckLabelCatalog.All
            .SelectMany(LookupNames)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
