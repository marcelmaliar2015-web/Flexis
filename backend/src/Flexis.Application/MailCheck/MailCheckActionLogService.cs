using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed class MailCheckActionLogService
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    private readonly IMailCheckActionLogRepository _logs;

    public MailCheckActionLogService(IMailCheckActionLogRepository logs)
    {
        _logs = logs;
    }

    public async Task<MailCheckActionLogPageDto> ListAsync(
        Guid userId,
        MailCheckActionLogQuery query,
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
            NormalizeFilter(query.Source),
            NormalizeFilter(query.Action),
            query.MailboxId,
            query.Query,
            cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new MailCheckActionLogPageDto(
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

    private static MailCheckActionLogDto ToDto(MailCheckActionLog log)
    {
        return new MailCheckActionLogDto(
            log.Id,
            log.RunId,
            log.OccurredAt,
            log.Source,
            log.MailConnectionId,
            log.MailboxEmail,
            log.MailboxProvider,
            log.MessageId,
            log.Subject,
            log.FromAddress,
            log.Action,
            log.Label,
            log.Detail,
            log.DurationMs);
    }
}
