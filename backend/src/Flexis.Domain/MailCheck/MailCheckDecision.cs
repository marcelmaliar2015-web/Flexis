namespace Flexis.Domain.MailCheck;

public enum MailCheckDecision
{
    Skip,
    Discard,
    InterviewScheduled,
    WaitingForAnswer,
    NeedToSchedule,
    Others
}
