using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Dsp;
using NAudio.Wave;

namespace Island.App.Services;

internal sealed class SpectrumAnalyzer(int rate, int channels)
{
    private const int Size = 1024;
    public const int Bands = 6;
    private static readonly double[] Edges = [35, 140, 380, 950, 2400, 6000, 16000];
    private static readonly float[] Window = Enumerable.Range(0, Size)
        .Select(i => (float)(.5 - .5 * Math.Cos(2 * Math.PI * i / (Size - 1)))).ToArray();
    private readonly Complex[] fft = new Complex[Size];
    private readonly float[] latest = new float[Bands];
    private int count;
    private long lastBuffer;

    public void Push(ReadOnlySpan<float> samples)
    {
        for (int i = 0; i + channels <= samples.Length; i += channels)
        {
            float mono = 0;
            for (int c = 0; c < channels; c++) mono += samples[i + c];
            fft[count].X = mono / channels * Window[count];
            fft[count].Y = 0;
            if (++count != Size) continue;
            count = 0;
            FastFourierTransform.FFT(true, 10, fft);
            lock (latest)
            {
                for (int b = 0; b < Bands; b++)
                {
                    double peak = 0;
                    int end = Math.Min(Size / 2, (int)Math.Ceiling(Edges[b + 1] * Size / rate));
                    for (int k = Math.Max(1, (int)Math.Ceiling(Edges[b] * Size / rate)); k < end; k++)
                        peak = Math.Max(peak, fft[k].X * fft[k].X + fft[k].Y * fft[k].Y);
                    latest[b] = (float)Math.Clamp((10 * Math.Log10(Math.Max(peak, 1e-10)) + 72) / 64, 0, 1);
                }
                lastBuffer = Stopwatch.GetTimestamp();
            }
        }
    }

    public void Read(float[] output)
    {
        lock (latest)
        {
            if (Stopwatch.GetElapsedTime(lastBuffer).TotalMilliseconds > 220) Array.Clear(output);
            else latest.CopyTo(output, 0);
        }
    }
}

internal sealed class LoopbackSpectrum : IMMNotificationClient, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly Action<string> log;
    private readonly DispatcherTimer timer;
    private MMDeviceEnumerator? devices;
    private MMDevice? device;
    private WasapiLoopbackCapture? capture;
    private SpectrumAnalyzer? analyzer;
    private bool wanted;
    public bool Available => capture != null;
    public float[] Levels { get; } = new float[SpectrumAnalyzer.Bands];
    public event Action? Frame;

    public LoopbackSpectrum(Dispatcher dispatcher, Action<string> log)
    {
        this.dispatcher = dispatcher;
        this.log = log;
        timer = new(DispatcherPriority.Background, dispatcher) { Interval = TimeSpan.FromMilliseconds(40) };
        timer.Tick += (_, _) => { analyzer?.Read(Levels); Frame?.Invoke(); };
    }

    public void Enable(bool active)
    {
        if (wanted == active) return;
        wanted = active;
        if (!active) { Release(); return; }
        try
        {
            devices = new MMDeviceEnumerator();
            devices.RegisterEndpointNotificationCallback(this);
            Open();
        }
        catch (Exception ex) { log($"Output meter unavailable: {ex.HResult:X8}"); }
    }

    private void Open()
    {
        CloseCapture();
        if (!wanted || devices == null) return;
        try
        {
            device = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            capture = new WasapiLoopbackCapture(device);
            var format = capture.WaveFormat;
            capture.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels);
            var reader = new SpectrumAnalyzer(format.SampleRate, format.Channels);
            analyzer = reader;
            capture.DataAvailable += (_, e) =>
                reader.Push(MemoryMarshal.Cast<byte, float>(e.Buffer.AsSpan(0, e.BytesRecorded)));
            capture.RecordingStopped += Stopped;
            capture.StartRecording();
            timer.Start();
            Frame?.Invoke();
        }
        catch (Exception ex) { log($"Output capture unavailable: {ex.HResult:X8}"); CloseCapture(); }
    }

    private void Stopped(object? sender, StoppedEventArgs args) => dispatcher.InvokeAsync(() =>
    {
        if (sender != capture) return;
        if (args.Exception != null) log($"Output capture stopped: {args.Exception.HResult:X8}");
        CloseCapture();
    });

    private void CloseCapture()
    {
        timer.Stop();
        if (capture != null)
        {
            capture.RecordingStopped -= Stopped;
            try { capture.Dispose(); }
            catch (Exception ex) { log($"Output capture disconnected: {ex.HResult:X8}"); }
            capture = null;
        }
        device?.Dispose();
        device = null;
        analyzer = null;
        Array.Clear(Levels);
        Frame?.Invoke();
    }

    private void Release()
    {
        CloseCapture();
        if (devices == null) return;
        try { devices.UnregisterEndpointNotificationCallback(this); }
        catch (Exception ex) { log($"Output device notifications disconnected: {ex.HResult:X8}"); }
        finally { devices.Dispose(); devices = null; }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string id)
    {
        if (flow == DataFlow.Render && role == Role.Multimedia) dispatcher.InvokeAsync(() => { if (wanted) Open(); });
    }
    public void OnDeviceStateChanged(string id, DeviceState state) { }
    public void OnDeviceAdded(string id) { }
    public void OnDeviceRemoved(string id) { }
    public void OnPropertyValueChanged(string id, PropertyKey key) { }
    public void Dispose() => Enable(false);
}
