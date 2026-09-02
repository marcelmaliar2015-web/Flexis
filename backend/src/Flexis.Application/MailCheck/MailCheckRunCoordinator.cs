using System.Collections.Concurrent;
using System.Diagnostics;

namespace Flexis.Application.MailCheck;

public sealed class MailCheckRunCoordinator
{
    private sealed class UserSlot
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public CancellationTokenSource? RunCts { get; set; }

        public string? RunKind { get; set; }

        public bool WaitingForLock { get; set; }

        public string? WaitingRequestKind { get; set; }

        public int ProgressGeneration { get; set; }
    }

    private readonly ConcurrentDictionary<Guid, UserSlot> _slots = new();

    public async Task<(MailCheckRunHandle? Handle, int LockMs)> TryAcquireAsync(
        Guid userId,
        bool manual,
        CancellationToken cancellationToken)
    {
        var slot = _slots.GetOrAdd(userId, _ => new UserSlot());
        if (manual)
        {
            slot.RunCts?.Cancel();
        }

        slot.WaitingForLock = true;
        slot.WaitingRequestKind = manual ? "manual" : "auto";
        var waitWatch = Stopwatch.StartNew();
        try
        {
            var timeout = manual ? TimeSpan.FromSeconds(20) : TimeSpan.Zero;
            if (!await slot.Gate.WaitAsync(timeout, cancellationToken))
            {
                return (null, (int)waitWatch.ElapsedMilliseconds);
            }
        }
        finally
        {
            slot.WaitingForLock = false;
            slot.WaitingRequestKind = null;
        }

        var lockMs = (int)waitWatch.ElapsedMilliseconds;
        var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        slot.RunCts = runCts;
        slot.RunKind = manual ? "manual" : "auto";
        slot.ProgressGeneration += 1;
        return (new MailCheckRunHandle(this, userId, runCts, slot.ProgressGeneration), lockMs);
    }

    internal void Release(MailCheckRunHandle handle)
    {
        if (!_slots.TryGetValue(handle.UserId, out var slot))
        {
            handle.RunCts.Dispose();
            return;
        }

        if (!ReferenceEquals(slot.RunCts, handle.RunCts))
        {
            handle.RunCts.Dispose();
            return;
        }

        handle.RunCts.Dispose();
        slot.RunCts = null;
        slot.RunKind = null;
        slot.Gate.Release();
    }

    public MailCheckRunLockStatus GetStatus(Guid userId)
    {
        if (!_slots.TryGetValue(userId, out var slot))
        {
            return new MailCheckRunLockStatus(false, false, null, null, 0);
        }

        return new MailCheckRunLockStatus(
            !string.IsNullOrWhiteSpace(slot.RunKind),
            slot.WaitingForLock,
            slot.RunKind,
            slot.WaitingRequestKind,
            slot.ProgressGeneration);
    }
}

public sealed record MailCheckRunLockStatus(
    bool RunActive,
    bool WaitingForLock,
    string? ActiveRunKind,
    string? WaitingRequestKind,
    int ProgressGeneration);

public sealed class MailCheckRunHandle : IDisposable
{
    private readonly MailCheckRunCoordinator _coordinator;
    private int _disposed;

    internal MailCheckRunHandle(
        MailCheckRunCoordinator coordinator,
        Guid userId,
        CancellationTokenSource runCts,
        int progressGeneration)
    {
        _coordinator = coordinator;
        UserId = userId;
        RunCts = runCts;
        ProgressGeneration = progressGeneration;
    }

    internal Guid UserId { get; }

    internal CancellationTokenSource RunCts { get; }

    public int ProgressGeneration { get; }

    public CancellationToken Token => RunCts.Token;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _coordinator.Release(this);
    }
}
