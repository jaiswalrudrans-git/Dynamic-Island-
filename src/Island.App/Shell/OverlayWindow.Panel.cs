using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Island.App.Interop;
using Island.App.Services.Alerts;
using Island.App.Services.Controls;
using Island.App.ViewModels;
using Island.App.Views;
using Island.Core;
using Island.Core.Alerts;
using Island.Core.Calendar;
using Island.Core.Clipboard;
using Island.Core.Focus;
using Island.Core.Motion;
using Island.Core.Screenshots;
using Island.Core.Settings;
using Island.Core.Tasks;
using Island.Core.Weather;

namespace Island.App.Shell;

public partial class OverlayWindow
{
    private const int EscapeId = 0x491;
    private const int ClipboardHotkeyId = 0x492;
    private readonly PanelMotion motion = new();
    private readonly NotchClip notch = new();
    private readonly DispatcherTimer intent = new();
    private PanelNavigation navigation = null!;
    private MotionSettingsWindow? preferences;
    private long lastFrame;
    private double lastShape = double.NaN;
    private readonly SpringValue previewWidth = new();
    private readonly SpringValue alertSpring = new();
    private readonly SpringValue currentWidthSpring = new();
    private readonly SpringValue currentHeightSpring = new();
    private readonly DispatcherTimer contentDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private double targetIslandWidth = 656;
    private double targetIslandHeight = 356;
    private double lastPreview = double.NaN;
    private double lastWidth = double.NaN;
    private double lastHeight = double.NaN;
    private MusicViewModel music = null!;
    private SystemViewModel system = null!;
    private TaskStore taskStore = null!;
    private TasksViewModel tasks = null!;
    private Island.Core.Focus.FocusManager focusManager = null!;
    private FocusViewModel focus = null!;
    private QuickControlsManager quickControls = null!;
    private QuickControlsViewModel controls = null!;
    private CalendarStore calendarStore = null!;
    private WeatherClient weatherClient = null!;
    private CalendarWeatherViewModel calendarWeather = null!;
    private ClipboardStore clipboardStore = null!;
    private ClipboardViewModel clipboard = null!;
    private ScreenshotsStore screenshotsStore = null!;
    private ScreenshotsWatcher screenshotsWatcher = null!;
    private ScreenshotsViewModel screenshots = null!;
    private IntPtr lastExternalForegroundHwnd = IntPtr.Zero;
    private bool suppressNextClipboardUpdate;
    private bool clipboardHotkeyRegistered;
    private AlertQueue alertQueue = null!;
    private AlertsViewModel alerts = null!;
    private AudioOsdWatcher audioOsd = null!;
    private BrightnessOsdWatcher brightnessOsd = null!;
    private PowerAlertWatcher powerAlerts = null!;
    private TemperatureAlertWatcher tempAlerts = null!;
    private bool rendering, openingIntent, suppressHover, menuOpen, escapeRegistered, previousPin;
    private string lastAppliedClockStyle = "";
    private string lastAppliedPreset = "";
    internal bool Paused { get; private set; }
    internal bool Pinned => settings.Current.IsPinned;

