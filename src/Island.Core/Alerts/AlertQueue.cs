namespace Island.Core.Alerts;

public sealed class AlertQueue : IDisposable
{
    private readonly LinkedList<IslandAlert> pending = new();
    private readonly object gate = new();
    private readonly Timer dismissTimer;
    private readonly TimeSpan defaultDuration;
    private IslandAlert? activeAlert;
    private bool disposed;

    public IslandAlert? ActiveAlert
    {
        get { lock (gate) return activeAlert; }
    }

    public int PendingCount
    {
        get { lock (gate) return pending.Count; }
    }

    public event Action<IslandAlert?>? ActiveAlertChanged;
    public event Action<IslandAlert>? AlertEnqueued;

    public AlertQueue(TimeSpan? defaultDuration = null)
    {
        this.defaultDuration = defaultDuration ?? TimeSpan.FromSeconds(4);
        dismissTimer = new Timer(OnDismissTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Enqueue(IslandAlert alert)
    {
        lock (gate)
        {
            if (disposed) return;

            // In-place update for high-frequency continuous OSD alerts
            if (activeAlert != null && activeAlert.Type == alert.Type &&
                alert.Type is AlertType.Volume or AlertType.Brightness or AlertType.Mic)
            {
                activeAlert = alert;
                ResetTimer(alert.Duration ?? defaultDuration);
                ActiveAlertChanged?.Invoke(activeAlert);
                return;
            }

            if (alert.Level == AlertLevel.Critical)
            {
                if (activeAlert == null || activeAlert.Level != AlertLevel.Critical)
                {
                    if (activeAlert != null)
                    {
                        pending.AddFirst(activeAlert);
                    }
                    activeAlert = alert;
                    ResetTimer(alert.Duration ?? defaultDuration);
                    ActiveAlertChanged?.Invoke(activeAlert);
                    AlertEnqueued?.Invoke(alert);
                    return;
                }
                pending.AddFirst(alert);
            }
            else
            {
                pending.AddLast(alert);
            }

            AlertEnqueued?.Invoke(alert);

            if (activeAlert == null)
            {
                PromoteNext();
            }
        }
    }

    public void DismissActive()
    {
        lock (gate)
        {
            if (disposed) return;
            PromoteNext();
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            if (disposed) return;
            pending.Clear();
            activeAlert = null;
            dismissTimer.Change(Timeout.Infinite, Timeout.Infinite);
            ActiveAlertChanged?.Invoke(null);
        }
    }

    private void PromoteNext()
    {
        dismissTimer.Change(Timeout.Infinite, Timeout.Infinite);
        if (pending.Count > 0)
        {
            activeAlert = pending.First!.Value;
            pending.RemoveFirst();
            ResetTimer(activeAlert.Duration ?? defaultDuration);
        }
        else
        {
            activeAlert = null;
        }

        ActiveAlertChanged?.Invoke(activeAlert);
    }

    private void ResetTimer(TimeSpan duration)
    {
        int ms = (int)Math.Clamp(duration.TotalMilliseconds, 500, 30000);
        dismissTimer.Change(ms, Timeout.Infinite);
    }

    private void OnDismissTick(object? state)
    {
        lock (gate)
        {
            if (disposed) return;
            PromoteNext();
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            dismissTimer.Dispose();
            pending.Clear();
            activeAlert = null;
        }
    }
}
