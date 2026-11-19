using System.Globalization;
using System.IO;
using Island.Core.Services;
using Xunit;

namespace Island.Tests;

public sealed class RollingLogTests : TestFolder
{
    [Fact]
    public void LogRollsByUtcDateAndKeepsSevenDays()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Folder, "logs")).FullName;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        string LogFor(int daysAgo) => Path.Combine(directory,
            "app-" + today.AddDays(-daysAgo).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
        File.WriteAllText(LogFor(7), "old");
        File.WriteAllText(LogFor(6), "retained");
        string other = Path.Combine(directory, "unrelated.log");
        File.WriteAllText(other, "unrelated");

        new RollingLog(Folder).Write("started");

        Assert.False(File.Exists(LogFor(7)));
        Assert.True(File.Exists(LogFor(6)));
        Assert.True(File.Exists(other));
        Assert.Contains("started", File.ReadAllText(LogFor(0)));
    }
}
