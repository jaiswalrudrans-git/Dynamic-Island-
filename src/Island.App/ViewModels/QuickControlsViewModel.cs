using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.App.Services.Controls;

namespace Island.App.ViewModels;

public sealed class QuickControlsViewModel : ObservableObject, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly QuickControlsManager manager;
    private bool disposed;

    public float VolumeFraction
    {
        get => manager.Volume;
        set => manager.SetVolume(value);
    }

    public string VolumePercentText => $"{(int)Math.Round(manager.Volume * 100)}%";
    public bool IsMuted => manager.IsMuted;
    public string VolumeGlyph => manager.IsMuted ? "\uE74F" : manager.Volume == 0 ? "\uE74F" : manager.Volume < 0.33 ? "\uE992" : manager.Volume < 0.66 ? "\uE993" : "\uE995";

    public int Brightness
    {
        get => manager.Brightness;
        set => manager.SetBrightness(value);
    }

    public string BrightnessText => $"{manager.Brightness}%";

    public bool IsMicMuted => manager.IsMicMuted;
    public string MicStatusText => manager.IsMicMuted ? "Muted" : "Active";
    public string MicGlyph => "\uE720";

    public bool? IsWifiOn => manager.IsWifiOn;
    public bool IsWifiActive => manager.IsWifiOn == true;
    public string WifiStatusText => manager.IsWifiOn.HasValue ? (manager.IsWifiOn.Value ? "On" : "Off") : "Unavailable";
    public string WifiGlyph => "\uE701";

    public bool? IsBluetoothOn => manager.IsBluetoothOn;
    public bool IsBluetoothActive => manager.IsBluetoothOn == true;
    public string BluetoothStatusText => manager.IsBluetoothOn.HasValue ? (manager.IsBluetoothOn.Value ? "On" : "Off") : "Unavailable";
    public string BluetoothGlyph => "\uE702";

    public QuickControlsViewModel(Dispatcher dispatcher, QuickControlsManager manager)
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

    public void ToggleMute() => manager.ToggleMute();
    public void ToggleMicMute() => manager.ToggleMicMute();
    public async Task ToggleWifiAsync() => await manager.ToggleWifiAsync();
    public async Task ToggleBluetoothAsync() => await manager.ToggleBluetoothAsync();
    public void TakeScreenshot() => manager.TakeScreenshot();
    public void LockPc() => manager.LockPc();
    public void SleepPc() => manager.SleepPc();
    public void OpenDndSettings() => manager.OpenDndSettings();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        manager.StateChanged -= OnStateChanged;
    }
}
