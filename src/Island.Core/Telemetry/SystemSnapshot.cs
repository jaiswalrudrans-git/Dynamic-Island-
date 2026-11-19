namespace Island.Core.Telemetry;

public sealed record SystemSnapshot
{
    public double CpuPercent { get; init; }
    public IReadOnlyList<double> CorePercents { get; init; } = Array.Empty<double>();
    public double? CpuTemperatureC { get; init; }

    public long RamUsedBytes { get; init; }
    public long RamTotalBytes { get; init; }
    public double RamPercent => RamTotalBytes > 0 ? Math.Clamp((double)RamUsedBytes / RamTotalBytes * 100.0, 0, 100) : 0;

    public string GpuName { get; init; } = "";
    public double? GpuPercent { get; init; }
    public long? VramUsedBytes { get; init; }
    public long? VramTotalBytes { get; init; }
    public double? GpuTemperatureC { get; init; }
    public double? GpuPowerWatts { get; init; }

    public double NetworkDownloadBps { get; init; }
    public double NetworkUploadBps { get; init; }
    public long TodayDownloadedBytes { get; init; }
    public long TodayUploadedBytes { get; init; }

    public IReadOnlyList<DriveMetric> Drives { get; init; } = Array.Empty<DriveMetric>();

    public double? PowerWatts { get; init; }
    public IReadOnlyList<double> PowerHistory { get; init; } = Array.Empty<double>();
    public double? PowerMeanWatts { get; init; }

    public BatteryMetric? Battery { get; init; }

    public IReadOnlyList<ProcessMetric> TopProcesses { get; init; } = Array.Empty<ProcessMetric>();
}

public sealed record DriveMetric(string Name, string Label, long UsedBytes, long TotalBytes, double? TempC)
{
    public double Percent => TotalBytes > 0 ? Math.Clamp((double)UsedBytes / TotalBytes * 100.0, 0, 100) : 0;
}

public sealed record BatteryMetric(
    int Percent,
    bool IsCharging,
    bool IsAcConnected,
    int SecondsRemaining,
    int? HealthPercent,
    string PowerMode
);

public sealed record ProcessMetric(
    int Id,
    string Name,
    double CpuPercent,
    long WorkingSetBytes,
    int ThreadCount,
    int HandleCount
);
