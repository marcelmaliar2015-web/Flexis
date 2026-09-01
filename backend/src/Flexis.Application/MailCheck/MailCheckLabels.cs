using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public static class MailCheckLabels
{
    public const string InterviewSchedule = "Interview Schedule";
    public const string AvailabilityRequest = "Availability Request";
    public const string AssessmentRequest = "Assessment Request";
    public const string HrTeamMessage = "HR Team Message";
    public const string ReplyRequired = "Reply required";

    public static readonly IReadOnlyList<MailCheckDecision> KeepDecisions =
    [
        MailCheckDecision.InterviewSchedule,
        MailCheckDecision.AvailabilityRequest,
        MailCheckDecision.AssessmentRequest,
        MailCheckDecision.HrTeamMessage,
        MailCheckDecision.ReplyRequired
    ];

    public static string NameFor(MailCheckDecision decision)
    {
        return decision switch
        {
            MailCheckDecision.InterviewSchedule => InterviewSchedule,
            MailCheckDecision.AvailabilityRequest => AvailabilityRequest,
            MailCheckDecision.AssessmentRequest => AssessmentRequest,
            MailCheckDecision.HrTeamMessage => HrTeamMessage,
            MailCheckDecision.ReplyRequired => ReplyRequired,
            _ => string.Empty
        };
    }

    public static string SlugFor(MailCheckDecision decision)
    {
        return decision switch
        {
            MailCheckDecision.InterviewSchedule => "interviewSchedule",
            MailCheckDecision.AvailabilityRequest => "availabilityRequest",
            MailCheckDecision.AssessmentRequest => "assessmentRequest",
            MailCheckDecision.HrTeamMessage => "hrTeamMessage",
            MailCheckDecision.ReplyRequired => "replyRequired",
            MailCheckDecision.Discard => "discard",
            MailCheckDecision.Skip => "skip",
            _ => "replyRequired"
        };
    }

    public static MailCheckDecision? KeepFromSlug(string? slug)
    {
        return slug switch
        {
            "interviewSchedule" => MailCheckDecision.InterviewSchedule,
            "availabilityRequest" => MailCheckDecision.AvailabilityRequest,
            "assessmentRequest" => MailCheckDecision.AssessmentRequest,
            "hrTeamMessage" => MailCheckDecision.HrTeamMessage,
            "replyRequired" => MailCheckDecision.ReplyRequired,
            _ => null
        };
    }
}
