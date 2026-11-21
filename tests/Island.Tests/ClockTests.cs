using Island.App.Header.ViewModels;
using Island.App.ViewModels;
using Island.Core.Settings;
using Xunit;

namespace Island.Tests;

public sealed class ClockTests
{
    [Theory]
    [InlineData(0, false, false, "12:59", 250)]
    [InlineData(13, false, true, "01:59:59", 250)]
    [InlineData(23, true, false, "23:59", 250)]
    [InlineData(0, true, true, "00:59:59", 250)]
    public void FormatsAndSchedulesAcrossHourBoundary(int hour, bool use24, bool seconds, string text, int delayMs)
    {
        var clock = new ClockViewModel();
        var now = new DateTime(2026, 1, 1, hour, 59, 59, 750);
        var delay = clock.Refresh(now, new AppSettings { Use24HourClock = use24, ShowSeconds = seconds });
        Assert.Equal(text, clock.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(delayMs), delay);
    }

    [Theory]
    [InlineData(false, 60000)]
    [InlineData(true, 1000)]
    public void WakesOnlyWhenDisplayedTimeCanChange(bool seconds, int delayMs)
    {
        var clock = new ClockViewModel();
        var delay = clock.Refresh(DateTime.Today, new AppSettings { ShowSeconds = seconds });
        Assert.Equal(TimeSpan.FromMilliseconds(delayMs), delay);
    }

    [Fact]
    public void BlinkingColonTicksEverySecond()
    {
        var clock = new ClockViewModel();
        var delay = clock.Refresh(DateTime.Today, new AppSettings { ShowSeconds = false, ClockBlinkingColon = true });
        Assert.Equal(TimeSpan.FromMilliseconds(1000), delay);
    }
}
