using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.App.Services;
using Island.Core.Settings;
using Island.Core.Telemetry;

namespace Island.App.ViewModels;

public sealed record CoreViewModel(int Index, double Percent, double Height, string Tooltip);

public sealed record DriveItemViewModel(string Name, string Label, string UsageText, double Percent, string Tooltip);

public sealed record ProcessItemViewModel(int Id, string Name, string MetricText, string Tooltip);

public sealed class SystemViewModel : ObservableObject, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly SystemHardwareMonitor monitor;
    private readonly Action<string>? log;
    private AppSettings settings;

    private bool detail;
    private bool collapsed = true;
    private bool paused;
    private bool disposed;

    private SystemSnapshot snapshot = new();

    public double CpuPercent => Math.Clamp(snapshot.CpuPercent, 0, 100);
    public string CpuUsageFormatted => $"{Math.Round(CpuPercent):0}%";
    public string CpuTempFormatted => snapshot.CpuTemperatureC.HasValue ? $"{Math.Round(snapshot.CpuTemperatureC.Value):0}°C" : "N/A";
    public string CpuTooltip => $"CPU Usage: {CpuUsageFormatted} | Logical Cores: {snapshot.CorePercents.Count} | Temp: {CpuTempFormatted}";

    public IReadOnlyList<CoreViewModel> CpuCores
    {
        get
        {
            var list = new List<CoreViewModel>();
            for (int i = 0; i < snapshot.CorePercents.Count; i++)
            {
                double pct = Math.Clamp(snapshot.CorePercents[i], 0, 100);
                double h = Math.Max(2, (pct / 100.0) * 26.0);
                list.Add(new CoreViewModel(i, pct, h, $"Core {i}: {Math.Round(pct):0}%"));
            }
            return list;
        }
    }

    public double RamPercent => Math.Clamp(snapshot.RamPercent, 0, 100);
    public string RamPercentFormatted => $"{Math.Round(RamPercent):0}%";
    public string RamText => $"{FormatBytes(snapshot.RamUsedBytes)} / {FormatBytes(snapshot.RamTotalBytes)}";
    public string RamTooltip => $"Memory: {FormatBytes(snapshot.RamUsedBytes)} used of {FormatBytes(snapshot.RamTotalBytes)} ({RamPercentFormatted})";

    public string GpuName => string.IsNullOrWhiteSpace(snapshot.GpuName) ? "GPU" : snapshot.GpuName;
    public double GpuPercent => snapshot.GpuPercent.HasValue ? Math.Clamp(snapshot.GpuPercent.Value, 0, 100) : 0;
    public string GpuUsageFormatted => snapshot.GpuPercent.HasValue ? $"{Math.Round(snapshot.GpuPercent.Value):0}%" : "N/A";
    public double VramPercent => snapshot.VramTotalBytes.HasValue && snapshot.VramTotalBytes > 0 && snapshot.VramUsedBytes.HasValue
        ? Math.Clamp((double)snapshot.VramUsedBytes.Value / snapshot.VramTotalBytes.Value * 100.0, 0, 100) : 0;
    public string VramText => snapshot.VramTotalBytes.HasValue && snapshot.VramTotalBytes > 0 && snapshot.VramUsedBytes.HasValue
        ? $"{FormatBytes(snapshot.VramUsedBytes.Value)} / {FormatBytes(snapshot.VramTotalBytes.Value)}" : "N/A";
    public string GpuTempFormatted => snapshot.GpuTemperatureC.HasValue ? $"{Math.Round(snapshot.GpuTemperatureC.Value):0}°C" : "N/A";
    public string GpuPowerFormatted => snapshot.GpuPowerWatts.HasValue ? $"{snapshot.GpuPowerWatts.Value:0.0} W" : "N/A";
    public string GpuTooltip => $"GPU: {GpuName} | Usage: {GpuUsageFormatted} | VRAM: {VramText} | Temp: {GpuTempFormatted} | Power: {GpuPowerFormatted}";

    public string DownSpeedFormatted => FormatSpeed(snapshot.NetworkDownloadBps);
    public string UpSpeedFormatted => FormatSpeed(snapshot.NetworkUploadBps);
    public string TodayDownFormatted => $"{FormatBytes(snapshot.TodayDownloadedBytes)} ↓";
    public string TodayUpFormatted => $"{FormatBytes(snapshot.TodayUploadedBytes)} ↑";
    public string NetworkTooltip => $"Speed: {DownSpeedFormatted} ↓ | {UpSpeedFormatted} ↑\nToday: {FormatBytes(snapshot.TodayDownloadedBytes)} downloaded | {FormatBytes(snapshot.TodayUploadedBytes)} uploaded";

    public IReadOnlyList<DriveItemViewModel> Drives
    {
        get
        {
            var list = new List<DriveItemViewModel>();
            foreach (var d in snapshot.Drives)
            {
                list.Add(new DriveItemViewModel(
                    d.Name,
                    d.Label,
                    $"{FormatBytes(d.UsedBytes)} / {FormatBytes(d.TotalBytes)}",
                    d.Percent,
                    $"{d.Name} ({d.Label}): {FormatBytes(d.UsedBytes)} / {FormatBytes(d.TotalBytes)} ({Math.Round(d.Percent):0}%)"
                ));
            }
            return list;
        }
    }

    public string PowerWattsFormatted => snapshot.PowerWatts.HasValue ? $"{snapshot.PowerWatts.Value:0.0} W" : "N/A";
    public string PowerMeanFormatted => snapshot.PowerMeanWatts.HasValue ? $"Mean: {snapshot.PowerMeanWatts.Value:0.0} W" : "Mean: N/A";
    public string PowerTooltip => $"Current Power: {PowerWattsFormatted} | 60s Mean: {PowerMeanFormatted}";

    public PointCollection PowerPoints
    {
        get
        {
            var points = new PointCollection();
            var history = snapshot.PowerHistory;
            if (history.Count < 2) return points;

            double max = 10;
            for (int i = 0; i < history.Count; i++) if (history[i] > max) max = history[i];

            double w = 92.0;
            double h = 26.0;
            double dx = w / Math.Max(1, history.Count - 1);

            for (int i = 0; i < history.Count; i++)
            {
                double x = i * dx;
                double y = h - (history[i] / max * (h - 4)) - 2;
                points.Add(new Point(x, Math.Clamp(y, 0, h)));
            }
            return points;
        }
    }

    public bool HasBattery => snapshot.Battery != null;
    public string BatteryPercentFormatted => snapshot.Battery != null ? $"{snapshot.Battery.Percent}%" : "N/A";
    public string BatteryGlyph => snapshot.Battery == null ? "\uE83E" : snapshot.Battery.IsCharging ? "\uEBAE" : "\uE83F";
    public string BatteryStatusText => snapshot.Battery == null ? "Desktop (AC)" : snapshot.Battery.IsCharging ? "Charging" : snapshot.Battery.PowerMode;
    public string BatteryTooltip => snapshot.Battery == null ? "AC Power connected (Desktop)" :
        $"Battery: {snapshot.Battery.Percent}% | {BatteryStatusText} | Plugged in: {snapshot.Battery.IsAcConnected}";

    public bool SortByCpu => settings.SystemProcessSort != "Memory";
    public bool SortByMemory => settings.SystemProcessSort == "Memory";

    public IReadOnlyList<ProcessItemViewModel> TopProcesses
    {
        get
        {
            var list = new List<ProcessItemViewModel>();
            foreach (var p in snapshot.TopProcesses)
            {
                string metric = SortByMemory ? FormatBytes(p.WorkingSetBytes) : $"{p.CpuPercent:0.0}%";
                list.Add(new ProcessItemViewModel(
                    p.Id,
                    p.Name,
                    metric,
                    $"PID: {p.Id}\nName: {p.Name}\nCPU: {p.CpuPercent:0.0}%\nMemory: {FormatBytes(p.WorkingSetBytes)}\nThreads: {p.ThreadCount} | Handles: {p.HandleCount}"
                ));
            }
            return list;
        }
    }

    public bool HasCollapsedStat => settings.CollapsedStat != "None";
    public string CollapsedStatGlyph => settings.CollapsedStat switch
    {
        "Cpu" => "\uE950",
        "Ram" => "\uE7B8",
        "Network" => "\uE839",
        "Battery" => BatteryGlyph,
        "CpuRam" => "\uE950",
        _ => ""
    };

    public string CollapsedStatLabel => settings.CollapsedStat switch
    {
        "Cpu" => $"{CpuUsageFormatted} CPU",
        "Ram" => $"RAM {RamPercentFormatted}",
        "Network" => DownSpeedFormatted,
        "Battery" => BatteryPercentFormatted,
        "CpuRam" => $"C:{CpuUsageFormatted} R:{RamPercentFormatted}",
        _ => ""
    };

    public event Action? CollapsedStatChanged;

    public SystemViewModel(Dispatcher dispatcher, AppSettings config, Action<string>? log = null)
    {
        this.dispatcher = dispatcher;
        this.log = log;
        settings = config;
        monitor = new SystemHardwareMonitor(config, log);
        monitor.SnapshotUpdated += OnSnapshotUpdated;
    }

    public void Configure(AppSettings config)
    {
        settings = config;
        monitor.Configure(config);
        UpdateDemand();
        OnPropertyChanged(string.Empty);
        CollapsedStatChanged?.Invoke();
    }

    public void Display(bool showDetail, bool showCompact, bool suspend)
    {
        if (disposed || (detail, collapsed, paused) == (showDetail, showCompact, suspend)) return;
        detail = showDetail;
        collapsed = showCompact;
        paused = suspend;
        UpdateDemand();
    }

    private void UpdateDemand()
    {
        bool needed = !paused && (detail || (collapsed && HasCollapsedStat));
        monitor.Active = needed;
    }

    private void OnSnapshotUpdated(SystemSnapshot next)
    {
        if (disposed) return;
        dispatcher.InvokeAsync(() =>
        {
            snapshot = next;
            OnPropertyChanged(string.Empty);
            CollapsedStatChanged?.Invoke();
        }, DispatcherPriority.Background);
    }

    public void SetProcessSort(string sortBy, Action<AppSettings> saveSetting)
    {
        string norm = string.Equals(sortBy, "Memory", StringComparison.OrdinalIgnoreCase) ? "Memory" : "Cpu";
        if (settings.SystemProcessSort == norm) return;
        saveSetting(settings with { SystemProcessSort = norm });
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "0 B";
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double val = bytes;
        int idx = 0;
        while (val >= 1024 && idx < units.Length - 1)
        {
            val /= 1024.0;
            idx++;
        }
        return idx == 0 ? $"{val:0} B" : $"{val:0.0} {units[idx]}";
    }

    public static string FormatSpeed(double bps)
    {
        if (bps < 0) return "0 B/s";
        string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
        double val = bps;
        int idx = 0;
        while (val >= 1024 && idx < units.Length - 1)
        {
            val /= 1024.0;
            idx++;
        }
        return idx == 0 ? $"{val:0} B/s" : $"{val:0.0} {units[idx]}";
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        monitor.SnapshotUpdated -= OnSnapshotUpdated;
        monitor.Dispose();
    }
}
