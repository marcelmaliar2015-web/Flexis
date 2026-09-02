namespace Flexis.Application.MailCheck;

public static class MailCheckAutoCheck
{
    public const int IntervalSeconds = 20;

    public const int RunBatchSize = 3;

    public const int MaxAlreadyHandledSkipsPerRun = 32;
}
