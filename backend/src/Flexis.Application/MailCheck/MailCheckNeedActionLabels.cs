using System.Text.Json;
using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public static class MailCheckNeedActionLabels
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static readonly IReadOnlyList<MailCheckLabel> Default =
    [
        MailCheckLabel.Schedule,
        MailCheckLabel.Assessment,
        MailCheckLabel.Availability
    ];

    public const string DefaultJson = "[\"schedule\",\"assessment\",\"availability\"]";

    public static IReadOnlyList<MailCheckLabel> Resolve(MailCheckSettings settings)
    {
        return Parse(settings.NeedActionLabelsJson);
    }

    public static IReadOnlyList<MailCheckLabel> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Default.ToList();
        }

        try
        {
            var raw = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            if (raw is null || raw.Count == 0)
            {
                return Default.ToList();
            }

            var labels = new List<MailCheckLabel>();
            foreach (var slug in raw)
            {
                var label = MailCheckLabelCatalog.ParseSlug(slug);
                if (label is not null && !labels.Contains(label.Value))
                {
                    labels.Add(label.Value);
                }
            }

            return labels.Count > 0 ? labels : Default.ToList();
        }
        catch (JsonException)
        {
            return Default.ToList();
        }
    }

    public static string Serialize(IReadOnlyList<MailCheckLabel> labels)
    {
        var slugs = labels
            .Select(MailCheckLabelCatalog.SlugFor)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return JsonSerializer.Serialize(slugs, JsonOptions);
    }

    public static IReadOnlyList<string> ToSlugList(IReadOnlyList<MailCheckLabel> labels)
    {
        return labels.Select(MailCheckLabelCatalog.SlugFor).ToList();
    }
}