    private void InitializePanel()
    {
        Shell.Clip = notch.Geometry;
        InnerRim.Data = notch.Geometry;
        OuterShadow.Data = notch.Geometry;
        alertQueue = new(TimeSpan.FromSeconds(settings.Current.AlertDurationSeconds));
        alerts = new(Dispatcher, alertQueue, settings.Current, log);
        CompactAlert.DataContext = alerts;
        alerts.ActiveAlertChanged += OnAlertChanged;

        audioOsd = new(Dispatcher, alertQueue, settings.Current, log);
        brightnessOsd = new(Dispatcher, alertQueue, settings.Current, log);
        powerAlerts = new(Dispatcher, alertQueue, settings.Current, log);
        tempAlerts = new(Dispatcher, alertQueue, settings.Current, log);

        music = new(Dispatcher, settings.Current, log);
        CompactMusic.DataContext = music;
        CompactMeter.Attach(music.Spectrum);
        system = new(Dispatcher, settings.Current, log);
        system.SnapshotUpdated += tempAlerts.OnSnapshot;
        CompactStat.DataContext = system;
        system.CollapsedStatChanged += SystemStatChanged;

        taskStore = new(AppIdentity.DataDirectory, log);
        taskStore.ReminderFired += item =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                alertQueue.Enqueue(new IslandAlert(
                    Id: Guid.NewGuid().ToString("N"),
                    Type: AlertType.Custom,
                    Title: $"Reminder: {item.Title}",
                    Glyph: "\uE9D5",
                    Level: item.Priority == TaskPriority.High ? AlertLevel.Warning : AlertLevel.Info,
                    Duration: TimeSpan.FromSeconds(6),
                    PlaySound: true
                ));
            });
        };
        tasks = new(Dispatcher, taskStore);
        var tasksPage = new TasksPage(tasks, SetTypingFocus);

        focusManager = new();
        focusManager.SessionFinished += msg =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                alertQueue.Enqueue(new IslandAlert(
                    Id: Guid.NewGuid().ToString("N"),
                    Type: AlertType.Custom,
                    Title: msg,
                    Glyph: "\uE916",
                    Level: AlertLevel.Info,
                    Duration: TimeSpan.FromSeconds(5),
                    PlaySound: true
                ));
            });
        };
        focus = new(Dispatcher, focusManager);
        focusManager.StateChanged += () => Dispatcher.InvokeAsync(FocusChanged);
        CompactFocus.DataContext = focus;
        var focusPage = new FocusPage(focus);

        quickControls = new(Dispatcher, log);
        controls = new(Dispatcher, quickControls);
        var quickControlsPage = new QuickControlsPage(controls);

        var musicPage = new MusicPage(music, id => Save(s => s with { PreferredPlayer = id }),
            open => SetMenuOpen(open), () => settings.Current);
        var systemPage = new SystemPage(system, next => Save(_ => next), () => settings.Current);

        calendarStore = new(AppIdentity.DataDirectory, log);
        calendarStore.ReminderFired += ev =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                alertQueue.Enqueue(new IslandAlert(
                    Id: Guid.NewGuid().ToString("N"),
                    Type: AlertType.Custom,
                    Title: $"Event: {ev.Title}",
                    Glyph: "\uE787",
                    Level: AlertLevel.Info,
                    Duration: TimeSpan.FromSeconds(6),
                    PlaySound: true
                ));
            });
        };
        weatherClient = new(AppIdentity.DataDirectory, log);
        calendarWeather = new(Dispatcher, calendarStore, weatherClient, new Island.App.Services.WindowsCalendarService(log));
        var calendarWeatherPage = new CalendarWeatherPage(calendarWeather, SetTypingFocus);

        screenshotsStore = new(log);
        screenshotsWatcher = new(screenshotsStore,
            AppIdentity.DataDirectory,
            () => settings.Current.ScreenshotsWatchedFolders,
            () => settings.Current.ScreenshotsIncludeClipboard,
            log);
        screenshotsWatcher.Start();
        screenshots = new(Dispatcher, screenshotsStore, () => settings.Current, OnPasteScreenshotItem, OnCopyScreenshotItem, LaunchSnip);

        clipboardStore = new(AppIdentity.DataDirectory,
            () => settings.Current.ClipboardRememberAcrossRestarts,
            () => settings.Current.ClipboardHistorySize,
            () => settings.Current.ClipboardAutoExpireHours,
            () => settings.Current.ClipboardFilterSecrets,
            () => settings.Current.ClipboardIgnoredApps,
            log);
        clipboard = new(Dispatcher, clipboardStore, screenshotsStore, () => settings.Current,
            OnPasteClipboardItem, OnCopyClipboardItem,
            OnPasteScreenshotItem, OnCopyScreenshotItem, LaunchSnip);
        var clipboardPage = new ClipboardPage(clipboard, SetTypingFocus);

        navigation = new(TabButtons, Pages, PillIndicator, PillX, (Style)FindResource("IconButton"), () => settings.Current, RequestFrame,
            musicPage, systemPage, tasksPage, quickControlsPage, focusPage, calendarWeatherPage, clipboardPage);
        navigation.SelectionChanged += () =>
        {
            UpdateDemands();
            UpdateIslandTargetSize();
        };

        contentDebounceTimer.Tick += (_, _) =>
        {
            contentDebounceTimer.Stop();
            UpdateIslandTargetSize();
        };
        clipboardStore.HistoryChanged += () =>
        {
            if (navigation.ClipboardSelected)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    contentDebounceTimer.Stop();
                    contentDebounceTimer.Start();
                });
            }
        };
        tasks.TasksCountChanged += () =>
        {
            contentDebounceTimer.Stop();
            contentDebounceTimer.Start();
        };
        system.SnapshotUpdated += _ =>
        {
            if (navigation.SystemSelected)
            {
                contentDebounceTimer.Stop();
                contentDebounceTimer.Start();
            }
        };
        focus.PropertyChanged += (_, _) =>
        {
            if (navigation.FocusSelected)
            {
                contentDebounceTimer.Stop();
                contentDebounceTimer.Start();
            }
        };

        var initSize = navigation.GetDesiredSize(0);
        targetIslandWidth = initSize.Width;
        targetIslandHeight = initSize.Height;
        bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        double overhead = belowClock ? 116.0 : 80.0;
        double initContentW = Math.Max(580 - 32, Math.Round(initSize.Width - 32));
        double initContentH = Math.Max(60, Math.Round(initSize.Height - overhead));
        Header.Width = initContentW;
        Body.Width = initContentW;
        Body.Height = initContentH;
        currentWidthSpring.Set(targetIslandWidth);
        currentHeightSpring.Set(targetIslandHeight);

        music.PreviewChanged += MusicChanged;
        music.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MusicViewModel.PreviewLabel)) UpdatePreviewLabel();
        };
        PanelNavigation.AttachButtonMotion(PinButton, () => settings.Current);
        PanelNavigation.AttachButtonMotion(GearButton, () => settings.Current);
        var menu = new ContextMenu();
        MenuItem Item(string name, Action action)
        {
            var item = new MenuItem { Header = name };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
            return item;
        }
        Item("Settings", OpenSettings);
        var pin = Item("Pin", TogglePin);
        Item("Quit", Close);
        menu.Opened += (_, _) =>
        {
            pin.Header = Pinned ? "Unpin" : "Pin";
            SetMenuOpen(true);
        };
        menu.Closed += (_, _) => SetMenuOpen(false);
        Shell.ContextMenu = menu;
        previousPin = Pinned;
        ApplyPanelSettings();
        ApplyTabBarLayout();
        intent.Tick += IntentTick;
        DrawPanel();
    }

    public void SelectTab(int index)
    {
        navigation?.Select(index);
        UpdateDemands();
        UpdateIslandTargetSize();
    }

    private void UpdateDemands()
    {
        music.Display(motion.Expanded && navigation.MusicSelected, !motion.Expanded, Paused);
        system.Display(motion.Expanded && navigation.SystemSelected, !motion.Expanded, Paused);
    }

    private void OnAlertChanged()
    {
        if (alerts.HasActiveAlert)
        {
            alertSpring.Set(0);
        }
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat || (focus != null && focus.IsRunning)) ? 1 : 0;
        if (previewWidth.Value != target || alertSpring.Value != (alerts.HasActiveAlert ? 1 : 0))
            RequestFrame();
        DrawPanel();
    }

    private void SystemStatChanged()
    {
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat || (focus != null && focus.IsRunning)) ? 1 : 0;
        if (previewWidth.Value != target) RequestFrame();
        DrawPanel();
    }

    private void MusicChanged()
    {
        UpdatePreviewLabel();
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat || (focus != null && focus.IsRunning)) ? 1 : 0;
        if (previewWidth.Value != target) RequestFrame();
        DrawPanel();
    }

    private void FocusChanged()
    {
        if (navigation != null && navigation.FocusSelected)
        {
            contentDebounceTimer.Stop();
            contentDebounceTimer.Start();
        }
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat || (focus != null && focus.IsRunning)) ? 1 : 0;
        if (previewWidth.Value != target) RequestFrame();
        DrawPanel();
    }

    private void UpdatePreviewLabel() => PreviewLabel.Update(music.PreviewLabel,
        music.Preview && !music.PreviewClock && !motion.Expanded, settings.Current.ReduceMotion);

    internal void SetMenuOpen(bool open, bool checkLeave = true)
    {
        menuOpen = open;
        intent.Stop();
        SyncEscape();
        if (checkLeave && !open && !Shell.IsMouseOver && !Pinned && motion.Expanded && !Paused)
            Schedule(false);
    }

    internal void OpenPanel()
    {
        if (Paused) SetPaused(false);
        suppressHover = false;
        Expand(true);
    }

    internal void SetPaused(bool paused)
    {
        if (Paused == paused) return;
        Paused = paused;
        intent.Stop();
        if (paused)
        {
            StopFrames();
            tick.Stop();
            motion.Target(false);
            UpdateDemands();
            motion.Advance(2, true);
            DrawPanel();
            SyncEscape();
            Hide();
        }
        else
        {
            suppressHover = false;
            Show();
            Place();
            RefreshClock();
            UpdateDemands();
            if (Pinned) Expand(true);
        }
    }

    private void DisposePanel()
    {
        alerts.ActiveAlertChanged -= OnAlertChanged;
        system.SnapshotUpdated -= tempAlerts.OnSnapshot;
        audioOsd.Dispose();
        brightnessOsd.Dispose();
        powerAlerts.Dispose();
        tempAlerts.Dispose();
        alerts.Dispose();
        alertQueue.Dispose();
        music.Dispose();
        system.Dispose();
        tasks.Dispose();
        taskStore.Dispose();
        focus.Dispose();
        focusManager.Dispose();
        controls.Dispose();
        quickControls.Dispose();
        calendarWeather.Dispose();
        calendarStore.Dispose();
        weatherClient.Dispose();
        CompactMeter.Attach(null);
        intent.Stop();
        StopFrames();
        motion.Target(false);
        SyncEscape();
        if (clipboardHotkeyRegistered && source != null)
        {
            UnregisterHotKey(source.Handle, ClipboardHotkeyId);
            clipboardHotkeyRegistered = false;
        }
        clipboardStore?.Dispose();
        screenshotsWatcher?.Dispose();
        preferences?.Close();
    }
}
