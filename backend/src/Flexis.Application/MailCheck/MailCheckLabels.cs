using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public static class MailCheckLabels
{
    public const string InterviewScheduled = "Interview Scheduled";
    public const string WaitingForAnswer = "Waiting for answer";
    public const string NeedToSchedule = "Need to Schedule/Availability";
    public const string Others = "Others";

    public static readonly IReadOnlyList<MailCheckDecision> KeepDecisions =
    [
        MailCheckDecision.InterviewScheduled,
        MailCheckDecision.WaitingForAnswer,
        MailCheckDecision.NeedToSchedule,
        MailCheckDecision.Others
    ];

    public static string NameFor(MailCheckDecision decision)
    {
        return decision switch
        {
            MailCheckDecision.InterviewScheduled => InterviewScheduled,
            MailCheckDecision.WaitingForAnswer => WaitingForAnswer,
            MailCheckDecision.NeedToSchedule => NeedToSchedule,
            MailCheckDecision.Others => Others,
            _ => string.Empty
        };
    }

    public static string SlugFor(MailCheckDecision decision)
    {
        return decision switch
        {
            MailCheckDecision.InterviewScheduled => "interviewScheduled",
            MailCheckDecision.WaitingForAnswer => "waitingForAnswer",
            MailCheckDecision.NeedToSchedule => "needToSchedule",
            MailCheckDecision.Others => "others",
            MailCheckDecision.Discard => "discard",
            MailCheckDecision.Skip => "skip",
            _ => "others"
        };
    }

    public static MailCheckDecision? KeepFromSlug(string? slug)
    {
        return slug switch
        {
            "interviewScheduled" => MailCheckDecision.InterviewScheduled,
            "waitingForAnswer" => MailCheckDecision.WaitingForAnswer,
            "needToSchedule" => MailCheckDecision.NeedToSchedule,
            "others" => MailCheckDecision.Others,
            _ => null
        };
    }
}
