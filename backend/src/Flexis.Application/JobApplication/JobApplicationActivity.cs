using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobApplicationActivity
{
    private readonly IJobApplicationLogRepository _logs;

    public JobApplicationActivity(IJobApplicationLogRepository logs)
    {
        _logs = logs;
    }

    public async Task WriteAsync(
        Guid userId,
        string category,
        string action,
        string summary,
        string detail,
        CancellationToken cancellationToken)
    {
        var log = JobApplicationLog.Create(
            userId,
            category,
            action,
            Trim(summary, 240),
            Trim(detail, 2000));
        await _logs.AddAsync(log, cancellationToken);
        await _logs.SaveChangesAsync(cancellationToken);
    }

    private static string Trim(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max].TrimEnd();
    }
}
