using Flexis.Domain.MailCheck;

namespace Flexis.Application.MailCheck;

public sealed record MailCheckUsageHourDto(
    string CapturedOn,
    string CapturedHour,
    string UpdatedAt,
    int CallCount,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCostUsd,
    string LastModel);

public sealed record MailCheckUsageTotalsDto(
    int CallCount,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCostUsd);

public sealed record MailCheckUsageDto(
    MailCheckUsageTotalsDto Lifetime,
    MailCheckUsageTotalsDto Today,
    IReadOnlyList<MailCheckUsageHourDto> History);

public interface IMailCheckUsageHourRepository
{
    Task<MailCheckUsageHour?> GetByUserAndHourAsync(
        Guid userId,
        DateTimeOffset capturedHour,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MailCheckUsageHour>> ListRecentAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken);

    Task AddAsync(MailCheckUsageHour hour, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
