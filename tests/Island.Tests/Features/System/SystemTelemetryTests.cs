using Island.App.Features.SystemInfo.Services;
using Island.App.Features.SystemInfo.ViewModels;
using Island.Core.Settings;
using Island.Core.Telemetry;
using Xunit;

namespace Island.Tests;

public sealed class SystemTelemetryTests
{
    [Fact]
    public void PowerHistoryBufferComputesMeanAndRollsAtCapacity()
    {
        var buffer = new PowerHistoryBuffer(5);
        Assert.Null(buffer.Mean);
        Assert.Empty(buffer.GetSamples());

        buffer.Add(10.0);
        buffer.Add(20.0);
        Assert.Equal(15.0, buffer.Mean);
        Assert.Equal(new[] { 10.0, 20.0 }, buffer.GetSamples());

        buffer.Add(30.0);
        buffer.Add(40.0);
        buffer.Add(50.0);
        Assert.Equal(30.0, buffer.Mean);
        Assert.Equal(new[] { 10.0, 20.0, 30.0, 40.0, 50.0 }, buffer.GetSamples());

        // Rollover: 6th element replaces oldest (10.0)
        buffer.Add(60.0);
        Assert.Equal(40.0, buffer.Mean);
        Assert.Equal(new[] { 20.0, 30.0, 40.0, 50.0, 60.0 }, buffer.GetSamples());

        // Invalid values are rejected
        buffer.Add(double.NaN);
        buffer.Add(-5.0);
        buffer.Add(double.PositiveInfinity);
        Assert.Equal(40.0, buffer.Mean);

        buffer.Clear();
        Assert.Null(buffer.Mean);
        Assert.Empty(buffer.GetSamples());
    }

    [Fact]
    public void NetworkAccumulatorComputesRatesAndHandlesRollover()
    {
        var fakeTime = new DateTime(2026, 11, 19, 10, 0, 0);
        var accumulator = new NetworkAccumulator(() => fakeTime);

        // Baseline tick
        var (down, up, todayDown, todayUp) = accumulator.Update(1000, 500);
        Assert.Equal(0, down);
        Assert.Equal(0, up);
        Assert.Equal(0, todayDown);
        Assert.Equal(0, todayUp);

        // 1 second later: 2000 bytes down, 1000 bytes up
        fakeTime = fakeTime.AddSeconds(1);
        (down, up, todayDown, todayUp) = accumulator.Update(3000, 1500);
        Assert.Equal(2000, down);
        Assert.Equal(1000, up);
        Assert.Equal(2000, todayDown);
        Assert.Equal(1000, todayUp);

        // 2 seconds later: 4000 bytes down, 2000 bytes up -> 2000 Bps down, 1000 Bps up
        fakeTime = fakeTime.AddSeconds(2);
        (down, up, todayDown, todayUp) = accumulator.Update(7000, 3500);
        Assert.Equal(2000, down);
        Assert.Equal(1000, up);
        Assert.Equal(6000, todayDown);
        Assert.Equal(3000, todayUp);

        // Midnight rollover resets today's totals
        fakeTime = new DateTime(2026, 11, 20, 0, 0, 1);
        (down, up, todayDown, todayUp) = accumulator.Update(8000, 4000);
        Assert.Equal(1000, todayDown);
        Assert.Equal(500, todayUp);
    }

    [Fact]
    public void ProcessTrackerCalculatesCpuDeltaAndSorts()
    {
        var tracker = new ProcessTracker();
        var time = new DateTime(2026, 11, 19, 10, 0, 0);
        int cores = 4;

        var sample1 = new[]
        {
            new ProcessSample(101, "procA", TimeSpan.FromSeconds(10), 200 * 1024 * 1024, 4, 100),
            new ProcessSample(102, "procB", TimeSpan.FromSeconds(5), 500 * 1024 * 1024, 8, 200)
        };

        // First sample creates baseline
        var initial = tracker.Update(sample1, time, cores, "Cpu");
        Assert.Equal(2, initial.Count);
        Assert.All(initial, p => Assert.Equal(0, p.CpuPercent));

        // 1 second later:
        // procA consumed 2 seconds of CPU time across 4 cores -> (2 / (1 * 4)) * 100 = 50.0%
        // procB consumed 0.4 seconds of CPU time across 4 cores -> (0.4 / (1 * 4)) * 100 = 10.0%
        time = time.AddSeconds(1);
        var sample2 = new[]
        {
            new ProcessSample(101, "procA", TimeSpan.FromSeconds(12), 210 * 1024 * 1024, 4, 100),
            new ProcessSample(102, "procB", TimeSpan.FromSeconds(5.4), 520 * 1024 * 1024, 8, 200)
        };

        var byCpu = tracker.Update(sample2, time, cores, "Cpu");
        Assert.Equal("procA", byCpu[0].Name);
        Assert.Equal(50.0, byCpu[0].CpuPercent);
        Assert.Equal("procB", byCpu[1].Name);
        Assert.Equal(10.0, byCpu[1].CpuPercent);

        // Sort by memory: procB (520 MB) comes before procA (210 MB)
        var byMemory = tracker.Update(sample2, time, cores, "Memory");
        Assert.Equal("procB", byMemory[0].Name);
        Assert.Equal("procA", byMemory[1].Name);
    }

