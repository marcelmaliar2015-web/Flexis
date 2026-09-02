using System.Collections.Concurrent;

namespace Flexis.Application.MailCheck;

public sealed class MailCheckRunProgressStore
{
    private sealed record Snapshot(
        int Generation,
        string Stage,
        string Message,
        string? MailboxEmail,
        int Processed,
        int Scanned,
        int AlreadySeen,
        int ScanPage,
        DateTimeOffset StartedAt);

    private readonly ConcurrentDictionary<Guid, Snapshot> _snapshots = new();

    public void Begin(Guid userId, int generation, string message)
    {
        _snapshots[userId] = new Snapshot(
            generation,
            "lock",
            message,
            null,
            0,
            0,
            0,
            0,
            DateTimeOffset.UtcNow);
    }

    public void Report(
        Guid userId,
        int generation,
        string stage,
        string message,
        string? mailboxEmail = null,
        int processed = 0,
        int scanned = 0,
        int alreadySeen = 0,
        int scanPage = 0)
    {
        _snapshots.AddOrUpdate(
            userId,
            _ => new Snapshot(generation, stage, message, mailboxEmail, processed, scanned, alreadySeen, scanPage, DateTimeOffset.UtcNow),
            (_, existing) =>
            {
                if (existing.Generation > generation)
                {
                    return existing;
                }

                return existing with
                {
                    Generation = generation,
                    Stage = stage,
                    Message = message,
                    MailboxEmail = mailboxEmail ?? existing.MailboxEmail,
                    Processed = processed,
                    Scanned = scanned,
                    AlreadySeen = alreadySeen,
                    ScanPage = scanPage,
                    StartedAt = existing.Generation == generation ? existing.StartedAt : DateTimeOffset.UtcNow,
                };
            });
    }

    public MailCheckRunProgressDto Get(Guid userId)
    {
        if (!_snapshots.TryGetValue(userId, out var snapshot))
        {
            return new MailCheckRunProgressDto(false, "idle", string.Empty, null, 0, 0, 0, 0, 0, false, null, null);
        }

        var elapsedMs = (long)(DateTimeOffset.UtcNow - snapshot.StartedAt).TotalMilliseconds;
        return new MailCheckRunProgressDto(
            true,
            snapshot.Stage,
            snapshot.Message,
            snapshot.MailboxEmail,
            snapshot.Processed,
            snapshot.Scanned,
            snapshot.AlreadySeen,
            snapshot.ScanPage,
            elapsedMs,
            false,
            null,
            null);
    }

    public void End(Guid userId, int generation)
    {
        _snapshots.TryGetValue(userId, out var snapshot);
        if (snapshot is null || snapshot.Generation != generation)
        {
            return;
        }

        _snapshots.TryRemove(userId, out _);
    }
}
