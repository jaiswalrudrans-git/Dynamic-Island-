using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Island.App.Services.Alerts;
using Island.App.ViewModels;
using Island.Core.Alerts;
using Island.Core.Motion;
using Island.Core.Settings;

namespace Island.App.Views;

public partial class OverlayWindow
{
    private const int EscapeId = 0x491;
    private readonly PanelMotion motion = new();
    private readonly NotchClip notch = new();
    private readonly DispatcherTimer intent = new();
    private PanelNavigation navigation = null!;
    private MotionSettingsWindow? preferences;
    private long lastFrame;
    private double lastShape = double.NaN;
    private readonly SpringValue previewWidth = new();
    private readonly SpringValue alertSpring = new();
    private double lastPreview = double.NaN;
    private MusicViewModel music = null!;
    private SystemViewModel system = null!;
    private AlertQueue alertQueue = null!;
    private AlertsViewModel alerts = null!;
    private AudioOsdWatcher audioOsd = null!;
    private BrightnessOsdWatcher brightnessOsd = null!;
    private PowerAlertWatcher powerAlerts = null!;
    private TemperatureAlertWatcher tempAlerts = null!;
    private bool rendering, openingIntent, suppressHover, menuOpen, escapeRegistered, previousPin;
    internal bool Paused { get; private set; }
    internal bool Pinned => settings.Current.IsPinned;

    private void InitializePanel()
    {
        Shell.Clip = notch.Geometry;
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
        var musicPage = new MusicPage(music, id => Save(s => s with { PreferredPlayer = id }),
            open => SetMenuOpen(open), () => settings.Current);
        var systemPage = new SystemPage(system, next => Save(_ => next), () => settings.Current);
        navigation = new(TabButtons, Pages, PillX, (Style)FindResource("IconButton"), () => settings.Current, RequestFrame, musicPage, systemPage);
        navigation.SelectionChanged += UpdateDemands;
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
        intent.Tick += IntentTick;
        DrawPanel();
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
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat) ? 1 : 0;
        if (previewWidth.Value != target || alertSpring.Value != (alerts.HasActiveAlert ? 1 : 0))
            RequestFrame();
        DrawPanel();
    }

    private void SystemStatChanged()
    {
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat) ? 1 : 0;
        if (previewWidth.Value != target) RequestFrame();
        DrawPanel();
    }

    private void MusicChanged()
    {
        UpdatePreviewLabel();
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat) ? 1 : 0;
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
        if (motion.Progress > 0) Expand(true);
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
        intent.Interval = TimeSpan.FromMilliseconds(opening ? settings.Current.HoverDelayMs : settings.Current.CollapseDelayMs);
        intent.Start();
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
        bool moving = motion.Advance(dt, config.ReduceMotion);
        bool switching = navigation.Advance(dt, config.ReduceMotion);
        double target = (alerts.HasActiveAlert || music.Preview || system.HasCollapsedStat) ? 1 : 0;
        if (config.ReduceMotion)
        {
            previewWidth.Set(target);
            alertSpring.Set(alerts.HasActiveAlert ? 1 : 0);
        }
        bool widening = previewWidth.Advance(target, dt);
        bool alerting = alertSpring.Advance(alerts.HasActiveAlert ? 1 : 0, dt);
        AlertY.Y = config.ReduceMotion ? 0 : (1 - alertSpring.Value) * -12;
        DrawPanel();
        if (!moving && !switching && !widening && !alerting) StopFrames();
    }

    private void DrawPanel()
    {
        double p = Math.Clamp(motion.Progress, 0, 1.06);
        bool reduced = settings.Current.ReduceMotion;
        ReducedBase.Visibility = reduced && p > 0 && p < 1 ? Visibility.Visible : Visibility.Hidden;
        double shape = reduced && p > 0 ? 1 : p;
        if (shape != lastShape || previewWidth.Value != lastPreview)
        {
            notch.Update(shape, previewWidth.Value);
            lastPreview = previewWidth.Value;
            double bottom = 32 + 288 * shape;
            double shadowTop = bottom - 5 * shape;
            BottomShadow.StartPoint = new Point(0, shadowTop);
            BottomShadow.EndPoint = new Point(0, bottom + .01);
            EdgeOffset.Y = shadowTop - BottomEdge.Height;
            lastShape = shape;
        }
        Shell.Opacity = reduced && p > 0 ? Math.Clamp(p, .01, 1) : 1;
        double compactOpacity = reduced && p > 0 ? 0 : Math.Max(0, 1 - p * 2);
        bool showAlert = alerts.HasActiveAlert;
        bool showMusic = !showAlert && music.Preview;
        bool showStat = !showAlert && !showMusic && system.HasCollapsedStat;
        bool showClockOnly = !showAlert && !showMusic && !showStat;

        CompactAlert.Visibility = showAlert && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactAlert.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1) * Math.Clamp(alertSpring.Value, 0.01, 1);

        CompactMusic.Visibility = showMusic && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactMusic.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        CompactStat.Visibility = showStat && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactStat.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        CompactClock.Opacity = (showClockOnly || (showMusic && music.PreviewClock)) ? compactOpacity : 0;
        Header.Opacity = motion.HeaderOpacity;
        Body.Opacity = motion.BodyOpacity;
        HeaderY.Y = reduced ? 0 : 6 * (1 - Header.Opacity);
        BodyY.Y = reduced ? 0 : 6 * (1 - Body.Opacity);
        Header.IsHitTestVisible = motion.Expanded && Header.Opacity > .6;
        Body.IsHitTestVisible = motion.Expanded && Body.Opacity > .6;
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
        CompactMeter.Attach(null);
        intent.Stop();
        StopFrames();
        motion.Target(false);
        SyncEscape();
        preferences?.Close();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hwnd, int id);
}
