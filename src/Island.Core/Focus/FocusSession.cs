namespace Island.Core.Focus;

public enum FocusMode
{
    Pomodoro,
    Timer,
    Stopwatch
}

public enum PomodoroStage
{
    Work,
    ShortBreak,
    LongBreak
}

public sealed class FocusManager : IDisposable
{
    private readonly Timer timer;
    private readonly object gate = new();
    private readonly List<TimeSpan> laps = new();
    private readonly Func<DateTime> clock;

    private FocusMode mode = FocusMode.Pomodoro;
    private PomodoroStage pomodoroStage = PomodoroStage.Work;
    private bool isRunning;
    private int currentCycle = 1;
    private int maxCycles = 4;

    private TimeSpan pomodoroWorkDuration = TimeSpan.FromMinutes(25);
    private TimeSpan pomodoroShortBreakDuration = TimeSpan.FromMinutes(5);
    private TimeSpan pomodoroLongBreakDuration = TimeSpan.FromMinutes(15);
    private TimeSpan timerDuration = TimeSpan.FromMinutes(10);

    private TimeSpan remaining;
    private TimeSpan elapsed;
    private DateTime lastTick;
    private bool disposed;

    public FocusMode Mode
    {
        get { lock (gate) return mode; }
        set
        {
            lock (gate)
            {
                if (mode == value) return;
                mode = value;
                ResetInternal();
            }
            StateChanged?.Invoke();
        }
    }

    public PomodoroStage Stage
    {
        get { lock (gate) return pomodoroStage; }
    }

    public bool IsRunning
    {
        get { lock (gate) return isRunning; }
    }

    public int CurrentCycle
    {
        get { lock (gate) return currentCycle; }
    }

    public int MaxCycles
    {
        get { lock (gate) return maxCycles; }
    }

    public TimeSpan TimeRemaining
    {
        get { lock (gate) return remaining; }
    }

    public TimeSpan TimeElapsed
    {
        get { lock (gate) return elapsed; }
    }

    public double Progress
    {
        get
        {
            lock (gate)
            {
                if (mode == FocusMode.Stopwatch) return 0;
                double total = (remaining + elapsed).TotalSeconds;
                if (total <= 0) return 0;
                return Math.Clamp(elapsed.TotalSeconds / total, 0, 1);
            }
        }
    }

