using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Island.App.Interop;
using Island.App.ViewModels;
using Island.Core.Settings;

namespace Island.App.Views;

public partial class OverlayWindow : Window
{
    private readonly SettingsStore settings;
    private readonly Action<string> log;
    private readonly ClockViewModel clock = new();
    private readonly DispatcherTimer tick = new(DispatcherPriority.Background);
    private HwndSource? source;
    private bool placementPending;

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
        if (Pinned) Expand(true);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Place();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        QueuePlacement();
    }

    private nint WindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        switch (message)
        {
            case 0x0312 when wParam == EscapeId: // WM_HOTKEY
                Dismiss();
                handled = true;
                break;
            case 0x0021: // WM_MOUSEACTIVATE
                handled = true;
                return 3; // MA_NOACTIVATE still delivers the mouse event.
            case 0x007E: // WM_DISPLAYCHANGE
            case 0x001A: // WM_SETTINGCHANGE (includes monitor/work area changes)
                QueuePlacement();
                break;
            case 0x001E: // WM_TIMECHANGE
            case 0x0218 when wParam == 18: // PBT_APMRESUMEAUTOMATIC
                RefreshClock();
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
        double pixel = 1 / VisualTreeHelper.GetDpi(this).DpiScaleY;
        if (BottomEdge.Height != pixel)
        {
            BottomEdge.Height = pixel;
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
        var delay = clock.Refresh(DateTime.Now, settings.Current);
        tick.Interval = TimeSpan.FromMilliseconds(Math.Ceiling(delay.TotalMilliseconds));
        tick.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        DisposePanel();
        tick.Stop();
        tick.Tick -= ClockTick;
        settings.PropertyChanged -= SettingsChanged;
        source?.RemoveHook(WindowMessage);
        source = null;
        base.OnClosed(e);
    }
}
