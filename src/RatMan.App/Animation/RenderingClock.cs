using System.Diagnostics;
using System.Windows.Media;

namespace RatMan.App.Animation;

public sealed class RenderingClock : IDisposable
{
    public static readonly TimeSpan DefaultMaximumFrameTime = TimeSpan.FromMilliseconds(100);

    private readonly Action<TimeSpan> update;
    private readonly TimeSpan maximumFrameTime;
    private long previousTimestamp;
    private bool isRunning;

    public RenderingClock(
        Action<TimeSpan> update,
        TimeSpan? maximumFrameTime = null)
    {
        this.update = update ?? throw new ArgumentNullException(nameof(update));
        this.maximumFrameTime = maximumFrameTime ?? DefaultMaximumFrameTime;
        if (this.maximumFrameTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumFrameTime),
                maximumFrameTime,
                "Maximum frame time must be positive.");
        }
    }

    public bool IsRunning => isRunning;

    public void Start()
    {
        if (isRunning)
        {
            return;
        }

        previousTimestamp = Stopwatch.GetTimestamp();
        CompositionTarget.Rendering += OnRendering;
        isRunning = true;
    }

    public void Stop()
    {
        if (!isRunning)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        isRunning = false;
    }

    public void Dispose()
    {
        Stop();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        long timestamp = Stopwatch.GetTimestamp();
        TimeSpan elapsed = Stopwatch.GetElapsedTime(previousTimestamp, timestamp);
        previousTimestamp = timestamp;

        update(elapsed <= maximumFrameTime ? elapsed : maximumFrameTime);
    }
}