    [Fact]
    public void SystemViewModelFormattersFormatAppropriately()
    {
        Assert.Equal("0 B", SystemViewModel.FormatBytes(0));
        Assert.Equal("1.5 KB", SystemViewModel.FormatBytes(1536));
        Assert.Equal("10.0 MB", SystemViewModel.FormatBytes(10 * 1024 * 1024));
        Assert.Equal("3.2 GB", SystemViewModel.FormatBytes((long)(3.2 * 1024 * 1024 * 1024)));

        Assert.Equal("0 B/s", SystemViewModel.FormatSpeed(0));
        Assert.Equal("512 B/s", SystemViewModel.FormatSpeed(512));
        Assert.Equal("2.4 MB/s", SystemViewModel.FormatSpeed(2.4 * 1024 * 1024));
        Assert.Equal("1.0 GB/s", SystemViewModel.FormatSpeed(1024.0 * 1024 * 1024));
    }

    [Fact]
    public void AppSettingsNormalizesSystemAndCollapsedStatSettings()
    {
        var settings = new AppSettings
        {
            CollapsedStat = "InvalidStat",
            SystemProcessSort = "InvalidSort",
            SystemGpuIndex = -5
        };

        var normalized = settings.Normalize();
        Assert.Equal("None", normalized.CollapsedStat);
        Assert.Equal("Cpu", normalized.SystemProcessSort);
        Assert.Equal(0, normalized.SystemGpuIndex);

        var valid = new AppSettings
        {
            CollapsedStat = "Battery",
            SystemProcessSort = "Memory",
            SystemGpuIndex = 1
        };

        var normValid = valid.Normalize();
        Assert.Equal("Battery", normValid.CollapsedStat);
        Assert.Equal("Memory", normValid.SystemProcessSort);
        Assert.Equal(1, normValid.SystemGpuIndex);
    }

    [Fact]
    public void DynamicNvmlFailsGracefullyWhenInvalidOrMissing()
    {
        using var nvml = new DynamicNvml();
        // Regardless of whether host has NVIDIA or not, calling TryGetMetrics must never throw
        bool success = nvml.TryGetMetrics(999, out var metrics);
        if (!nvml.IsAvailable)
        {
            Assert.False(success);
            Assert.Null(metrics);
        }
    }

    [Fact]
    public async Task SystemHardwareMonitorCollectsRealHardwareSnapshot()
    {
        var settings = new AppSettings();
        using var monitor = new SystemHardwareMonitor(settings);
        var tcs = new TaskCompletionSource<SystemSnapshot>();

        monitor.SnapshotUpdated += snapshot =>
        {
            tcs.TrySetResult(snapshot);
        };

        monitor.Active = true;
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(3500));
        Assert.Same(tcs.Task, completed);

        var snap = await tcs.Task;
        Assert.NotNull(snap);
        Assert.InRange(snap.CpuPercent, 0, 100);
        Assert.NotEmpty(snap.CorePercents);
        Assert.True(snap.RamTotalBytes > 0);
        Assert.True(snap.RamUsedBytes > 0);
        Assert.InRange(snap.RamPercent, 0, 100);
        Assert.NotEmpty(snap.Drives);
        Assert.All(snap.Drives, d => Assert.True(d.TotalBytes > 0));
        Assert.NotEmpty(snap.TopProcesses);
    }
}
