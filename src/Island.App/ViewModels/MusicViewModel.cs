using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.App.Services;
using Island.Core.Music;
using Island.Core.Settings;
using Windows.Media;

namespace Island.App.ViewModels;

internal sealed class MusicViewModel : ObservableObject, IDisposable
{
    private readonly MediaSessions sessions;
    private readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(8), MaxResponseContentBufferSize = 1024 * 1024 };
    private readonly LyricsClient lyricsClient;
    private readonly DispatcherTimer timer;
    private AppSettings settings;
    private bool detail, collapsed, paused = true, disposed, commandBusy;
    private CancellationTokenSource? lookup;
    private TrackSignature? signature;
    private LyricsResult? lyrics;
    private int line = -2;
    private string commandError = "";
    private double position;
    public LoopbackSpectrum Spectrum { get; }
    public PlayerChoice[] Players => sessions.Players;
    public PlayerChoice? Selected => sessions.Selected;
    public bool Pinned => settings.PreferredPlayer != null && settings.PreferredPlayer == Selected?.Id;
    public bool HasPlayer => Selected != null;
    public string Player => Selected?.Label ?? "";
    public string EmptyStatus => sessions.Status;
    public string Title => string.IsNullOrEmpty(sessions.Track?.Title) ? "Untitled track" : sessions.Track.Title;
    public string Artist => sessions.Track?.Artist ?? "";
    public BitmapSource? Cover => sessions.Track?.Art?.Image;
    public bool MissingCover => Cover == null;
    public Color Glow => settings.CoverGlow && Playing && sessions.Track?.Art is { } art
        ? Color.FromArgb(150, art.Dominant.R, art.Dominant.G, art.Dominant.B) : Colors.Transparent;
    public bool Playing => sessions.State?.Playing == true;
    public bool Preview => !paused && settings.ShowMusicPreview && Playing;
    public bool PreviewClock => !Preview || settings.MusicPreviewText == "Clock";
    public string PreviewLabel => settings.MusicPreviewText == "Lyric" && CurrentLyric.Length > 0 ? CurrentLyric : sessions.Track?.Title ?? "";
    public bool CanToggle => !commandBusy && sessions.State?.CanToggle == true;
    public bool CanPrevious => !commandBusy && sessions.State?.Previous == true;
    public bool CanNext => !commandBusy && sessions.State?.Next == true;
    public bool CanShuffle => !commandBusy && sessions.State?.ShuffleEnabled == true;
    public bool CanRepeat => !commandBusy && sessions.State?.RepeatEnabled == true;
    public bool Shuffled => sessions.State?.Shuffled == true;
    public bool Repeating => sessions.State?.Repeat is MediaPlaybackAutoRepeatMode.List or MediaPlaybackAutoRepeatMode.Track;
    public string RepeatHint => $"Repeat · {sessions.State?.Repeat ?? MediaPlaybackAutoRepeatMode.None}";
    public string PlayGlyph => Playing ? "\uE769" : "\uE768";
    public string PlayHint => Playing ? "Pause" : "Play";
    public bool HasTimeline => sessions.End > sessions.Start;
    public bool CanSeek => !commandBusy && sessions.State?.Seek == true && sessions.MaxSeek > sessions.MinSeek && HasTimeline;
    public double Fraction => HasTimeline ? Math.Clamp((position - sessions.Start) / (sessions.End - sessions.Start), 0, 1) : 0;
    public string Elapsed => HasTimeline ? Format(position - sessions.Start) : "";
    public string Duration => HasTimeline ? Format(sessions.End - sessions.Start) : "";
    public bool ShowLyrics => settings.LyricsEnabled;
    public bool ReducedMotion => settings.ReduceMotion;
    public double AnimationSpeed => settings.AnimationSpeed;
    public string PreviousLyric => At(line - 1);
    public string CurrentLyric => At(line);
    public string NextLyric => At(line + 1);
    public string LyricDisplay => commandError.Length > 0 ? commandError :
        lyrics?.Lyrics.Lines.Count > 0 ? HasTimeline ? CurrentLyric : "Player timing unavailable" :
        lookup != null ? "Finding lyrics…" : lyrics?.Status ?? "No synced lyrics available";
    public event Action? PreviewChanged;

    public MusicViewModel(Dispatcher dispatcher, AppSettings config, Action<string> log)
    {
        settings = config;
        sessions = new(dispatcher, log);
        sessions.Prefer(config.PreferredPlayer);
        sessions.Changed += Refresh;
        Spectrum = new(dispatcher, log);
        lyricsClient = new(http);
        timer = new(DispatcherPriority.Background, dispatcher) { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) => UpdatePosition();
    }

    public void Configure(AppSettings config)
    {
        settings = config;
        sessions.Prefer(config.PreferredPlayer);
        Refresh();
    }

    public void Display(bool showDetail, bool showCompact, bool suspend)
    {
        if (disposed || (detail, collapsed, paused) == (showDetail, showCompact, suspend)) return;
        bool resume = paused && !suspend;
        detail = showDetail; collapsed = showCompact; paused = suspend;
        if (paused) sessions.Dispose();
        else if (resume || showDetail) _ = sessions.StartAsync();
        Refresh();
    }

    public void Select(PlayerChoice player) => sessions.Select(player);

    private static string Format(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
    }

    private string At(int index) => lyrics != null && index >= 0 && index < lyrics.Lyrics.Lines.Count ? lyrics.Lyrics.Lines[index].Text : "";

    private void Refresh()
    {
        if (disposed) return;
        bool compact = collapsed && Preview;
        bool useLyrics = !paused && settings.LyricsEnabled && (detail || (compact && settings.MusicPreviewText == "Lyric"));
        var track = sessions.Track;
        TrackSignature? key = useLyrics && !string.IsNullOrWhiteSpace(track?.Title) && !string.IsNullOrWhiteSpace(track.Artist)
            ? new(track.Title, track.Artist, track.Album, (int)Math.Round(sessions.End - sessions.Start)) : null;
        if (signature != key)
        {
            lookup?.Cancel();
            lookup = null;
            signature = key;
            lyrics = null;
            line = -2;
            if (key != null) _ = LoadLyricsAsync(key);
        }
        Spectrum.Enable(!paused && settings.MusicEqualizer && Playing && (compact || detail));
        timer.IsEnabled = !paused && Playing && HasTimeline && (detail || (useLyrics && compact));
        UpdatePosition();
        OnPropertyChanged(string.Empty);
        PreviewChanged?.Invoke();
    }

    private async Task LoadLyricsAsync(TrackSignature key)
    {
        using var operation = new CancellationTokenSource();
        lookup = operation;
        try
        {
            await Task.Delay(250, operation.Token);
            var result = await lyricsClient.GetAsync(key, operation.Token);
            if (operation.IsCancellationRequested) return;
            lyrics = result;
            line = -2;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested) { }
        finally
        {
            if (operation == lookup)
            {
                lookup = null;
                UpdatePosition();
                OnPropertyChanged(nameof(LyricDisplay));
            }
        }
    }

    private void UpdatePosition()
    {
        position = sessions.Position;
        OnPropertyChanged(nameof(Fraction));
        OnPropertyChanged(nameof(Elapsed));
        int index = HasTimeline ? lyrics?.Lyrics.At(position - sessions.Start) ?? -1 : -1;
        if (index == line) return;
        line = index;
        OnPropertyChanged(nameof(PreviousLyric));
        OnPropertyChanged(nameof(CurrentLyric));
        OnPropertyChanged(nameof(NextLyric));
        OnPropertyChanged(nameof(LyricDisplay));
        OnPropertyChanged(nameof(PreviewLabel));
    }

    public async Task CommandAsync(string action, double fraction = 0)
    {
        if (commandBusy) return;
        var original = Selected?.Session;
        commandBusy = true;
        commandError = "";
        OnPropertyChanged(string.Empty);
        try
        {
            bool accepted = await sessions.CommandAsync(action, sessions.Start + fraction * (sessions.End - sessions.Start));
            if (!accepted && Equals(original, Selected?.Session)) commandError = "Player declined the control";
        }
        finally
        {
            commandBusy = false;
            if (!disposed) Refresh();
        }
    }

    public void Dispose()
    {
        disposed = true;
        sessions.Changed -= Refresh;
        lookup?.Cancel();
        lookup = null;
        timer.Stop();
        sessions.Dispose();
        Spectrum.Dispose();
        http.Dispose();
    }
}
