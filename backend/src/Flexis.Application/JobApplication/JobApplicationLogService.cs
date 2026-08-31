using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobApplicationLogService
{
    private readonly IJobApplicationLogRepository _logs;

    public JobApplicationLogService(IJobApplicationLogRepository logs)
    {
        _logs = logs;
    }

    public async Task<IReadOnlyList<JobApplicationLogDto>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var items = await _logs.ListAsync(userId, 200, cancellationToken);
        return items.Select(ToDto).ToArray();
    }

    private static JobApplicationLogDto ToDto(JobApplicationLog log)
    {
        return new JobApplicationLogDto(
            log.Id,
            log.OccurredAt,
            log.Category,
            log.Action,
            log.Summary,
            log.Detail);
    }
}
