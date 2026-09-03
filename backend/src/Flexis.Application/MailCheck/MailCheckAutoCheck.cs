namespace Flexis.Application.MailCheck;

public static class MailCheckAutoCheck
{
    public const int IntervalSeconds = 20;

    public const int RunBatchSize = 3;

    public const int MaxAlreadyHandledSkipsPerRun = 32;

    public const int MaxFollowUpRuns = 5;

    public const int StartupDelaySeconds = 8;
}
