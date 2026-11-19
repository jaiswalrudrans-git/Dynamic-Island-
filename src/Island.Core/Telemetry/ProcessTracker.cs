namespace Island.Core.Telemetry;

public sealed class ProcessTracker
{
    private readonly Dictionary<int, (TimeSpan processorTime, DateTime timestamp)> previousTimes = new();

    public IReadOnlyList<ProcessMetric> Update(
        IEnumerable<ProcessSample> currentSamples,
        DateTime now,
        int logicalCores,
        string sortBy = "Cpu",
        int topCount = 5)
    {
        logicalCores = Math.Max(1, logicalCores);
        var currentPids = new HashSet<int>();
        var metrics = new List<ProcessMetric>();

        foreach (var sample in currentSamples)
        {
            currentPids.Add(sample.Id);
            double cpuPercent = 0;
            if (previousTimes.TryGetValue(sample.Id, out var prev))
            {
                double dt = (now - prev.timestamp).TotalSeconds;
                if (dt > 0)
                {
                    double cpuDeltaSec = (sample.TotalProcessorTime - prev.processorTime).TotalSeconds;
                    if (cpuDeltaSec > 0)
                    {
                        cpuPercent = Math.Clamp((cpuDeltaSec / (dt * logicalCores)) * 100.0, 0, 100);
                    }
                }
            }

            previousTimes[sample.Id] = (sample.TotalProcessorTime, now);

            metrics.Add(new ProcessMetric(
                sample.Id,
                sample.Name,
                Math.Round(cpuPercent, 1),
                sample.WorkingSetBytes,
                sample.ThreadCount,
                sample.HandleCount
            ));
        }

        if (previousTimes.Count > currentPids.Count * 2)
        {
            var deadPids = previousTimes.Keys.Where(pid => !currentPids.Contains(pid)).ToList();
            foreach (var pid in deadPids) previousTimes.Remove(pid);
        }

        IEnumerable<ProcessMetric> sorted = string.Equals(sortBy, "Memory", StringComparison.OrdinalIgnoreCase)
            ? metrics.OrderByDescending(p => p.WorkingSetBytes).ThenByDescending(p => p.CpuPercent)
            : metrics.OrderByDescending(p => p.CpuPercent).ThenByDescending(p => p.WorkingSetBytes);

        return sorted.Take(topCount).ToList();
    }

    public void Reset() => previousTimes.Clear();
}

public readonly record struct ProcessSample(
    int Id,
    string Name,
    TimeSpan TotalProcessorTime,
    long WorkingSetBytes,
    int ThreadCount,
    int HandleCount
);
