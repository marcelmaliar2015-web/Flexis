using Flexis.Domain.JobApplication;

namespace Flexis.Application.JobApplication;

public sealed class JobApplicationLogService
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    private readonly IJobApplicationLogRepository _logs;

    public JobApplicationLogService(IJobApplicationLogRepository logs)
    {
        _logs = logs;
    }

    public async Task<JobApplicationLogPageDto> ListAsync(
        Guid userId,
        JobApplicationLogQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1
            ? DefaultPageSize
            : Math.Min(query.PageSize, MaxPageSize);
        var (items, totalCount) = await _logs.ListAsync(
            userId,
            page,
            pageSize,
            NormalizeFilter(query.Category),
            query.Query,
            cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new JobApplicationLogPageDto(
            items.Select(ToDto).ToArray(),
            page,
            pageSize,
            totalCount,
            totalPages);
    }

    private static string? NormalizeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant();
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
