namespace Flexis.Domain.MailCheck;

public enum MailCheckDecision
{
    Skip,
    Discard,
    InterviewSchedule,
    AvailabilityRequest,
    AssessmentRequest,
    HrTeamMessage,
    ReplyRequired
}
