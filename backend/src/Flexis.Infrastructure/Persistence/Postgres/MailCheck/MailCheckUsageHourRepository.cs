using Flexis.Application.MailCheck;
using Flexis.Domain.MailCheck;
using Flexis.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Flexis.Infrastructure.Persistence.Postgres.MailCheck;

internal sealed class MailCheckUsageHourRepository : IMailCheckUsageHourRepository
{
    private readonly FlexisDbContext _db;

    public MailCheckUsageHourRepository(FlexisDbContext db)
    {
        _db = db;
    }

    public Task<MailCheckUsageHour?> GetByUserAndHourAsync(
        Guid userId,
        DateTimeOffset capturedHour,
        CancellationToken cancellationToken)
    {
        var hour = MailCheckUsageHour.TruncateToHour(capturedHour);
        return _db.MailCheckUsageHours.FirstOrDefaultAsync(
            item => item.UserId == userId && item.CapturedHour == hour,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MailCheckUsageHour>> ListRecentAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken)
    {
        return await _db.MailCheckUsageHours
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CapturedHour)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MailCheckUsageHour hour, CancellationToken cancellationToken)
    {
        await _db.MailCheckUsageHours.AddAsync(hour, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