    public string DisplayTime
    {
        get
        {
            lock (gate)
            {
                if (mode == FocusMode.Stopwatch)
                {
                    return $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 100:0}";
                }
                return $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
            }
        }
    }

    public IReadOnlyList<TimeSpan> Laps
    {
        get { lock (gate) return laps.ToList(); }
    }

    public event Action? StateChanged;
    public event Action<string>? SessionFinished;

    public FocusManager(Func<DateTime>? clock = null)
    {
        this.clock = clock ?? (() => DateTime.Now);
        remaining = pomodoroWorkDuration;
        timer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Configure(TimeSpan work, TimeSpan shortBreak, TimeSpan longBreak, int cycles)
    {
        lock (gate)
        {
            pomodoroWorkDuration = work;
            pomodoroShortBreakDuration = shortBreak;
            pomodoroLongBreakDuration = longBreak;
            maxCycles = Math.Max(1, cycles);
            if (!isRunning && mode == FocusMode.Pomodoro)
            {
                remaining = pomodoroStage switch
                {
                    PomodoroStage.Work => pomodoroWorkDuration,
                    PomodoroStage.ShortBreak => pomodoroShortBreakDuration,
                    PomodoroStage.LongBreak => pomodoroLongBreakDuration,
                    _ => pomodoroWorkDuration
                };
            }
        }
        StateChanged?.Invoke();
    }

    public void SetTimerDuration(TimeSpan duration)
    {
        lock (gate)
        {
            timerDuration = duration;
            if (mode == FocusMode.Timer && !isRunning)
            {
                remaining = duration;
                elapsed = TimeSpan.Zero;
            }
        }
        StateChanged?.Invoke();
    }

    public void ToggleStartPause()
    {
        lock (gate)
        {
            if (isRunning) PauseInternal();
            else StartInternal();
        }
        StateChanged?.Invoke();
    }

    public void Start()
    {
        lock (gate) StartInternal();
        StateChanged?.Invoke();
    }

    public void Pause()
    {
        lock (gate) PauseInternal();
        StateChanged?.Invoke();
    }

    public void Reset()
    {
        lock (gate) ResetInternal();
        StateChanged?.Invoke();
    }

    public void Skip()
    {
        string? finishMessage = null;
        lock (gate)
        {
            if (mode == FocusMode.Pomodoro)
            {
                AdvancePomodoro(out finishMessage);
            }
            else
            {
                ResetInternal();
            }
        }
        StateChanged?.Invoke();
        if (finishMessage != null) SessionFinished?.Invoke(finishMessage);
    }

    public void AddLap()
    {
        lock (gate)
        {
            if (mode == FocusMode.Stopwatch)
            {
                laps.Insert(0, elapsed);
            }
        }
        StateChanged?.Invoke();
    }

    private void StartInternal()
    {
        if (isRunning) return;
        isRunning = true;
        lastTick = clock();
        timer.Change(0, 100);
    }

    private void PauseInternal()
    {
        if (!isRunning) return;
        isRunning = false;
        timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void ResetInternal()
    {
        isRunning = false;
        timer.Change(Timeout.Infinite, Timeout.Infinite);
        elapsed = TimeSpan.Zero;
        laps.Clear();

        switch (mode)
        {
            case FocusMode.Pomodoro:
                pomodoroStage = PomodoroStage.Work;
                currentCycle = 1;
                remaining = pomodoroWorkDuration;
                break;
            case FocusMode.Timer:
                remaining = timerDuration;
                break;
            case FocusMode.Stopwatch:
                remaining = TimeSpan.Zero;
                break;
        }
    }

    private void OnTimerTick(object? state)
    {
        string? finishMsg = null;
        lock (gate)
        {
            if (disposed || !isRunning) return;

            var now = clock();
            var dt = now - lastTick;
            lastTick = now;

            elapsed += dt;

            if (mode == FocusMode.Stopwatch)
            {
                // Stopwatch just increments elapsed
            }
            else
            {
                remaining -= dt;
                if (remaining <= TimeSpan.Zero)
                {
                    remaining = TimeSpan.Zero;
                    isRunning = false;
                    timer.Change(Timeout.Infinite, Timeout.Infinite);

                    if (mode == FocusMode.Pomodoro)
                    {
                        AdvancePomodoro(out finishMsg);
                    }
                    else
                    {
                        finishMsg = "Timer Finished!";
                    }
                }
            }
        }

        StateChanged?.Invoke();
        if (finishMsg != null) SessionFinished?.Invoke(finishMsg);
    }

    private void AdvancePomodoro(out string? finishMsg)
    {
        elapsed = TimeSpan.Zero;
        if (pomodoroStage == PomodoroStage.Work)
        {
            if (currentCycle >= maxCycles)
            {
                pomodoroStage = PomodoroStage.LongBreak;
                remaining = pomodoroLongBreakDuration;
                currentCycle = 1;
                finishMsg = "Focus cycle complete! Enjoy a long break.";
            }
            else
            {
                pomodoroStage = PomodoroStage.ShortBreak;
                remaining = pomodoroShortBreakDuration;
                finishMsg = "Work session complete! Take a short break.";
            }
        }
        else
        {
            if (pomodoroStage == PomodoroStage.ShortBreak) currentCycle++;
            pomodoroStage = PomodoroStage.Work;
            remaining = pomodoroWorkDuration;
            finishMsg = "Break finished! Ready to focus?";
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            timer.Dispose();
        }
    }
}
