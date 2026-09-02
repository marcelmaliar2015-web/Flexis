using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public static class MailCheckLabelCatalog
{
    public static readonly IReadOnlyList<MailCheckLabel> All =
    [
        MailCheckLabel.Rejected,
        MailCheckLabel.Applied,
        MailCheckLabel.Schedule,
        MailCheckLabel.Scheduled,
        MailCheckLabel.Assessment,
        MailCheckLabel.Availability,
        MailCheckLabel.Success,
        MailCheckLabel.Other
    ];

    public static MailCheckLabel Parse(string? raw)
    {
        return ParseSlug(raw) ?? MailCheckLabel.Other;
    }

    public static MailCheckLabel? ParseSlug(string? raw)
    {
        var normalized = raw?.Trim().ToLowerInvariant().Replace('-', '_') ?? string.Empty;
        return normalized switch
        {
            "rejected" => MailCheckLabel.Rejected,
            "applied" => MailCheckLabel.Applied,
            "schedule" => MailCheckLabel.Schedule,
            "scheduled" => MailCheckLabel.Scheduled,
            "assessment" => MailCheckLabel.Assessment,
            "availability" => MailCheckLabel.Availability,
            "success" => MailCheckLabel.Success,
            "other" => MailCheckLabel.Other,
            _ => null
        };
    }

    public static string NameFor(MailCheckLabel label)
    {
        return label switch
        {
            MailCheckLabel.Rejected => "Rejected",
            MailCheckLabel.Applied => "Applied",
            MailCheckLabel.Schedule => "Schedule",
            MailCheckLabel.Scheduled => "Scheduled",
            MailCheckLabel.Assessment => "Assessment",
            MailCheckLabel.Availability => "Availability",
            MailCheckLabel.Success => "Success",
            MailCheckLabel.Other => "Other",
            _ => "Other"
        };
    }

    public static string SlugFor(MailCheckLabel label)
    {
        return label.ToString().ToLowerInvariant();
    }
}
