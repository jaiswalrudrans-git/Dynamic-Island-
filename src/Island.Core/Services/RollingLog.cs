using System.Globalization;

namespace Island.Core.Services;

public sealed class RollingLog(string dataDirectory)
{
    private readonly string directory = Path.Combine(dataDirectory, "logs");
    private readonly object gate = new();
    private DateOnly? lastPrunedDay;

    public void Write(string message)
    {
        lock (gate)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var day = DateOnly.FromDateTime(now.UtcDateTime);
                Directory.CreateDirectory(directory);
                string name = "app-" + day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log";
                File.AppendAllText(Path.Combine(directory, name), $"{now:O} {message}{Environment.NewLine}");
                if (lastPrunedDay == day) return;

                foreach (string path in Directory.EnumerateFiles(directory, "app-????-??-??.log"))
                {
                    if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(path).AsSpan(4),
                        "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                        && date < day.AddDays(-6))
                        File.Delete(path);
                }
                lastPrunedDay = day;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Trace.WriteLine($"Log unavailable: {ex.Message}");
            }
        }
    }
}
