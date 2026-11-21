using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Island.App.Interop;
using Island.App.Services.Alerts;
using Island.App.Services.Controls;
using Island.App.ViewModels;
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

namespace Island.App.Views;

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

    private void UpdateIslandTargetSize()
    {
        var size = navigation.GetDesiredSize(navigation.SelectedIndex);
        targetIslandWidth = size.Width;
        targetIslandHeight = size.Height;
        bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        double overhead = belowClock ? 116.0 : 80.0;
        double contentW = Math.Max(580 - 32, Math.Round(size.Width - 32));
        double contentH = Math.Max(60, Math.Round(size.Height - overhead));
        if (Header.Width != contentW) Header.Width = contentW;
        if (Body.Width != contentW) Body.Width = contentW;
        if (Body.Height != contentH) Body.Height = contentH;
        RequestFrame();
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

    private void ApplyPanelSettings()
    {
        TextOptions.SetTextRenderingMode(this,
            (settings.Current.BackgroundTransparency > 0 || settings.Current.CollapsedTransparency > 0)
                ? TextRenderingMode.Grayscale
                : TextRenderingMode.ClearType);
        CompactClock?.ApplySettings(settings.Current, music?.DominantColor);
        HeaderClock?.ApplySettings(settings.Current, music?.DominantColor);
        ReducedClock?.ApplySettings(settings.Current, music?.DominantColor);
        if (HeaderDate != null)
            HeaderDate.Visibility = settings.Current.ClockStyle == "WithDate" ? Visibility.Collapsed : Visibility.Visible;
        string action = Pinned ? "Unpin" : "Pin";
        PinButton.ToolTip = action;
        AutomationProperties.SetName(PinButton, action);
        if (Pinned) PinButton.SetResourceReference(Control.ForegroundProperty, "Theme.Accent");
        else PinButton.ClearValue(Control.ForegroundProperty);
        system?.Configure(settings.Current);
        alerts?.Configure(settings.Current);
        audioOsd?.Configure(settings.Current);
        brightnessOsd?.Configure(settings.Current);
        powerAlerts?.Configure(settings.Current);
        tempAlerts?.Configure(settings.Current);

        bool typographyChanged = lastAppliedClockStyle != settings.Current.ClockStyle ||
                                 lastAppliedPreset != settings.Current.TypographyPreset;
        lastAppliedClockStyle = settings.Current.ClockStyle;
        lastAppliedPreset = settings.Current.TypographyPreset;
        if (typographyChanged)
        {
            if (motion.Expanded) AnimateTypographyTransition();
            UpdateIslandTargetSize();
        }

        ApplyTabBarLayout();
        SyncClipboardHotkey();

        if (previousPin != Pinned)
        {
            previousPin = Pinned;
            if (Pinned && !Paused) Expand(true);
            else if (!Shell.IsMouseOver && !menuOpen && !Paused) Schedule(false);
        }
    }

    internal void OpenPanel()
    {
        if (Paused) SetPaused(false);
        suppressHover = false;
        Expand(true);
    }

    private void EnterShell(object sender, MouseEventArgs e)
    {
        intent.Stop();
        if (Paused || suppressHover || menuOpen || motion.Expanded) return;
        if (motion.Progress > 0 || settings.Current.HoverDelayMs <= 15) Expand(true);
        else Schedule(true);
    }

    private void LeaveShell(object sender, MouseEventArgs e)
    {
        suppressHover = false;
        intent.Stop();
        if (!Pinned && !Paused && !menuOpen && motion.Expanded) Schedule(false);
    }

    private void Schedule(bool opening)
    {
        intent.Stop();
        openingIntent = opening;
        int delay = opening
            ? settings.Current.HoverDelayMs
            : Math.Max(250, settings.Current.CollapseDelayMs);
        intent.Interval = TimeSpan.FromMilliseconds(delay);
        intent.Start();
    }

    private void AnimateTypographyTransition()
    {
        if (Body == null) return;
        var anim = new System.Windows.Media.Animation.DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };
        Body.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private void IntentTick(object? sender, EventArgs e)
    {
        intent.Stop();
        if (Paused || menuOpen) return;
        if (openingIntent && Shell.IsMouseOver && !suppressHover) Expand(true);
        if (!openingIntent && !Shell.IsMouseOver && !Pinned) Expand(false);
    }

    private void Expand(bool open)
    {
        intent.Stop();
        if (open)
        {
            UpdateExternalForegroundWindow();
        }
        else
        {
            Keyboard.ClearFocus();
            SetTypingFocus(false);
        }
        motion.Target(open);
        UpdateDemands();
        UpdatePreviewLabel();
        SyncEscape();
        RequestFrame();
    }

    private void RequestFrame()
    {
        if (rendering || Paused) return;
        rendering = true;
        lastFrame = Stopwatch.GetTimestamp();
        CompositionTarget.Rendering += RenderFrame;
    }

    private void RenderFrame(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        var config = settings.Current;
        double dt = Stopwatch.GetElapsedTime(lastFrame, now).TotalSeconds * config.AnimationSpeed;
        lastFrame = now;
        dt = Math.Clamp(dt, 0.001, 0.033);
        bool moving = motion.Advance(dt, config.ReduceMotion);
        bool switching = navigation.Advance(dt, config.ReduceMotion);
        bool sizingW = currentWidthSpring.Advance(targetIslandWidth, dt);
        bool sizingH = currentHeightSpring.Advance(targetIslandHeight, dt);
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat || (focus != null && focus.IsRunning)) ? 1 : 0;
        if (config.ReduceMotion)
        {
            previewWidth.Set(target);
            alertSpring.Set(alerts.HasActiveAlert ? 1 : 0);
            currentWidthSpring.Set(targetIslandWidth);
            currentHeightSpring.Set(targetIslandHeight);
            sizingW = false;
            sizingH = false;
        }
        bool widening = previewWidth.Advance(target, dt);
        bool alerting = alertSpring.Advance(alerts.HasActiveAlert ? 1 : 0, dt);
        AlertY.Y = config.ReduceMotion ? 0 : (1 - alertSpring.Value) * -12;
        DrawPanel();
        if (!moving && !switching && !widening && !alerting && !sizingW && !sizingH) StopFrames();
    }

    private void DrawPanel()
    {
        double p = Math.Clamp(motion.Progress, 0, 1.06);
        bool reduced = settings.Current.ReduceMotion;
        ReducedBase.Visibility = reduced && p > 0 && p < 1 ? Visibility.Visible : Visibility.Hidden;
        double shape = reduced && p > 0 ? 1 : p;
        double curW = currentWidthSpring.Value;
        double curH = currentHeightSpring.Value;
        if (shape != lastShape || previewWidth.Value != lastPreview || curW != lastWidth || curH != lastHeight)
        {
            double centerX = ActualWidth > 0 ? ActualWidth / 2.0 : 520.0;
            notch.Update(shape, previewWidth.Value, curW, curH, centerX);
            lastPreview = previewWidth.Value;
            lastWidth = curW;
            lastHeight = curH;
            double bottom = 35 + (curH - 35) * shape;
            double shadowTop = bottom - 5 * shape;
            EdgeOffset.Y = shadowTop - BottomEdge.Height;
            lastShape = shape;

            bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
            double overhead = belowClock ? 116.0 : 80.0;
            double liveContentW = Math.Max(300, Math.Round(curW - 32));
            double liveContentH = Math.Max(60, Math.Round(curH - overhead));
            if (Header.Width != liveContentW) Header.Width = liveContentW;
            if (Body.Width != liveContentW) Body.Width = liveContentW;
            if (Body.Height != liveContentH) Body.Height = liveContentH;
            Pages.Clip = new RectangleGeometry(new Rect(0, 0, liveContentW, liveContentH), 27, 27);
        }
        IslandBackground.Opacity = shape;
        CollapsedBackground.Opacity = Math.Max(0, 1.0 - shape);
        OuterShadow.Opacity = 0.25 + 0.15 * shape;
        Shell.Opacity = reduced && p > 0 ? Math.Clamp(p, .01, 1) : 1;
        double compactOpacity = reduced && p > 0 ? 0 : Math.Max(0, 1 - p * 2);
        bool showAlert = alerts.HasActiveAlert;
        bool showMusic = !showAlert && music.Preview;
        bool showStat = !showAlert && !showMusic && system.HasCollapsedStat;
        bool showFocus = !showAlert && !showMusic && !showStat && (focus != null && focus.IsRunning);
        bool showClockOnly = !showAlert && !showMusic && !showStat && !showFocus && settings.Current.ShowNotchClock;

        CompactAlert.Visibility = showAlert && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactAlert.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1) * Math.Clamp(alertSpring.Value, 0.01, 1);

        CompactMusic.Visibility = showMusic && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactMusic.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        CompactStat.Visibility = showStat && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactStat.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        CompactFocus.Visibility = showFocus && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactFocus.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        double clockOpacity = (showClockOnly || (showMusic && music.PreviewClock && settings.Current.ShowNotchClock)) ? compactOpacity : 0;
        CompactClock.Opacity = clockOpacity;
        CompactClock.Visibility = clockOpacity > 0.01 ? Visibility.Visible : Visibility.Hidden;
        Header.Opacity = motion.HeaderOpacity;
        Body.Opacity = motion.BodyOpacity;
        HeaderY.Y = reduced ? 0 : 6 * (1 - Header.Opacity);
        BodyY.Y = reduced ? 0 : 6 * (1 - Body.Opacity);
        Header.IsHitTestVisible = motion.Expanded && Header.Opacity > .15;
        Body.IsHitTestVisible = motion.Expanded && Body.Opacity > .15;
        UpdateBottomNeonLine();

        if (LayoutDebugOverlay != null)
        {
            if (settings.Current.LayoutDebug && shape > 0.1)
            {
                LayoutDebugOverlay.Visibility = Visibility.Visible;
                double centerX = ActualWidth > 0 ? ActualWidth / 2.0 : 520.0;
                double iLeft = centerX - curW / 2.0;
                Canvas.SetLeft(DebugIslandBounds, iLeft);
                Canvas.SetTop(DebugIslandBounds, 0);
                DebugIslandBounds.Width = curW;
                DebugIslandBounds.Height = curH;

                double cLeft = centerX - Body.Width / 2.0;
                Canvas.SetLeft(DebugHeaderBounds, cLeft);
                Canvas.SetTop(DebugHeaderBounds, 12);
                DebugHeaderBounds.Width = Header.Width;
                DebugHeaderBounds.Height = 44;

                Canvas.SetLeft(DebugContentBounds, cLeft);
                Canvas.SetTop(DebugContentBounds, 64);
                DebugContentBounds.Width = Body.Width;
                DebugContentBounds.Height = Body.Height;

                Canvas.SetLeft(DebugHud, iLeft + 8);
                Canvas.SetTop(DebugHud, curH + 6);

                double overheadDbg = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase) ? 116.0 : 80.0;
                double slackX = Math.Max(0, (curW - 32) - Body.Width);
                double slackY = Math.Max(0, (curH - overheadDbg) - Body.Height);
                string tabName = navigation != null ? PanelTab.All[navigation.SelectedIndex].Name : "Tab";
                DebugHudText.Text = $"Tab: {tabName} | Island: {curW:0}x{curH:0} | Content: {Body.Width:0}x{Body.Height:0} | Safe Inset: 16/14 DIP | Slack: X={slackX:0} Y={slackY:0} DIP";
            }
            else
            {
                LayoutDebugOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void UpdateBottomNeonLine()
    {
        double intensity = settings.Current.NeonIntensity;
        var activeNeonColor = (Color)ColorConverter.ConvertFromString(settings.Current.Theme == "Cyberpunk" ? "#FF2E97" : "#00E5FF");

        if (!settings.Current.BottomNeonLine)
        {
            BottomEdge.Visibility = Visibility.Collapsed;
            return;
        }
        BottomEdge.Visibility = Visibility.Visible;

        if (settings.Current.NeonLoadReactive && system != null && (system.CpuPercent > 85 || alerts.HasActiveAlert))
        {
            BottomEdge.Background = (Brush)FindResource("Theme.Warning");
            BottomNeonGlow.Color = (Color)ColorConverter.ConvertFromString(settings.Current.Theme == "Cyberpunk" ? "#FCEE0A" : "#FFA726");
            BottomNeonGlow.BlurRadius = 12 * intensity;
            BottomNeonGlow.Opacity = 0.9 * intensity;
            return;
        }

        if (settings.Current.NeonMusicReactive && music != null && music.Playing)
        {
            BottomEdge.Background = (Brush)FindResource("Theme.NeonLine");
            BottomNeonGlow.Color = activeNeonColor;
            double energy = music.Spectrum?.Levels is { Length: > 0 } lvls ? lvls.Average() : 0.2;
            double pulse = Math.Clamp(energy * 2.5, 0.1, 1.0);
            BottomNeonGlow.BlurRadius = (8 + 10 * pulse) * intensity;
            BottomNeonGlow.Opacity = Math.Clamp(0.5 + 0.5 * pulse, 0.4, 1.0) * intensity;
            return;
        }

        BottomEdge.Background = (Brush)FindResource("Theme.NeonLine");
        BottomNeonGlow.Color = activeNeonColor;
        BottomNeonGlow.BlurRadius = 8 * intensity;
        BottomNeonGlow.Opacity = 0.75 * intensity;
    }

    private void StopFrames()
    {
        if (!rendering) return;
        CompositionTarget.Rendering -= RenderFrame;
        rendering = false;
    }

    private void Dismiss()
    {
        if (Pinned && !Save(s => s with { IsPinned = false })) return;
        suppressHover = Shell.IsMouseOver;
        Expand(false);
    }

    private void SyncEscape()
    {
        if (source is null) return;
        bool needed = motion.Expanded && !Paused && !menuOpen && preferences is null;
        if (needed == escapeRegistered) return;
        if (needed)
        {
            escapeRegistered = RegisterHotKey(source.Handle, EscapeId, 0x4000, 0x1B);
            if (!escapeRegistered) log("Escape shortcut unavailable while the panel is open.");
        }
        else
        {
            UnregisterHotKey(source.Handle, EscapeId);
            escapeRegistered = false;
        }
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

    internal void TogglePin()
    {
        if (Paused) SetPaused(false);
        Save(s => s with { IsPinned = !s.IsPinned });
    }

    private void PinClick(object sender, RoutedEventArgs e) => TogglePin();
    private void SettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    internal void OpenSettings()
    {
        if (preferences != null) { preferences.Activate(); return; }
        preferences = new MotionSettingsWindow(settings.Current, edit => Save(edit));
        SyncEscape();
        preferences.Closed += (_, _) => { preferences = null; SyncEscape(); };
        preferences.Show();
    }

    private bool Save(Func<AppSettings, AppSettings> edit)
    {
        try { settings.Update(edit); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            log($"Settings update failed: {ex.Message}");
            MessageBox.Show("Could not save this change. " + ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void ApplyTabBarLayout()
    {
        if (HeaderRow0 == null || HeaderRow1 == null || Body == null) return;
        bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        if (belowClock)
        {
            HeaderRow0.Height = new GridLength(44);
            HeaderRow1.Height = new GridLength(36);
            Header.Height = 80;
            if (HeaderLeftCol != null) HeaderLeftCol.Width = new GridLength(72);
            if (HeaderRightCol != null) HeaderRightCol.Width = new GridLength(72);
            Body.Margin = new Thickness(0, 102, 0, 0);
            if (LegacyTabContainer != null) LegacyTabContainer.Visibility = Visibility.Collapsed;
            if (TabBarRow != null) TabBarRow.Visibility = Visibility.Visible;
            if (TabBarRow != null && TabBarContainer != null && TabBarRow.Children.Count == 0 && LegacyTabContainer?.Child != null)
            {
                var el = LegacyTabContainer.Child;
                LegacyTabContainer.Child = null;
                TabBarRow.Children.Add(el);
            }
        }
        else
        {
            HeaderRow0.Height = new GridLength(44);
            HeaderRow1.Height = new GridLength(0);
            Header.Height = 44;
            if (HeaderLeftCol != null) HeaderLeftCol.Width = GridLength.Auto;
            if (HeaderRightCol != null) HeaderRightCol.Width = new GridLength(72);
            Body.Margin = new Thickness(0, 66, 0, 0);
            if (LegacyTabContainer != null) LegacyTabContainer.Visibility = Visibility.Visible;
            if (TabBarRow != null) TabBarRow.Visibility = Visibility.Collapsed;
            if (LegacyTabContainer != null && LegacyTabContainer.Child == null && TabBarRow != null && TabBarRow.Children.Count > 0)
            {
                var el = TabBarRow.Children[0];
                TabBarRow.Children.Clear();
                LegacyTabContainer.Child = el;
            }
        }
    }

    private void SyncClipboardHotkey()
    {
        if (source is null) return;
        bool enabled = settings.Current.ClipboardEnabled;
        if (enabled && !clipboardHotkeyRegistered)
        {
            // Ctrl + Alt + V (0x0001 | 0x0002 | 0x4000 = 0x4003, Key 0x56)
            clipboardHotkeyRegistered = RegisterHotKey(source.Handle, ClipboardHotkeyId, 0x4003, 0x56);
            if (!clipboardHotkeyRegistered) log("Clipboard hotkey (Ctrl+Alt+V) could not be registered.");
        }
        else if (!enabled && clipboardHotkeyRegistered)
        {
            UnregisterHotKey(source.Handle, ClipboardHotkeyId);
            clipboardHotkeyRegistered = false;
        }
    }

    private void UpdateExternalForegroundWindow()
    {
        if (source == null) return;
        var fg = ClipboardNative.GetForegroundWindow();
        if (fg != IntPtr.Zero && fg != source.Handle)
        {
            lastExternalForegroundHwnd = fg;
        }
    }

    private void Header_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!settings.Current.HeaderMouseWheelTabSwitch) return;
        if (e.Delta < 0) navigation.SelectNext();
        else if (e.Delta > 0) navigation.SelectPrevious();
        e.Handled = true;
    }

    private void OnOverlayPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!motion.Expanded) return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            int tabIndex = -1;
            if (e.Key >= Key.D1 && e.Key <= Key.D8)
            {
                tabIndex = (int)e.Key - (int)Key.D1;
            }
            else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad8)
            {
                tabIndex = (int)e.Key - (int)Key.NumPad1;
            }

            if (tabIndex >= 0 && tabIndex < PanelTab.All.Length)
            {
                navigation.Select(tabIndex);
                e.Handled = true;
            }
        }
    }

    internal void OnClipboardUpdate()
    {
        if (suppressNextClipboardUpdate)
        {
            suppressNextClipboardUpdate = false;
            return;
        }

        if (!settings.Current.ClipboardEnabled || clipboardStore.IsPaused)
            return;

        try
        {
            if (ClipboardNative.HasExcludedClipboardFormat())
                return;

            var fg = ClipboardNative.GetForegroundWindow();
            string? appName = ClipboardNative.GetProcessNameForWindow(fg);

            if (!string.IsNullOrEmpty(appName) &&
                ClipboardPrivacyFilter.IsIgnoredApp(appName, settings.Current.ClipboardIgnoredApps))
            {
                return;
            }

            IDataObject? data = null;
            try
            {
                data = Clipboard.GetDataObject();
            }
            catch
            {
                // Clipboard open failed
            }

            if (data == null) return;

            // Check files
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                if (data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    clipboardStore.AddFiles(files, appName);
                    return;
                }
            }

            // Check bitmap
            if (data.GetDataPresent(DataFormats.Bitmap))
            {
                if (Clipboard.GetImage() is BitmapSource bmp)
                {
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bmp));
                    using var ms = new MemoryStream();
                    enc.Save(ms);
                    byte[] bytes = ms.ToArray();
                    clipboardStore.AddImage(bytes, appName);

                    // Pass to screenshot watcher if snips are captured
                    if (settings.Current.ScreenshotsIncludeClipboard)
                    {
                        screenshotsWatcher.ProcessClipboardSnip(bytes);
                    }
                    return;
                }
            }

            // Check HTML
            string? html = null;
            if (data.GetDataPresent(DataFormats.Html))
            {
                html = data.GetData(DataFormats.Html) as string;
            }

            // Check Text
            if (data.GetDataPresent(DataFormats.UnicodeText))
            {
                string? text = data.GetData(DataFormats.UnicodeText) as string;
                if (!string.IsNullOrEmpty(text))
                {
                    if (html != null)
                    {
                        clipboardStore.AddRichText(text, html, appName);
                    }
                    else
                    {
                        clipboardStore.AddText(text, appName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log($"Clipboard listener error: {ex.Message}");
        }
    }

    private void OnPasteClipboardItem(ClipboardItem item, bool plainText)
    {
        suppressNextClipboardUpdate = true;
        try
        {
            var dataObject = new DataObject();
            if (plainText || item.Type == ClipboardItemType.Text)
            {
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.RichText)
            {
                if (!string.IsNullOrEmpty(item.HtmlText))
                    dataObject.SetData(DataFormats.Html, item.HtmlText);
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.Image && item.ImageBytes != null)
            {
                using var ms = new MemoryStream(item.ImageBytes);
                var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count > 0)
                    dataObject.SetData(DataFormats.Bitmap, decoder.Frames[0]);
            }
            else if (item.Type == ClipboardItemType.Files && item.FilePaths != null)
            {
                var sc = new System.Collections.Specialized.StringCollection();
                sc.AddRange(item.FilePaths.ToArray());
                dataObject.SetFileDropList(sc);
            }

            Clipboard.SetDataObject(dataObject, true);

            if (lastExternalForegroundHwnd != IntPtr.Zero)
            {
                ClipboardNative.SetForegroundWindow(lastExternalForegroundHwnd);
                System.Threading.Thread.Sleep(30);
                ClipboardNative.SendCtrlV();
            }

            if (settings.Current.ClipboardCollapseAfterPaste && !Pinned)
            {
                Collapse();
            }
        }
        catch (Exception ex)
        {
            log($"Paste failed: {ex.Message}");
        }
    }

    private void OnCopyClipboardItem(ClipboardItem item)
    {
        suppressNextClipboardUpdate = true;
        try
        {
            var dataObject = new DataObject();
            if (item.Type == ClipboardItemType.Text)
            {
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.RichText)
            {
                if (!string.IsNullOrEmpty(item.HtmlText))
                    dataObject.SetData(DataFormats.Html, item.HtmlText);
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.Image && item.ImageBytes != null)
            {
                using var ms = new MemoryStream(item.ImageBytes);
                var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count > 0)
                    dataObject.SetData(DataFormats.Bitmap, decoder.Frames[0]);
            }
            else if (item.Type == ClipboardItemType.Files && item.FilePaths != null)
            {
                var sc = new System.Collections.Specialized.StringCollection();
                sc.AddRange(item.FilePaths.ToArray());
                dataObject.SetFileDropList(sc);
            }

            Clipboard.SetDataObject(dataObject, true);
        }
        catch (Exception ex)
        {
            log($"Copy failed: {ex.Message}");
        }
    }

    private void OnPasteScreenshotItem(ScreenshotItem item)
    {
        if (File.Exists(item.FilePath))
        {
            suppressNextClipboardUpdate = true;
            try
            {
                var bmp = new BitmapImage(new Uri(item.FilePath, UriKind.Absolute));
                Clipboard.SetImage(bmp);

                if (lastExternalForegroundHwnd != IntPtr.Zero)
                {
                    ClipboardNative.SetForegroundWindow(lastExternalForegroundHwnd);
                    System.Threading.Thread.Sleep(30);
                    ClipboardNative.SendCtrlV();
                }

                if (settings.Current.ClipboardCollapseAfterPaste && !Pinned)
                {
                    Collapse();
                }
            }
            catch (Exception ex)
            {
                log($"Screenshot paste failed: {ex.Message}");
            }
        }
    }

    private void OnCopyScreenshotItem(ScreenshotItem item)
    {
        if (File.Exists(item.FilePath))
        {
            suppressNextClipboardUpdate = true;
            try
            {
                var bmp = new BitmapImage(new Uri(item.FilePath, UriKind.Absolute));
                Clipboard.SetImage(bmp);
            }
            catch (Exception ex)
            {
                log($"Screenshot copy failed: {ex.Message}");
            }
        }
    }

    private void LaunchSnip()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-screenclip:") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            log($"LaunchSnip error: {ex.Message}");
        }
    }

    private void Collapse()
    {
        Expand(false);
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hwnd, int id);
}
