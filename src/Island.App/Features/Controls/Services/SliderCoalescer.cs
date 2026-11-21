using System.Diagnostics;

namespace Island.App.Features.Controls.Services;

public sealed class SliderCoalescer : IDisposable
{
    private readonly Action<double> writeAction;
    private readonly int intervalMs;
    private readonly double echoTolerance;
    private readonly object sync = new();
    private readonly Thread workerThread;

    private double? pendingValue;
    private bool pendingIsFinal;
    private double lastWrittenValue = double.NaN;
    private bool hasWrittenAny;
    private bool isDragging;
    private DateTime lastDragEndTimeUtc = DateTime.MinValue;
    private readonly Stopwatch timer = Stopwatch.StartNew();
    private long lastWriteTimestampMs;
    private volatile bool disposed;

    public int TotalWritesCount { get; private set; }

    public double LastWrittenValue
    {
        get
        {
            lock (sync) return lastWrittenValue;
        }
    }

    public bool IsDragging
    {
        get
        {
            lock (sync) return isDragging;
        }
    }

    public SliderCoalescer(Action<double> writeAction, int intervalMs = 33, double echoTolerance = 0.01)
    {
        this.writeAction = writeAction ?? throw new ArgumentNullException(nameof(writeAction));
        this.intervalMs = intervalMs;
        this.echoTolerance = echoTolerance;

        workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "SliderCoalescerWorker"
        };
        workerThread.Start();
    }

    public void NotifyDragStarted()
    {
        lock (sync)
        {
            isDragging = true;
        }
    }

    public void NotifyDragCompleted()
    {
        lock (sync)
        {
            isDragging = false;
            lastDragEndTimeUtc = DateTime.UtcNow;
        }
    }

    public void RequestValue(double value, bool isFinal = false)
    {
        lock (sync)
        {
            pendingValue = value;
            if (isFinal)
            {
                pendingIsFinal = true;
                isDragging = false;
                lastDragEndTimeUtc = DateTime.UtcNow;
            }
            Monitor.Pulse(sync);
        }
    }

    public bool ShouldIgnoreIncomingNotification(double incomingValue)
    {
        lock (sync)
        {
            // 1. While the user is dragging (pointer captured)
            if (isDragging) return true;

            // 2. For 400 ms after release
            if ((DateTime.UtcNow - lastDragEndTimeUtc).TotalMilliseconds < 400)
                return true;

            // 3. Echo suppression: within 1 point of our own last write
            if (hasWrittenAny && Math.Abs(incomingValue - lastWrittenValue) <= echoTolerance)
                return true;

            return false;
        }
    }

    private void WorkerLoop()
    {
        while (!disposed)
        {
            double targetValue;
            bool isFinal;

            lock (sync)
            {
                while (!disposed && !pendingValue.HasValue)
                {
                    Monitor.Wait(sync);
                }

                if (disposed) break;

                long now = timer.ElapsedMilliseconds;
                long elapsed = now - lastWriteTimestampMs;

                // Rate limit intermediate writes to intervalMs (~33ms)
                if (!pendingIsFinal && elapsed < intervalMs)
                {
                    int waitTime = (int)(intervalMs - elapsed);
                    if (waitTime > 0)
                    {
                        Monitor.Wait(sync, waitTime);
                        continue;
                    }
                }

                targetValue = pendingValue!.Value;
                isFinal = pendingIsFinal;
                pendingValue = null;
                pendingIsFinal = false;
            }

            // Skip intermediate write if value hasn't changed
            bool skip = false;
            lock (sync)
            {
                if (hasWrittenAny && !isFinal && Math.Abs(targetValue - lastWrittenValue) < 0.0001)
                {
                    skip = true;
                }
            }

            if (!skip)
            {
                try
                {
                    writeAction(targetValue);
                }
                catch
                {
                    // Failures ignored/logged per requirements
                }

                lock (sync)
                {
                    lastWrittenValue = targetValue;
                    hasWrittenAny = true;
                    lastWriteTimestampMs = timer.ElapsedMilliseconds;
                    TotalWritesCount++;
                }
            }
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
            Monitor.PulseAll(sync);
        }
        workerThread.Join(500);
    }
}
