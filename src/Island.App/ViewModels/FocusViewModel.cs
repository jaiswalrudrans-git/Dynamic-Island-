using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.Core.Focus;

namespace Island.App.ViewModels;

public sealed class FocusViewModel : ObservableObject, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly FocusManager manager;
    private bool disposed;

    public FocusMode Mode => manager.Mode;
    public bool IsPomodoro => manager.Mode == FocusMode.Pomodoro;
    public bool IsTimer => manager.Mode == FocusMode.Timer;
    public bool IsStopwatch => manager.Mode == FocusMode.Stopwatch;

    public string DisplayTime => manager.DisplayTime;
    public double Progress => manager.Progress;
    public bool ShowProgress => manager.Mode != FocusMode.Stopwatch;
    public bool IsRunning => manager.IsRunning;
    public string PlayPauseGlyph => manager.IsRunning ? "\uE769" : "\uE768";
    public string PlayPauseText => manager.IsRunning ? "Pause" : "Start";

    public string StageText => manager.Mode switch
    {
        FocusMode.Pomodoro => manager.Stage switch
        {
            PomodoroStage.Work => "Work Focus",
            PomodoroStage.ShortBreak => "Short Break",
            PomodoroStage.LongBreak => "Long Break",
            _ => "Focus"
        },
        FocusMode.Timer => "Countdown Timer",
        FocusMode.Stopwatch => "Stopwatch",
        _ => "Focus"
    };

    public string CycleText => manager.Mode == FocusMode.Pomodoro
        ? $"Cycle {manager.CurrentCycle} of {manager.MaxCycles}" : "";

    public string StageShortName => manager.Stage switch
    {
        PomodoroStage.Work => "Work",
        PomodoroStage.ShortBreak => "Short Break",
        PomodoroStage.LongBreak => "Long Break",
        _ => "Focus"
    };

    public string CycleSubtitle => manager.Mode == FocusMode.Pomodoro
        ? $"{StageShortName} · {manager.CurrentCycle} of {manager.MaxCycles}"
        : StageText;

    public int CurrentCycle => manager.CurrentCycle;
    public int MaxCycles => manager.MaxCycles;

    public IReadOnlyList<string> LapStrings => manager.Laps
        .Select((l, i) => $"Lap {manager.Laps.Count - i}: {(int)l.TotalMinutes:00}:{l.Seconds:00}.{l.Milliseconds / 100:0}")
        .ToList();

    public bool HasLaps => manager.Laps.Count > 0;

    public FocusViewModel(Dispatcher dispatcher, FocusManager manager)
    {
        this.dispatcher = dispatcher;
        this.manager = manager;
        manager.StateChanged += OnStateChanged;
    }

    private void OnStateChanged()
    {
        if (disposed) return;
        dispatcher.InvokeAsync(() => OnPropertyChanged(string.Empty));
    }

    public void SetMode(FocusMode mode) => manager.Mode = mode;

    public void ToggleStartPause() => manager.ToggleStartPause();

    public void Reset() => manager.Reset();

    public void Skip() => manager.Skip();

    public void SetTimerMinutes(int minutes) => manager.SetTimerDuration(TimeSpan.FromMinutes(minutes));

    public void AddLap() => manager.AddLap();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        manager.StateChanged -= OnStateChanged;
    }
}
