namespace Island.Core.Telemetry;

public sealed class NetworkAccumulator
{
    private readonly Func<DateTime> clock;
    private DateTime today;
    private long lastReceived = -1;
    private long lastSent = -1;
    private DateTime lastTimestamp;
    private long todayDownloaded;
    private long todayUploaded;
    private readonly object gate = new();

    public long TodayDownloaded => todayDownloaded;
    public long TodayUploaded => todayUploaded;

    public NetworkAccumulator(Func<DateTime>? clock = null)
    {
        this.clock = clock ?? (() => DateTime.Now);
        today = this.clock().Date;
    }

    public (double downloadBps, double uploadBps, long todayDown, long todayUp) Update(long currentReceived, long currentSent)
    {
        lock (gate)
        {
            var now = clock();
            if (now.Date != today)
            {
                today = now.Date;
                todayDownloaded = 0;
                todayUploaded = 0;
            }

            if (lastReceived < 0 || lastSent < 0)
            {
                lastReceived = currentReceived;
                lastSent = currentSent;
                lastTimestamp = now;
                return (0, 0, todayDownloaded, todayUploaded);
            }

            double dt = (now - lastTimestamp).TotalSeconds;
            if (dt <= 0) dt = 1.0;

            long deltaDown = currentReceived >= lastReceived ? currentReceived - lastReceived : 0;
            long deltaUp = currentSent >= lastSent ? currentSent - lastSent : 0;

            lastReceived = currentReceived;
            lastSent = currentSent;
            lastTimestamp = now;

            todayDownloaded += deltaDown;
            todayUploaded += deltaUp;

            double downBps = deltaDown / dt;
            double upBps = deltaUp / dt;

            return (downBps, upBps, todayDownloaded, todayUploaded);
        }
    }

    public void Reset()
    {
        lock (gate)
        {
            lastReceived = -1;
            lastSent = -1;
            todayDownloaded = 0;
            todayUploaded = 0;
            today = clock().Date;
        }
    }
}
