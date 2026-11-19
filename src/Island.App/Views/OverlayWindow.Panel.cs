using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
    private bool rendering, openingIntent, suppressHover, menuOpen, escapeRegistered, previousPin;
    internal bool Paused { get; private set; }
    internal bool Pinned => settings.Current.IsPinned;

    private void InitializePanel()
    {
        Shell.Clip = notch.Geometry;
        navigation = new(TabButtons, Pages, PillX, (Style)FindResource("IconButton"), () => settings.Current, RequestFrame);
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
        DrawPanel();
        if (!moving && !switching) StopFrames();
    }

    private void DrawPanel()
    {
        double p = Math.Clamp(motion.Progress, 0, 1.06);
        bool reduced = settings.Current.ReduceMotion;
        ReducedBase.Visibility = reduced && p > 0 && p < 1 ? Visibility.Visible : Visibility.Hidden;
        double shape = reduced && p > 0 ? 1 : p;
        if (shape != lastShape)
        {
            notch.Update(shape);
            double bottom = 32 + 288 * shape;
            double shadowTop = bottom - 5 * shape;
            BottomShadow.StartPoint = new Point(0, shadowTop);
            BottomShadow.EndPoint = new Point(0, bottom + .01);
            EdgeOffset.Y = shadowTop - BottomEdge.Height;
            lastShape = shape;
        }
        Shell.Opacity = reduced && p > 0 ? Math.Clamp(p, .01, 1) : 1;
        CompactClock.Opacity = reduced && p > 0 ? 0 : Math.Max(0, 1 - p * 2);
        Header.Opacity = motion.HeaderOpacity;
        Body.Opacity = motion.BodyOpacity;
        HeaderY.Y = reduced ? 0 : 6 * (1 - Header.Opacity);
        BodyY.Y = reduced ? 0 : 6 * (1 - Body.Opacity);
        Header.IsHitTestVisible = motion.Expanded && Header.Opacity > .6;
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
