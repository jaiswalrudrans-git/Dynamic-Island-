using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Windows.Devices.Radios;

namespace Island.App.Services.Controls;

public sealed class QuickControlsManager : IMMNotificationClient, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly Action<string>? log;
    private readonly object gate = new();

    private MMDeviceEnumerator? enumerator;
    private MMDevice? renderDevice;
    private MMDevice? captureDevice;

    private Radio? wifiRadio;
    private Radio? btRadio;

    private float currentVolume;
    private bool isMuted;
    private bool isMicMuted;
    private int currentBrightness = 50;
    private bool? isWifiOn;
    private bool? isBtOn;
    private bool disposed;

    public float Volume => currentVolume;
    public bool IsMuted => isMuted;
    public bool IsMicMuted => isMicMuted;
    public int Brightness => currentBrightness;
    public bool? IsWifiOn => isWifiOn;
    public bool? IsBluetoothOn => isBtOn;
    public string OutputDeviceName => renderDevice?.FriendlyName ?? "Speakers";

    public string FocusAssistState
    {
        get
        {
            try
            {
                if (SHQueryUserNotificationState(out int qState) == 0)
                {
                    return qState switch
                    {
                        5 => "Off",
                        6 => "Priority only",
                        2 => "Alarms only",
                        3 or 4 => "Presentation",
                        _ => "Open"
                    };
                }
            }
            catch { }
            return "Open";
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int qState);

    public event Action? StateChanged;

    public QuickControlsManager(Dispatcher dispatcher, Action<string>? log = null)
    {
        this.dispatcher = dispatcher;
        this.log = log;

        InitializeAudio();
        _ = InitializeRadiosAsync();
        _ = QueryBrightnessAsync();
    }

    private void InitializeAudio()
    {
        try
        {
            enumerator = new MMDeviceEnumerator();
            enumerator.RegisterEndpointNotificationCallback(this);
            AttachRenderDevice();
            AttachCaptureDevice();
        }
        catch (Exception ex)
        {
            log?.Invoke($"Quick controls audio init error: {ex.Message}");
        }
    }

    private void AttachRenderDevice()
    {
        try
        {
            if (renderDevice != null)
            {
                renderDevice.AudioEndpointVolume.OnVolumeNotification -= OnRenderVolumeNotification;
                renderDevice.Dispose();
                renderDevice = null;
            }

            if (enumerator == null) return;
            renderDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (renderDevice != null)
            {
                currentVolume = renderDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                isMuted = renderDevice.AudioEndpointVolume.Mute;
                renderDevice.AudioEndpointVolume.OnVolumeNotification += OnRenderVolumeNotification;
            }
        }
        catch { }
    }

    private void AttachCaptureDevice()
    {
        try
        {
            if (captureDevice != null)
            {
                captureDevice.AudioEndpointVolume.OnVolumeNotification -= OnCaptureVolumeNotification;
                captureDevice.Dispose();
                captureDevice = null;
            }

            if (enumerator == null) return;
            captureDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            if (captureDevice != null)
            {
                isMicMuted = captureDevice.AudioEndpointVolume.Mute;
                captureDevice.AudioEndpointVolume.OnVolumeNotification += OnCaptureVolumeNotification;
            }
        }
        catch { }
    }

    private void OnRenderVolumeNotification(AudioVolumeNotificationData data)
    {
        lock (gate)
        {
            currentVolume = data.MasterVolume;
            isMuted = data.Muted;
        }
        dispatcher.InvokeAsync(() => StateChanged?.Invoke());
    }

    private void OnCaptureVolumeNotification(AudioVolumeNotificationData data)
    {
        lock (gate)
        {
            isMicMuted = data.Muted;
        }
        dispatcher.InvokeAsync(() => StateChanged?.Invoke());
    }

    public void SetVolume(float volume)
    {
        lock (gate)
        {
            try
            {
                if (renderDevice != null)
                {
                    renderDevice.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(volume, 0, 1);
                    currentVolume = volume;
                }
            }
            catch { }
        }
        StateChanged?.Invoke();
    }

    public void ToggleMute()
    {
        lock (gate)
        {
            try
            {
                if (renderDevice != null)
                {
                    bool next = !renderDevice.AudioEndpointVolume.Mute;
                    renderDevice.AudioEndpointVolume.Mute = next;
                    isMuted = next;
                }
            }
            catch { }
        }
        StateChanged?.Invoke();
    }

    public void ToggleMicMute()
    {
        lock (gate)
        {
            try
            {
                if (captureDevice != null)
                {
                    bool next = !captureDevice.AudioEndpointVolume.Mute;
                    captureDevice.AudioEndpointVolume.Mute = next;
                    isMicMuted = next;
                }
            }
            catch { }
        }
        StateChanged?.Invoke();
    }

    public void SetBrightness(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        lock (gate) currentBrightness = percent;
        StateChanged?.Invoke();

        Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    new ManagementScope(@"\\.\root\wmi"),
                    new SelectQuery("WmiMonitorBrightnessMethods"));

                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        obj.InvokeMethod("WmiSetBrightness", new object[] { 1, (byte)percent });
                    }
                }
            }
            catch { }
        });
    }

    private async Task QueryBrightnessAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    new ManagementScope(@"\\.\root\wmi"),
                    new SelectQuery("WmiMonitorBrightness"));

                foreach (ManagementObject obj in searcher.Get())
                {
                    using (obj)
                    {
                        if (obj["CurrentBrightness"] is byte b)
                        {
                            lock (gate) currentBrightness = b;
                            _ = dispatcher.InvokeAsync(() => StateChanged?.Invoke());
                            break;
                        }
                    }
                }
            }
            catch { }
        });
    }

    private async Task InitializeRadiosAsync()
    {
        try
        {
            var radios = await Radio.GetRadiosAsync();
            foreach (var r in radios)
            {
                if (r.Kind == RadioKind.WiFi)
                {
                    wifiRadio = r;
                    isWifiOn = r.State == RadioState.On;
                    r.StateChanged += (_, _) =>
                    {
                        isWifiOn = r.State == RadioState.On;
                        _ = dispatcher.InvokeAsync(() => StateChanged?.Invoke());
                    };
                }
                else if (r.Kind == RadioKind.Bluetooth)
                {
                    btRadio = r;
                    isBtOn = r.State == RadioState.On;
                    r.StateChanged += (_, _) =>
                    {
                        isBtOn = r.State == RadioState.On;
                        _ = dispatcher.InvokeAsync(() => StateChanged?.Invoke());
                    };
                }
            }
            await dispatcher.InvokeAsync(() => StateChanged?.Invoke());
        }
        catch (Exception ex)
        {
            log?.Invoke($"Radios init error: {ex.Message}");
        }
    }

    public async Task ToggleWifiAsync()
    {
        if (wifiRadio == null) return;
        try
        {
            var target = wifiRadio.State == RadioState.On ? RadioState.Off : RadioState.On;
            await wifiRadio.SetStateAsync(target);
            isWifiOn = target == RadioState.On;
            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            log?.Invoke($"Wi-Fi toggle error: {ex.Message}");
        }
    }

    public async Task ToggleBluetoothAsync()
    {
        if (btRadio == null) return;
        try
        {
            var target = btRadio.State == RadioState.On ? RadioState.Off : RadioState.On;
            await btRadio.SetStateAsync(target);
            isBtOn = target == RadioState.On;
            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            log?.Invoke($"Bluetooth toggle error: {ex.Message}");
        }
    }

    public void TakeScreenshot()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-screenclip:") { UseShellExecute = true });
        }
        catch { }
    }

    public void LockPc()
    {
        try { LockWorkStation(); } catch { }
    }

    public void SleepPc()
    {
        try { SetSuspendState(false, true, false); } catch { }
    }

    public void OpenDndSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:quiethours") { UseShellExecute = true });
        }
        catch { }
    }

    public void OpenTaskManager()
    {
        try
        {
            Process.Start(new ProcessStartInfo("taskmgr") { UseShellExecute = true });
        }
        catch { }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        lock (gate)
        {
            if (disposed) return;
            if (flow == DataFlow.Render && role == Role.Multimedia) AttachRenderDevice();
            else if (flow == DataFlow.Capture && role == Role.Communications) AttachCaptureDevice();
        }
        dispatcher.InvokeAsync(() => StateChanged?.Invoke());
    }

    public void OnDeviceAdded(string pwstrDeviceId) { }
    public void OnDeviceRemoved(string pwstrDeviceId) { }
    public void OnDeviceStateChanged(string pwstrDeviceId, DeviceState dwNewState) { }
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;

            try
            {
                if (enumerator != null) enumerator.UnregisterEndpointNotificationCallback(this);
                if (renderDevice != null) renderDevice.AudioEndpointVolume.OnVolumeNotification -= OnRenderVolumeNotification;
                if (captureDevice != null) captureDevice.AudioEndpointVolume.OnVolumeNotification -= OnCaptureVolumeNotification;
            }
            catch { }

            renderDevice?.Dispose();
            captureDevice?.Dispose();
            enumerator?.Dispose();
        }
    }
}
