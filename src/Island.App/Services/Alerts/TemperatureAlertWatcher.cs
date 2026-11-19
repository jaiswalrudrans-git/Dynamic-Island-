using System.Windows.Threading;
using Island.Core.Alerts;
using Island.Core.Settings;
using Island.Core.Telemetry;

namespace Island.App.Services.Alerts;

public sealed class TemperatureAlertWatcher : IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly AlertQueue alertQueue;
    private readonly Action<string>? log;
    private readonly object gate = new();

    private bool highTempFired;
    private AppSettings settings;
    private bool disposed;

    public TemperatureAlertWatcher(Dispatcher dispatcher, AlertQueue alertQueue, AppSettings settings, Action<string>? log = null)
    {
        this.dispatcher = dispatcher;
        this.alertQueue = alertQueue;
        this.settings = settings;
        this.log = log;
    }

    public void Configure(AppSettings newSettings)
    {
        lock (gate)
        {
            settings = newSettings;
        }
    }

    public void OnSnapshot(SystemSnapshot snapshot)
    {
        lock (gate)
        {
            if (disposed || !settings.AlertsEnabled || !settings.AlertTemperatureEnabled) return;

            double maxTemp = 0;
            string source = "GPU";

            if (snapshot.GpuTemperatureC.HasValue && snapshot.GpuTemperatureC.Value > maxTemp)
            {
                maxTemp = snapshot.GpuTemperatureC.Value;
                source = "GPU";
            }

            if (snapshot.CpuTemperatureC.HasValue && snapshot.CpuTemperatureC.Value > maxTemp)
            {
                maxTemp = snapshot.CpuTemperatureC.Value;
                source = "CPU";
            }

            if (maxTemp >= settings.AlertTemperatureThreshold)
            {
                if (!highTempFired)
                {
                    highTempFired = true;
                    int t = (int)Math.Round(maxTemp);
                    dispatcher.InvokeAsync(() =>
                    {
                        alertQueue.Enqueue(new IslandAlert(
                            Id: "alert-temperature",
                            Type: AlertType.Temperature,
                            Title: $"{source} Temp {t}°C",
                            Glyph: "\uE7BA",
                            Level: AlertLevel.Warning,
                            Duration: TimeSpan.FromSeconds(settings.AlertDurationSeconds),
                            PlaySound: settings.AlertSoundEnabled
                        ));
                    });
                }
            }
            else if (maxTemp < settings.AlertTemperatureThreshold - 5)
            {
                highTempFired = false;
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
