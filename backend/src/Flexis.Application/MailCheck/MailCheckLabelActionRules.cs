using System.Text.Json;
using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public static class MailCheckLabelActionRules
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> DefaultActions =
        new Dictionary<MailCheckLabel, MailCheckMailboxAction>
        {
            [MailCheckLabel.Rejected] = MailCheckMailboxAction.Trash,
            [MailCheckLabel.Applied] = MailCheckMailboxAction.Keep,
            [MailCheckLabel.Schedule] = MailCheckMailboxAction.Pin,
            [MailCheckLabel.Scheduled] = MailCheckMailboxAction.Pin,
            [MailCheckLabel.Assessment] = MailCheckMailboxAction.Pin,
            [MailCheckLabel.Availability] = MailCheckMailboxAction.Pin,
            [MailCheckLabel.Success] = MailCheckMailboxAction.Pin,
            [MailCheckLabel.Other] = MailCheckMailboxAction.Keep,
            [MailCheckLabel.LessImportant] = MailCheckMailboxAction.Trash
        };

    public static string DefaultJson => Serialize(DefaultActions);

    public static IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> Resolve(MailCheckSettings settings)
    {
        return Parse(settings.LabelActionsJson);
    }

    public static IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> Parse(string? json)
    {
        var result = new Dictionary<MailCheckLabel, MailCheckMailboxAction>(DefaultActions);
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            if (raw is null)
            {
                return result;
            }

            foreach (var (key, value) in raw)
            {
                var label = MailCheckLabelCatalog.ParseSlug(key);
                var action = ParseActionSlug(value);
                if (label is not null && action is not null)
                {
                    result[label.Value] = action.Value;
                }
            }
        }
        catch (JsonException)
        {
        }

        return result;
    }

    public static string Serialize(IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> rules)
    {
        var dict = MailCheckLabelCatalog.All.ToDictionary(
            MailCheckLabelCatalog.SlugFor,
            label => ActionSlug(rules.TryGetValue(label, out var action) ? action : DefaultActions[label]));
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    public static IReadOnlyDictionary<string, string> ToSlugMap(
        IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> rules)
    {
        return MailCheckLabelCatalog.All.ToDictionary(
            MailCheckLabelCatalog.SlugFor,
            label => ActionSlug(rules[label]));
    }

    public static IReadOnlyList<MailCheckLabel> PinLabels(
        IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> rules)
    {
        return MailCheckLabelCatalog.All
            .Where(label => rules[label] == MailCheckMailboxAction.Pin)
            .ToList();
    }

    public static IReadOnlyList<MailCheckLabel> TrashLabels(
        IReadOnlyDictionary<MailCheckLabel, MailCheckMailboxAction> rules)
    {
        return MailCheckLabelCatalog.All
            .Where(label => rules[label] == MailCheckMailboxAction.Trash)
            .ToList();
    }

    public static MailCheckMailboxAction? ParseActionSlug(string? raw)
    {
        var normalized = raw?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "pin" => MailCheckMailboxAction.Pin,
            "trash" => MailCheckMailboxAction.Trash,
            "keep" => MailCheckMailboxAction.Keep,
            _ => null
        };
    }

    public static string ActionSlug(MailCheckMailboxAction action)
    {
        return action switch
        {
            MailCheckMailboxAction.Pin => "pin",
            MailCheckMailboxAction.Trash => "trash",
            MailCheckMailboxAction.Keep => "keep",
            _ => "keep"
        };
    }
}
