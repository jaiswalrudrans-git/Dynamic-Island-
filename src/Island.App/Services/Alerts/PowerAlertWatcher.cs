using System.Windows.Threading;
using Island.App.Interop;
using Island.Core.Alerts;
using Island.Core.Settings;

namespace Island.App.Services.Alerts;

public sealed class PowerAlertWatcher : IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly AlertQueue alertQueue;
    private readonly Action<string>? log;
    private readonly object gate = new();

    private bool initialized;
    private bool? lastAcOnline;
    private bool lowBatteryFired;
    private bool criticalBatteryFired;

    private AppSettings settings;
    private bool disposed;

    public PowerAlertWatcher(Dispatcher dispatcher, AlertQueue alertQueue, AppSettings settings, Action<string>? log = null)
    {
        this.dispatcher = dispatcher;
        this.alertQueue = alertQueue;
        this.settings = settings;
        this.log = log;

        CheckPowerStatus();
    }

    public void Configure(AppSettings newSettings)
    {
        lock (gate)
        {
            settings = newSettings;
        }
    }

    public void CheckPowerStatus()
    {
        lock (gate)
        {
            if (disposed) return;
        }

        if (!SystemMetricsNative.GetSystemPowerStatus(out var status)) return;

        bool hasBattery = (status.BatteryFlag & 128) == 0;
        bool isAc = status.ACLineStatus == 1;
        int pct = Math.Clamp((int)status.BatteryLifePercent, 0, 100);

        lock (gate)
        {
            if (disposed || !settings.AlertsEnabled) return;

            if (!initialized)
            {
                lastAcOnline = isAc;
                initialized = true;
                return;
            }

            // 1. Charger plugged in / unplugged
            if (settings.AlertChargerEnabled && lastAcOnline.HasValue && lastAcOnline.Value != isAc)
            {
                lastAcOnline = isAc;
                string title = isAc ? "Charging" : "On Battery";
                string glyph = isAc ? "\uEBAE" : "\uE83F";

                dispatcher.InvokeAsync(() =>
                {
                    alertQueue.Enqueue(new IslandAlert(
                        Id: "alert-charger",
                        Type: AlertType.Charger,
                        Title: title,
                        Glyph: glyph,
                        Level: AlertLevel.Info,
                        Duration: TimeSpan.FromSeconds(settings.AlertDurationSeconds)
                    ));
                });
            }

            if (!hasBattery) return;

            // 2. Low / Critical Battery (only alert when discharging on battery)
            if (settings.AlertBatteryEnabled && !isAc)
            {
                if (pct <= settings.AlertBatteryCriticalThreshold)
                {
                    if (!criticalBatteryFired)
                    {
                        criticalBatteryFired = true;
                        lowBatteryFired = true;
                        dispatcher.InvokeAsync(() =>
                        {
                            alertQueue.Enqueue(new IslandAlert(
                                Id: "alert-battery-critical",
                                Type: AlertType.BatteryCritical,
                                Title: $"Battery {pct}% - Connect Charger",
                                Glyph: "\uE83F",
                                Level: AlertLevel.Critical,
                                Duration: TimeSpan.FromSeconds(settings.AlertDurationSeconds + 2),
                                PlaySound: settings.AlertSoundEnabled
                            ));
                        });
                    }
                }
                else if (pct <= settings.AlertBatteryLowThreshold)
                {
                    if (!lowBatteryFired)
                    {
                        lowBatteryFired = true;
                        dispatcher.InvokeAsync(() =>
                        {
                            alertQueue.Enqueue(new IslandAlert(
                                Id: "alert-battery-low",
                                Type: AlertType.BatteryLow,
                                Title: $"Battery {pct}%",
                                Glyph: "\uE83F",
                                Level: AlertLevel.Warning,
                                Duration: TimeSpan.FromSeconds(settings.AlertDurationSeconds)
                            ));
                        });
                    }
                }
            }

            // Hysteresis reset
            if (isAc || pct > settings.AlertBatteryLowThreshold + 5)
            {
                lowBatteryFired = false;
                criticalBatteryFired = false;
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
        }
    }
}
