using System.Windows.Threading;
using Island.Core.Alerts;
using Island.Core.Settings;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Island.App.Services.Alerts;

public sealed class AudioOsdWatcher : IMMNotificationClient, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly AlertQueue alertQueue;
    private readonly Action<string>? log;
    private readonly object gate = new();

    private MMDeviceEnumerator? enumerator;
    private MMDevice? renderDevice;
    private MMDevice? captureDevice;

    private float lastVolume = -1;
    private bool? lastRenderMute;
    private bool? lastCaptureMute;

    private AppSettings settings;
    private bool disposed;

    public AudioOsdWatcher(Dispatcher dispatcher, AlertQueue alertQueue, AppSettings settings, Action<string>? log = null)
    {
        this.dispatcher = dispatcher;
        this.alertQueue = alertQueue;
        this.settings = settings;
        this.log = log;

        InitializeDevices();
    }

    public void Configure(AppSettings newSettings)
    {
        lock (gate)
        {
            settings = newSettings;
        }
    }

    private void InitializeDevices()
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
            log?.Invoke($"Audio OSD initialization failed: {ex.Message}");
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
                lastVolume = renderDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
                lastRenderMute = renderDevice.AudioEndpointVolume.Mute;
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
                lastCaptureMute = captureDevice.AudioEndpointVolume.Mute;
                captureDevice.AudioEndpointVolume.OnVolumeNotification += OnCaptureVolumeNotification;
            }
        }
        catch { }
    }

    private void OnRenderVolumeNotification(AudioVolumeNotificationData data)
    {
        lock (gate)
        {
            if (disposed || !settings.AlertsEnabled || !settings.AlertVolumeOsdEnabled) return;

            float currentVol = data.MasterVolume;
            bool currentMute = data.Muted;

            // Don't fire if nothing changed
            if (Math.Abs(currentVol - lastVolume) < 0.005f && currentMute == lastRenderMute)
                return;

            lastVolume = currentVol;
            lastRenderMute = currentMute;

            int pct = (int)Math.Round(currentVol * 100);
            string title = currentMute ? "Muted" : $"Volume {pct}%";
            string glyph = currentMute ? "\uE74F" : pct == 0 ? "\uE74F" : pct < 33 ? "\uE992" : pct < 66 ? "\uE993" : "\uE995";

            dispatcher.InvokeAsync(() =>
            {
                alertQueue.Enqueue(new IslandAlert(
                    Id: "osd-volume",
                    Type: AlertType.Volume,
                    Title: title,
                    Glyph: glyph,
                    Level: AlertLevel.Info,
                    Progress: currentVol,
                    Duration: TimeSpan.FromSeconds(Math.Min(settings.AlertDurationSeconds, 3))
                ));
            });
        }
    }

    private void OnCaptureVolumeNotification(AudioVolumeNotificationData data)
    {
        lock (gate)
        {
            if (disposed || !settings.AlertsEnabled || !settings.AlertMicOsdEnabled) return;

            bool currentMute = data.Muted;
            if (currentMute == lastCaptureMute) return;
            lastCaptureMute = currentMute;

            string title = currentMute ? "Mic Muted" : "Mic Unmuted";
            string glyph = currentMute ? "\uE720" : "\uE720";
            var level = currentMute ? AlertLevel.Warning : AlertLevel.Info;

            dispatcher.InvokeAsync(() =>
            {
                alertQueue.Enqueue(new IslandAlert(
                    Id: "osd-mic",
                    Type: AlertType.Mic,
                    Title: title,
                    Glyph: glyph,
                    Level: level,
                    Duration: TimeSpan.FromSeconds(Math.Min(settings.AlertDurationSeconds, 3))
                ));
            });
        }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        lock (gate)
        {
            if (disposed) return;
            if (flow == DataFlow.Render && role == Role.Multimedia)
            {
                AttachRenderDevice();
            }
            else if (flow == DataFlow.Capture && role == Role.Communications)
            {
                AttachCaptureDevice();
            }
        }
    }

    public void OnDeviceAdded(string pwstrDeviceId) { }
    public void OnDeviceRemoved(string pwstrDeviceId) { }
    public void OnDeviceStateChanged(string pwstrDeviceId, DeviceState dwNewState) { }
    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;

            try
            {
                if (enumerator != null)
                {
                    enumerator.UnregisterEndpointNotificationCallback(this);
                }
            }
            catch { }

            try
            {
                if (renderDevice != null)
                {
                    renderDevice.AudioEndpointVolume.OnVolumeNotification -= OnRenderVolumeNotification;
                    renderDevice.Dispose();
                    renderDevice = null;
                }
            }
            catch { }

            try
            {
                if (captureDevice != null)
                {
                    captureDevice.AudioEndpointVolume.OnVolumeNotification -= OnCaptureVolumeNotification;
                    captureDevice.Dispose();
                    captureDevice = null;
                }
            }
            catch { }

            enumerator?.Dispose();
            enumerator = null;
        }
    }
}
