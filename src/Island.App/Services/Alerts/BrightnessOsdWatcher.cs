using System.Management;
using System.Windows.Threading;
using Island.Core.Alerts;
using Island.Core.Settings;

namespace Island.App.Services.Alerts;

public sealed class BrightnessOsdWatcher : IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly AlertQueue alertQueue;
    private readonly Action<string>? log;
    private readonly Timer checkTimer;
    private readonly object gate = new();

    private int lastBrightness = -1;
    private bool initialized;
    private AppSettings settings;
    private bool disposed;

    public BrightnessOsdWatcher(Dispatcher dispatcher, AlertQueue alertQueue, AppSettings settings, Action<string>? log = null)
    {
        this.dispatcher = dispatcher;
        this.alertQueue = alertQueue;
        this.settings = settings;
        this.log = log;

        checkTimer = new Timer(OnCheckTick, null, 1000, 1500);
    }

    public void Configure(AppSettings newSettings)
    {
        lock (gate)
        {
            settings = newSettings;
        }
    }

    private void OnCheckTick(object? state)
    {
        lock (gate)
        {
            if (disposed || !settings.AlertsEnabled || !settings.AlertBrightnessOsdEnabled) return;
        }

        int? current = QueryBrightness();
        if (!current.HasValue) return;

        lock (gate)
        {
            if (disposed) return;
            if (!initialized)
            {
                lastBrightness = current.Value;
                initialized = true;
                return;
            }

            if (current.Value == lastBrightness) return;
            lastBrightness = current.Value;

            int pct = current.Value;
            dispatcher.InvokeAsync(() =>
            {
                alertQueue.Enqueue(new IslandAlert(
                    Id: "osd-brightness",
                    Type: AlertType.Brightness,
                    Title: $"Brightness {pct}%",
                    Glyph: "\uE706",
                    Level: AlertLevel.Info,
                    Progress: pct / 100.0,
                    Duration: TimeSpan.FromSeconds(Math.Min(settings.AlertDurationSeconds, 3))
                ));
            });
        }
    }

    private int? QueryBrightness()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(@"\\.\root\wmi"),
                new SelectQuery("WmiMonitorBrightness"));

            using var results = searcher.Get();
            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    if (obj["CurrentBrightness"] is byte b) return b;
                    if (obj["CurrentBrightness"] is int i) return i;
                }
            }
        }
        catch { }
        return null;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            checkTimer.Dispose();
        }
    }
}
