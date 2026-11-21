using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Island.App.Core;
using Island.Core.Settings;
using Island.Core.Telemetry;

namespace Island.App.Features.SystemInfo.Services;

public sealed class SystemHardwareMonitor : IDisposable
{
    private readonly Action<string>? log;
    private readonly DynamicNvml nvml;
    private readonly PdhGpuReader pdhGpu;
    private readonly NetworkAccumulator networkAccumulator = new();
    private readonly PowerHistoryBuffer powerBuffer = new(60);
    private readonly ProcessTracker processTracker = new();
    private readonly Timer sampleTimer;

    private readonly object gate = new();
    private bool active;
    private bool sampling;
    private bool disposed;

    private long lastIdleTime = -1;
    private long lastKernelTime = -1;
    private long lastUserTime = -1;

    private SystemMetricsNative.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[]? lastCoreInfo;
    private int coreCount;

    private AppSettings settings;
    private SystemSnapshot currentSnapshot = new();

    public SystemSnapshot CurrentSnapshot => currentSnapshot;
    public event Action<SystemSnapshot>? SnapshotUpdated;

    public bool Active
    {
        get => active;
        set
        {
            lock (gate)
            {
                if (active == value || disposed) return;
                active = value;
                if (active)
                {
                    sampleTimer.Change(0, 1000);
                }
                else
                {
                    sampleTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }
    }

    public SystemHardwareMonitor(AppSettings settings, Action<string>? log = null)
    {
        this.settings = settings;
        this.log = log;
        nvml = new DynamicNvml(log);
        pdhGpu = new PdhGpuReader(log);
        coreCount = Math.Max(1, Environment.ProcessorCount);
        sampleTimer = new Timer(OnSampleTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Configure(AppSettings newSettings)
    {
        lock (gate)
        {
            settings = newSettings;
        }
    }

    private void OnSampleTick(object? state)
    {
        if (disposed || !active) return;
        lock (gate)
        {
            if (sampling) return;
            sampling = true;
        }

        try
        {
            var snapshot = CollectSnapshot();
            currentSnapshot = snapshot;
            SnapshotUpdated?.Invoke(snapshot);
        }
        catch (Exception ex)
        {
            log?.Invoke($"Hardware monitoring error: {ex.Message}");
        }
        finally
        {
            lock (gate)
            {
                sampling = false;
            }
        }
    }

    private SystemSnapshot CollectSnapshot()
    {
        // 1. Total CPU
        double cpuPercent = 0;
        if (SystemMetricsNative.GetSystemTimes(out long idle, out long kernel, out long user))
        {
            if (lastIdleTime >= 0)
            {
                long usr = user - lastUserTime;
                long ker = kernel - lastKernelTime;
                long idl = idle - lastIdleTime;
                long sys = ker + usr;
                if (sys > 0)
                {
                    cpuPercent = Math.Clamp(Math.Round((sys - idl) * 100.0 / sys, 1), 0, 100);
                }
            }
            lastIdleTime = idle;
            lastKernelTime = kernel;
            lastUserTime = user;
        }

        // 2. Per-core CPU
        var corePercents = new List<double>();
        int itemSize = Marshal.SizeOf<SystemMetricsNative.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>();
        int totalSize = itemSize * coreCount;
        nint coreBuffer = Marshal.AllocHGlobal(totalSize);
        try
        {
            int status = SystemMetricsNative.NtQuerySystemInformation(8, coreBuffer, totalSize, out int returned);
            if (status == 0 && returned >= totalSize)
            {
                var currentCores = new SystemMetricsNative.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[coreCount];
                for (int i = 0; i < coreCount; i++)
                {
                    nint ptr = coreBuffer + (i * itemSize);
                    currentCores[i] = Marshal.PtrToStructure<SystemMetricsNative.SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION>(ptr);
                }

                if (lastCoreInfo != null && lastCoreInfo.Length == coreCount)
                {
                    for (int i = 0; i < coreCount; i++)
                    {
                        long k = currentCores[i].KernelTime - lastCoreInfo[i].KernelTime;
                        long u = currentCores[i].UserTime - lastCoreInfo[i].UserTime;
                        long idl = currentCores[i].IdleTime - lastCoreInfo[i].IdleTime;
                        long sys = k + u;
                        double corePct = sys > 0 ? Math.Clamp(Math.Round((sys - idl) * 100.0 / sys, 1), 0, 100) : 0;
                        corePercents.Add(corePct);
                    }
                }
                else
                {
                    for (int i = 0; i < coreCount; i++) corePercents.Add(0);
                }
                lastCoreInfo = currentCores;
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"Per-core CPU read error: {ex.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(coreBuffer);
        }

        // 3. RAM
        long ramUsed = 0, ramTotal = 0;
        var memStatus = new SystemMetricsNative.MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<SystemMetricsNative.MEMORYSTATUSEX>() };
        if (SystemMetricsNative.GlobalMemoryStatusEx(ref memStatus))
        {
            ramTotal = (long)memStatus.ullTotalPhys;
            ramUsed = (long)(memStatus.ullTotalPhys - memStatus.ullAvailPhys);
        }

        // 4. GPU
        string gpuName = "GPU";
        double? gpuPercent = null;
        long? vramUsed = null, vramTotal = null;
        double? gpuTemp = null, gpuPower = null;

        if (nvml.IsAvailable && nvml.TryGetMetrics(settings.SystemGpuIndex, out var nvMetrics) && nvMetrics != null)
        {
            gpuName = nvMetrics.Name;
            gpuPercent = nvMetrics.GpuPercent;
            vramUsed = nvMetrics.VramUsedBytes;
            vramTotal = nvMetrics.VramTotalBytes;
            gpuTemp = nvMetrics.TemperatureC;
            gpuPower = nvMetrics.PowerWatts;
        }
        else if (pdhGpu.IsAvailable)
        {
            gpuPercent = pdhGpu.GetUtilization();
            gpuName = "GPU (System)";
        }

        // 5. Network
        long netRx = 0, netTx = 0;
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                var stats = ni.GetIPStatistics();
                netRx += stats.BytesReceived;
                netTx += stats.BytesSent;
            }
        }
        catch { }

        var (downBps, upBps, todayDown, todayUp) = networkAccumulator.Update(netRx, netTx);

        // 6. Storage (Internal drives only)
        var driveMetrics = new List<DriveMetric>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                string label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
                long total = drive.TotalSize;
                long used = total - drive.TotalFreeSpace;
                driveMetrics.Add(new DriveMetric(drive.Name, label, used, total, null));
            }
        }
        catch { }

        // 7. Battery & Power
        BatteryMetric? battery = null;
        double? powerWatts = gpuPower;

        if (SystemMetricsNative.GetSystemPowerStatus(out var powerStatus))
        {
            bool hasBattery = (powerStatus.BatteryFlag & 128) == 0;
            bool isAc = powerStatus.ACLineStatus == 1;
            bool isCharging = (powerStatus.BatteryFlag & 8) != 0;
            int pct = Math.Clamp((int)powerStatus.BatteryLifePercent, 0, 100);
            int secRemaining = powerStatus.BatteryLifeTime;

            if (hasBattery)
            {
                battery = new BatteryMetric(
                    pct,
                    isCharging,
                    isAc,
                    secRemaining,
                    null,
                    isAc ? "Plugged in" : "On battery"
                );
            }
        }

        if (powerWatts.HasValue)
        {
            powerBuffer.Add(powerWatts.Value);
        }

        // 8. Top Processes
        var processSamples = new List<ProcessSample>();
        try
        {
            var procs = Process.GetProcesses();
            foreach (var p in procs)
            {
                try
                {
                    processSamples.Add(new ProcessSample(
                        p.Id,
                        p.ProcessName,
                        p.TotalProcessorTime,
                        p.WorkingSet64,
                        p.Threads.Count,
                        p.HandleCount
                    ));
                }
                catch
                {
                    // Access denied or exited process
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch { }

        var topProcs = processTracker.Update(
            processSamples,
            DateTime.Now,
            coreCount,
            settings.SystemProcessSort,
            5
        );

        return new SystemSnapshot
        {
            CpuPercent = cpuPercent,
            CorePercents = corePercents,
            CpuTemperatureC = null,
            RamUsedBytes = ramUsed,
            RamTotalBytes = ramTotal,
            GpuName = gpuName,
            GpuPercent = gpuPercent,
            VramUsedBytes = vramUsed,
            VramTotalBytes = vramTotal,
            GpuTemperatureC = gpuTemp,
            GpuPowerWatts = gpuPower,
            NetworkDownloadBps = downBps,
            NetworkUploadBps = upBps,
            TodayDownloadedBytes = todayDown,
            TodayUploadedBytes = todayUp,
            Drives = driveMetrics,
            PowerWatts = powerWatts,
            PowerHistory = powerBuffer.GetSamples(),
            PowerMeanWatts = powerBuffer.Mean,
            Battery = battery,
            TopProcesses = topProcs
        };
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            active = false;
        }

        sampleTimer.Dispose();
        nvml.Dispose();
        pdhGpu.Dispose();
    }
}
