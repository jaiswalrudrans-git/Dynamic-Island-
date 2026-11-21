using Island.Core.Focus;
using Xunit;

namespace Island.Tests;

public sealed class FocusTests : TestFolder
{
    [Fact]
    public void FocusManager_Pomodoro_CyclesStagesCorrectly()
    {
        var now = new DateTime(2026, 11, 19, 10, 0, 0);
        using var manager = new FocusManager(clock: () => now);
        manager.Configure(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), cycles: 4);

        Assert.Equal(FocusMode.Pomodoro, manager.Mode);
        Assert.Equal(PomodoroStage.Work, manager.Stage);
        Assert.Equal(1, manager.CurrentCycle);

        // Skip to break
        manager.Skip();
        Assert.Equal(PomodoroStage.ShortBreak, manager.Stage);
        Assert.Equal(1, manager.CurrentCycle);

        // Skip to next work session
        manager.Skip();
        Assert.Equal(PomodoroStage.Work, manager.Stage);
        Assert.Equal(2, manager.CurrentCycle);
    }

    [Fact]
    public void FocusManager_Timer_PresetsAndDuration()
    {
        using var manager = new FocusManager();
        manager.Mode = FocusMode.Timer;
        Assert.Equal(FocusMode.Timer, manager.Mode);

        manager.SetTimerDuration(TimeSpan.FromMinutes(15));
        Assert.Equal(TimeSpan.FromMinutes(15), manager.TimeRemaining);
        Assert.Equal("15:00", manager.DisplayTime);
    }

    [Fact]
    public void FocusManager_Stopwatch_LapsAndReset()
    {
        using var manager = new FocusManager();
        manager.Mode = FocusMode.Stopwatch;
        Assert.Equal(FocusMode.Stopwatch, manager.Mode);

        manager.AddLap();
        manager.AddLap();
        Assert.Equal(2, manager.Laps.Count);

        manager.Reset();
        Assert.Empty(manager.Laps);
        Assert.Equal("00:00.0", manager.DisplayTime);
    }
}
