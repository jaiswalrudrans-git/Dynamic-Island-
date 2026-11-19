using System.Globalization;
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

public sealed record ProcessItemViewModel(string Name, string MetricText, string Tooltip);

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
    public bool HasCpuTemp => snapshot.CpuTemperatureC.HasValue && snapshot.CpuTemperatureC.Value > 0;
    public string CpuTempFormatted => HasCpuTemp ? $"{Math.Round(snapshot.CpuTemperatureC!.Value):0}°C" : "";
    public string CpuTooltip => $"CPU Usage: {CpuUsageFormatted} | Logical Cores: {snapshot.CorePercents.Count}" +
        (HasCpuTemp ? $" | Temp: {CpuTempFormatted}" : "");

    public IReadOnlyList<CoreViewModel> CpuCores
    {
        get
        {
            var list = new List<CoreViewModel>();
            for (int i = 0; i < snapshot.CorePercents.Count; i++)
            {
                double pct = Math.Clamp(snapshot.CorePercents[i], 0, 100);
                double h = Math.Max(2, (pct / 100.0) * 22.0);
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
    public string GpuShortName
    {
        get
        {
            var name = GpuName;
            if (name.Contains("NVIDIA GeForce ", StringComparison.OrdinalIgnoreCase))
                name = name.Replace("NVIDIA GeForce ", "", StringComparison.OrdinalIgnoreCase);
            if (name.Contains(" GPU", StringComparison.OrdinalIgnoreCase))
                name = name.Replace(" GPU", "", StringComparison.OrdinalIgnoreCase);
            if (name.Contains("Intel(R) ", StringComparison.OrdinalIgnoreCase))
                name = name.Replace("Intel(R) ", "", StringComparison.OrdinalIgnoreCase);
            return name.Trim();
        }
    }

    public double GpuPercent => snapshot.GpuPercent.HasValue ? Math.Clamp(snapshot.GpuPercent.Value, 0, 100) : 0;
    public string GpuUsageFormatted => snapshot.GpuPercent.HasValue ? $"{Math.Round(snapshot.GpuPercent.Value):0}%" : "";
    public bool HasGpu => snapshot.GpuPercent.HasValue;

    public bool HasVram => snapshot.VramTotalBytes.HasValue && snapshot.VramTotalBytes > 0 && snapshot.VramUsedBytes.HasValue;
    public double VramPercent => HasVram
        ? Math.Clamp((double)snapshot.VramUsedBytes!.Value / snapshot.VramTotalBytes!.Value * 100.0, 0, 100) : 0;
    public string VramText => HasVram
        ? $"{FormatBytes(snapshot.VramUsedBytes!.Value)} / {FormatBytes(snapshot.VramTotalBytes!.Value)}" : "";

    public bool HasGpuTemp => snapshot.GpuTemperatureC.HasValue && snapshot.GpuTemperatureC.Value > 0;
    public string GpuTempFormatted => HasGpuTemp ? $"{Math.Round(snapshot.GpuTemperatureC!.Value):0}°C" : "";

    public bool HasGpuPower => snapshot.GpuPowerWatts.HasValue && snapshot.GpuPowerWatts.Value > 0;
    public string GpuPowerFormatted => HasGpuPower ? $"{snapshot.GpuPowerWatts!.Value:0.0} W" : "";

    public string GpuTooltip => $"GPU: {GpuName}" +
        (HasGpu ? $" | Usage: {GpuUsageFormatted}" : "") +
        (HasVram ? $" | VRAM: {VramText}" : "") +
        (HasGpuTemp ? $" | Temp: {GpuTempFormatted}" : "") +
        (HasGpuPower ? $" | Power: {GpuPowerFormatted}" : "");

    public string DownSpeedFormatted => FormatSpeed(snapshot.NetworkDownloadBps);
    public string UpSpeedFormatted => FormatSpeed(snapshot.NetworkUploadBps);
    public string TodayDownFormatted => FormatBytes(snapshot.TodayDownloadedBytes);
    public string TodayUpFormatted => FormatBytes(snapshot.TodayUploadedBytes);
    public string TodayTotalsFormatted => $"Today: {TodayDownFormatted} down · {TodayUpFormatted} up";
    public string NetworkTooltip => $"Speed: ↓ {DownSpeedFormatted} · ↑ {UpSpeedFormatted}\nToday: {TodayDownFormatted} downloaded · {TodayUpFormatted} uploaded";

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

    public bool HasPower => snapshot.PowerWatts.HasValue && snapshot.PowerWatts.Value > 0;
    public string PowerWattsFormatted => HasPower ? $"{snapshot.PowerWatts!.Value:0.0} W" : "-- W";
    public bool HasPowerMean => snapshot.PowerMeanWatts.HasValue && snapshot.PowerMeanWatts.Value > 0;
    public string PowerMeanFormatted => HasPowerMean ? $"Mean {snapshot.PowerMeanWatts!.Value:0.0} W" : "";
    public string PowerTooltip => $"Power Draw: {PowerWattsFormatted}" + (HasPowerMean ? $" | 60s Mean: {PowerMeanFormatted}" : "");

    public PointCollection PowerPoints
    {
        get
        {
            var points = new PointCollection();
            var history = snapshot.PowerHistory;
            if (history.Count < 2) return points;

            double max = 10;
            for (int i = 0; i < history.Count; i++) if (history[i] > max) max = history[i];

            double w = 90.0;
            double h = 24.0;
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
    public string BatteryPercentFormatted => snapshot.Battery != null ? $"{snapshot.Battery.Percent}%" : "100%";
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
            if (snapshot.TopProcesses.Count == 0) return list;

            // Group processes by friendly application name to eliminate clutter like LenovoVantage-1, LenovoVantage-2
            var grouped = snapshot.TopProcesses
                .GroupBy(p => FriendlyProcessName(p.Name))
                .Select(g => new
                {
                    Name = g.Key,
                    Cpu = g.Sum(x => x.CpuPercent),
                    Mem = g.Sum(x => x.WorkingSetBytes),
                    Count = g.Count(),
                    Pids = string.Join(", ", g.Select(x => x.Id))
                });

            var sorted = SortByMemory
                ? grouped.OrderByDescending(g => g.Mem).Take(5)
                : grouped.OrderByDescending(g => g.Cpu).Take(5);

            foreach (var p in sorted)
            {
                string metric = SortByMemory ? FormatBytes(p.Mem) : $"{p.Cpu:0.0}%";
                string countSuffix = p.Count > 1 ? $" ({p.Count})" : "";
                string displayName = p.Name + countSuffix;
                string tooltip = $"{p.Name}\nInstances: {p.Count}\nPIDs: {p.Pids}\nCPU: {p.Cpu:0.0}%\nMemory: {FormatBytes(p.Mem)}";

                list.Add(new ProcessItemViewModel(displayName, metric, tooltip));
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
    public event Action<SystemSnapshot>? SnapshotUpdated;

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
        SnapshotUpdated?.Invoke(next);
        dispatcher.InvokeAsync(() =>
        {
            snapshot = next;
            OnPropertyChanged(string.Empty);
            CollapsedStatChanged?.Invoke();
        }, DispatcherPriority.Background);
    }

    public void SetProcessSort(string sortBy, Action<AppSettings> saveSetting)
    {
        if (settings.SystemProcessSort == sortBy) return;
        saveSetting(settings with { SystemProcessSort = sortBy });
    }

    private static string FriendlyProcessName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Process";
        string name = raw;
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];

        // Strip GUIDs, hex IDs, or dash hashes like LenovoVantage-1234
        int dashIdx = name.IndexOf('-');
        if (dashIdx > 2 && dashIdx < name.Length - 1)
        {
            string prefix = name[..dashIdx];
            if (prefix.Equals("LenovoVantage", StringComparison.OrdinalIgnoreCase)) return "Lenovo Vantage";
            if (prefix.Equals("MicrosoftEdge", StringComparison.OrdinalIgnoreCase)) return "Microsoft Edge";
        }

        return name switch
        {
            "LenovoVantage" => "Lenovo Vantage",
            "chrome" => "Google Chrome",
            "msedge" => "Microsoft Edge",
            "brave" => "Brave Browser",
            "firefox" => "Firefox",
            "Code" => "Visual Studio Code",
            "devenv" => "Visual Studio",
            "explorer" => "Windows Explorer",
            "Spotify" => "Spotify",
            "Discord" => "Discord",
            "Slack" => "Slack",
            "Steam" or "steamwebhelper" => "Steam",
            "Island.App" => "Island",
            _ => name
        };
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:0.0} KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:0.0} MB";
        double gb = mb / 1024.0;
        return $"{gb:0.0} GB";
    }

    public static string FormatSpeed(double bps)
    {
        if (bps >= 1_073_741_824.0) return $"{bps / 1_073_741_824.0:0.0} GB/s";
        if (bps >= 1_048_576.0) return $"{bps / 1_048_576.0:0.0} MB/s";
        if (bps >= 1024.0) return $"{bps / 1024.0:0.0} KB/s";
        return $"{Math.Round(bps):0} B/s";
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        monitor.Dispose();
    }
}
