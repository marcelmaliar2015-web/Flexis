using System.Diagnostics;

namespace Flexis.Application.MailCheck;

internal sealed class MailCheckRunTiming
{
    public int LockMs { get; private set; }

    public int TokenMs { get; private set; }

    public int LabelsMs { get; private set; }

    public int ScanMs { get; private set; }

    public int FetchMs { get; private set; }

    public int ClassifyMs { get; private set; }

    public int ApplyMs { get; private set; }

    public int TotalMs => LockMs + TokenMs + LabelsMs + ScanMs + FetchMs + ClassifyMs + ApplyMs;

    public void AddLockMs(int lockMs)
    {
        LockMs += lockMs;
    }

    public MailCheckRunTimingDto ToDto()
    {
        return new MailCheckRunTimingDto(TotalMs, LockMs, TokenMs, LabelsMs, ScanMs, FetchMs, ClassifyMs, ApplyMs);
    }

    public async Task<T> TrackTokenAsync<T>(Func<Task<T>> action)
    {
        var (elapsed, result) = await MeasureAsync(action);
        TokenMs += elapsed;
        return result;
    }

    public async Task<T> TrackLabelsAsync<T>(Func<Task<T>> action)
    {
        var (elapsed, result) = await MeasureAsync(action);
        LabelsMs += elapsed;
        return result;
    }

    public async Task<T> TrackScanAsync<T>(Func<Task<T>> action)
    {
        var (elapsed, result) = await MeasureAsync(action);
        ScanMs += elapsed;
        return result;
    }

    public async Task<T> TrackFetchAsync<T>(Func<Task<T>> action)
    {
        var (elapsed, result) = await MeasureAsync(action);
        FetchMs += elapsed;
        return result;
    }

    public async Task<T> TrackClassifyAsync<T>(Func<Task<T>> action)
    {
        var (elapsed, result) = await MeasureAsync(action);
        ClassifyMs += elapsed;
        return result;
    }

    public async Task TrackApplyAsync(Func<Task> action)
    {
        ApplyMs += await MeasureAsync(action);
    }

    public async Task<T> TrackApplyAsync<T>(Func<Task<T>> action)
    {
        var (elapsed, result) = await MeasureAsync(action);
        ApplyMs += elapsed;
        return result;
    }

    private static async Task<int> MeasureAsync(Func<Task> action)
    {
        var (elapsed, _) = await MeasureAsync(async () =>
        {
            await action();
            return true;
        });
        return elapsed;
    }

    private static async Task<(int ElapsedMs, T Result)> MeasureAsync<T>(Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await action();
        stopwatch.Stop();
        return ((int)stopwatch.ElapsedMilliseconds, result);
    }
}
