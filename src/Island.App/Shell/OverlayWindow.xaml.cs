using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Island.App.Core;
using Island.App.Features.Clipboard.Services;
using Island.App.Features.Music.ViewModels;
using Island.App.Features.SystemInfo.ViewModels;
using Island.App.Header.ViewModels;
using Island.App.Header.Views;
using Island.App.ViewModels;
using Island.App.Views;
using Island.Core.Settings;

namespace Island.App.Shell;

public partial class OverlayWindow : Window
{
    private readonly SettingsStore settings;
    private readonly Action<string> log;
    private readonly ClockViewModel clock = new();
    private readonly DispatcherTimer tick = new(DispatcherPriority.Background);
    private IslandTray? tray;
    private HwndSource? source;
    private bool placementPending;
    private bool isTyping;
    private static readonly uint WM_TASKBARCREATED = App.RegisterWindowMessage("TaskbarCreated");

    internal void AttachTray(IslandTray trayInstance) => tray = trayInstance;

    public OverlayWindow(SettingsStore settings, Action<string> log)
    {
        this.settings = settings;
        this.log = log;
        InitializeComponent();
        DataContext = clock;
        InitializePanel();
        settings.PropertyChanged += SettingsChanged;
        tick.Tick += ClockTick;
        RefreshClock();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        source = (HwndSource)PresentationSource.FromVisual(this);
        source.AddHook(WindowMessage);
        OverlayDesktop.Configure(source.Handle);
        Place();
        PreviewKeyDown += OnOverlayPreviewKeyDown;
        ClipboardNative.AddClipboardFormatListener(source.Handle);
        SyncClipboardHotkey();
        if (Pinned) Expand(true);
        else UpdateDemands();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (source != null) OverlayDesktop.Configure(source.Handle, rendered: true);
        Place();
    }

    internal void SetTypingFocus(bool typing)
    {
        isTyping = typing;
        if (source != null)
        {
            OverlayDesktop.Configure(source.Handle, rendered: true, typing: typing);
            if (typing)
            {
                SetForegroundWindow(source.Handle);
                SetActiveWindow(source.Handle);
                Activate();
            }
        }
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        QueuePlacement();
    }

    private nint WindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if ((uint)message == App.WM_ISLAND_ACTIVATE)
        {
            OpenPanel();
            handled = true;
            return 0;
        }
        if ((uint)message == WM_TASKBARCREATED)
        {
            tray?.Refresh();
            handled = true;
            return 0;
        }

        switch (message)
        {
            case 0x0312 when wParam == EscapeId: // WM_HOTKEY
                Dismiss();
                handled = true;
                break;
            case 0x0312 when wParam == ClipboardHotkeyId:
                UpdateExternalForegroundWindow();
                Expand(true);
                navigation.Select(6); // Switch to Clipboard tab
                handled = true;
                break;
            case ClipboardNative.WM_CLIPBOARDUPDATE: // 0x031D
                OnClipboardUpdate();
                handled = true;
                break;
            case 0x020E: // WM_MOUSEHWHEEL
                short tilt = (short)((wParam >> 16) & 0xFFFF);
                if (tilt > 0) navigation.SelectNext();
                else if (tilt < 0) navigation.SelectPrevious();
                handled = true;
                break;
            case 0x0021: // WM_MOUSEACTIVATE
                if (isTyping)
                {
                    handled = false;
                    return 1; // MA_ACTIVATE
                }
                handled = true;
                return 3; // MA_NOACTIVATE still delivers the mouse event.
            case 0x007E: // WM_DISPLAYCHANGE
            case 0x001A: // WM_SETTINGCHANGE (includes monitor/work area changes)
                QueuePlacement();
                break;
            case 0x001E: // WM_TIMECHANGE
            case 0x0218: // WM_POWERBROADCAST
                if (wParam == 18) RefreshClock();
                powerAlerts?.CheckPowerStatus();
                break;
        }
        return 0;
    }

    private void SettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SettingsStore.Current)) return;
        RefreshClock();
        QueuePlacement();
        ApplyPanelSettings();
        music.Configure(settings.Current);
        system.Configure(settings.Current);
    }

    private void QueuePlacement()
    {
        if (source is null || placementPending) return;
        placementPending = true;
        Dispatcher.InvokeAsync(() =>
        {
            placementPending = false;
            Place();
        }, DispatcherPriority.Loaded);
    }

    private void Place()
    {
        if (source is null) return;
        double lineH = Math.Max(1.5, 1 / VisualTreeHelper.GetDpi(this).DpiScaleY);
        if (BottomEdge.Height != lineH)
        {
            BottomEdge.Height = lineH;
            lastShape = double.NaN;
            DrawPanel();
        }
        try
        {
            OverlayDesktop.Place(source.Handle, settings.Current.MonitorDeviceName);
        }
        catch (Win32Exception ex)
        {
            log($"Could not position overlay: {ex.Message}");
        }
    }

    private void ClockTick(object? sender, EventArgs e) => RefreshClock();

    private void RefreshClock()
    {
        tick.Stop();
        if (Paused) return;
        var now = DateTime.Now;
        var delay = clock.Refresh(now, settings.Current);
        CompactClock.Time = now;
        HeaderClock.Time = now;
        ReducedClock.Time = now;
        tick.Interval = TimeSpan.FromMilliseconds(Math.Ceiling(delay.TotalMilliseconds));
        tick.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (source != null)
        {
            ClipboardNative.RemoveClipboardFormatListener(source.Handle);
        }
        DisposePanel();
        tick.Stop();
        tick.Tick -= ClockTick;
        settings.PropertyChanged -= SettingsChanged;
        source?.RemoveHook(WindowMessage);
        source = null;
        base.OnClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")]
    private static extern nint SetActiveWindow(nint hwnd);
}
