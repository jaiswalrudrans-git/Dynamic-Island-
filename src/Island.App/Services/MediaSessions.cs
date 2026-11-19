using System.IO;
using System.Diagnostics;
using System.Windows.Threading;
using Windows.Media;
using Windows.Media.Control;
using Session = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;
using Manager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;

namespace Island.App.Services;

internal sealed record PlayerChoice(Session Session, string Id, string Label);
internal sealed record MediaTrack(string Title, string Artist, string Album, MediaArtwork? Art);
internal sealed record Playback(bool Playing, bool CanToggle, bool Previous, bool Next, bool Seek,
    bool ShuffleEnabled, bool Shuffled, bool RepeatEnabled, MediaPlaybackAutoRepeatMode Repeat, double Rate);
internal readonly record struct Timeline(double Start, double End, double MinSeek, double MaxSeek,
    double Position, DateTimeOffset Updated);

internal sealed class MediaSessions(Dispatcher dispatcher, Action<string> log) : IDisposable
{
    private Manager? manager;
    private Session? manual;
    private Session? session;
    private string? preferred;
    private CancellationTokenSource? reading;
    private int lifetime;
    private bool starting, suspended = true;
    private Timeline timeline, reported;
    private double? pendingSeekPosition;
    private DateTimeOffset pendingSeekUntil;
    private readonly Dictionary<string, string> playerNames = new();
    public PlayerChoice[] Players { get; private set; } = [];
    public PlayerChoice? Selected => Players.FirstOrDefault(p => Equals(p.Session, session));
    public MediaTrack? Track { get; private set; }
    public Playback? State { get; private set; }
    public string Status { get; private set; } = "Looking for media players…";
    public double Start => timeline.Start;
    public double End => timeline.End;
    public double MinSeek => Math.Max(Start, timeline.MinSeek);
    public double MaxSeek => timeline.MaxSeek > 0 ? Math.Min(timeline.MaxSeek, End) : End;
    public event Action? Changed;

    public double Position
    {
        get
        {
            if (End <= Start) return Start;
            if (pendingSeekPosition.HasValue)
            {
                if (DateTimeOffset.UtcNow >= pendingSeekUntil)
                {
                    pendingSeekPosition = null;
                }
                else
                {
                    double elapsed = State is { Playing: true } seekPlayback
                        ? Math.Max(0, (DateTimeOffset.UtcNow - timeline.Updated).TotalSeconds) * seekPlayback.Rate
                        : 0;
                    return Math.Clamp(pendingSeekPosition.Value + elapsed, Start, End);
                }
            }
            double position = timeline.Position;
            if (State is { Playing: true } playback && timeline.Updated.Year > 1970)
                position += Math.Max(0, (DateTimeOffset.UtcNow - timeline.Updated).TotalSeconds) * playback.Rate;
            return Math.Clamp(position, Start, End);
        }
    }

    public async Task StartAsync()
    {
        suspended = false;
        if (manager != null || starting) return;
        starting = true;
        int generation = lifetime;
        try
        {
            var result = await Manager.RequestAsync();
            if (generation != lifetime) return;
            manager = result;
            manager.SessionsChanged += SessionsChanged;
            manager.CurrentSessionChanged += CurrentChanged;
            RefreshPlayers();
        }
        catch (Exception ex) when (generation == lifetime)
        {
            Status = "Windows media sessions unavailable";
            log($"Media session access failed: {ex.HResult:X8}");
            Changed?.Invoke();
        }
        catch (Exception) when (generation != lifetime) { }
        finally { if (generation == lifetime) starting = false; }
    }

    public void Prefer(string? id)
    {
        if (preferred == id) return;
        preferred = id;
        manual = null;
        RefreshPlayers();
    }

    public void Select(PlayerChoice choice)
    {
        manual = choice.Session;
        RefreshPlayers();
    }

    private void RefreshPlayers()
    {
        if (manager is null) return;
        try
        {
            var players = new List<PlayerChoice>();
            foreach (var item in manager.GetSessions())
            {
                try
                {
                    string id = item.SourceAppUserModelId;
                    string label = PlayerName(id);
                    int ordinal = players.Count(p => p.Id == id) + 1;
                    players.Add(new(item, id, ordinal == 1 ? label : $"{label} ({ordinal})"));
                }
                catch (Exception ex) { log($"Media player disappeared: {ex.HResult:X8}"); }
            }
            Players = players.ToArray();
            if (!Players.Any(p => Equals(p.Session, manual))) manual = null;
            var current = manager.GetCurrentSession();
            var chosen = Players.FirstOrDefault(p => Equals(p.Session, manual) && (preferred == null || p.Id == preferred))
                ?? Players.FirstOrDefault(p => p.Id == preferred && Equals(p.Session, session))
                ?? Players.FirstOrDefault(p => p.Id == preferred)
                ?? Players.FirstOrDefault(p => Equals(p.Session, current))
                ?? Players.FirstOrDefault();
            SetSession(chosen?.Session);
            Status = chosen is null ? "No active media session" : "";
        }
        catch (Exception ex)
        {
            log($"Could not enumerate media sessions: {ex.HResult:X8}");
            Players = [];
            SetSession(null);
            Status = "Windows media sessions unavailable";
        }
        Changed?.Invoke();
    }

    private string PlayerName(string id)
    {
        if (playerNames.TryGetValue(id, out var name)) return name;

        string lower = id.ToLowerInvariant();
        if (lower.Contains("spotify")) name = "Spotify";
        else if (lower.Contains("msedge") || lower.Contains("microsoft.edge")) name = "Microsoft Edge";
        else if (lower.Contains("chrome")) name = "Google Chrome";
        else if (lower.Contains("brave")) name = "Brave";
        else if (lower.Contains("firefox")) name = "Firefox";
        else if (lower.Contains("zunemusic") || lower.Contains("mediaplayer")) name = "Media Player";
        else if (lower.Contains("applemusic")) name = "Apple Music";
        else if (lower.Contains("vlc")) name = "VLC";
        else if (lower.Contains("tidal")) name = "Tidal";
        else if (lower.Contains("foobar2000")) name = "foobar2000";

        if (string.IsNullOrWhiteSpace(name))
        {
            try { name = Windows.ApplicationModel.AppInfo.GetFromAppUserModelId(id).DisplayInfo.DisplayName; }
            catch (Exception) { name = null; } // Unpackaged players can expose only a path or an ID.
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            if (File.Exists(id))
            {
                try
                {
                    var vi = FileVersionInfo.GetVersionInfo(id);
                    if (!string.IsNullOrWhiteSpace(vi.FileDescription)) name = vi.FileDescription;
                    else if (!string.IsNullOrWhiteSpace(vi.ProductName)) name = vi.ProductName;
                }
                catch { }
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = id.Split('!')[0].Split('\\')[^1];
            if (name.Contains('_')) name = name.Split('_')[0];
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        }

        playerNames[id] = name;
        return name;
    }

    private void SetSession(Session? next)
    {
        if (Equals(next, session)) return;
        CancelRead();
        if (session != null)
        {
            try
            {
                session.MediaPropertiesChanged -= MetadataChanged;
                session.PlaybackInfoChanged -= PlaybackChanged;
                session.TimelinePropertiesChanged -= TimelineChanged;
            }
            catch (Exception ex) { log($"Media session disconnected: {ex.HResult:X8}"); }
        }
        session = next;
        pendingSeekPosition = null;
        State = null;
        timeline = reported = default;
        Track = null;
        if (next is null) return;
        next.MediaPropertiesChanged += MetadataChanged;
        next.PlaybackInfoChanged += PlaybackChanged;
        next.TimelinePropertiesChanged += TimelineChanged;
        ReadPlayback();
        ReadTimeline();
        _ = ReadTrackAsync(next);
    }

    private void ReadPlayback()
    {
        try
        {
            var info = session?.GetPlaybackInfo();
            var c = info?.Controls;
            if (info == null || c == null) { State = null; return; }
            bool playing = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            State = new(playing, c.IsPlayPauseToggleEnabled || (playing ? c.IsPauseEnabled : c.IsPlayEnabled),
                c.IsPreviousEnabled, c.IsNextEnabled, c.IsPlaybackPositionEnabled, c.IsShuffleEnabled,
                info.IsShuffleActive == true, c.IsRepeatEnabled, info.AutoRepeatMode ?? MediaPlaybackAutoRepeatMode.None,
                info.PlaybackRate ?? 1);
        }
        catch (Exception ex) { State = null; log($"Playback info unavailable: {ex.HResult:X8}"); }
    }

    private void ReadTimeline(double? continuedPosition = null)
    {
        try
        {
            var t = session?.GetTimelineProperties();
            var next = t == null ? default : new Timeline(t.StartTime.TotalSeconds, t.EndTime.TotalSeconds,
                t.MinSeekTime.TotalSeconds, t.MaxSeekTime.TotalSeconds, t.Position.TotalSeconds, t.LastUpdatedTime);
            if (pendingSeekPosition.HasValue)
            {
                if (DateTimeOffset.UtcNow >= pendingSeekUntil)
                {
                    pendingSeekPosition = null;
                }
                else
                {
                    double diff = Math.Abs(next.Position - pendingSeekPosition.Value);
                    if (diff <= 1.5)
                    {
                        pendingSeekPosition = null;
                    }
                    else
                    {
                        // Player hasn't caught up to seek target yet; ignore pre-seek timeline event
                        return;
                    }
                }
            }
            if (next != reported) timeline = reported = next;
            else if (continuedPosition.HasValue)
                timeline = next with { Position = continuedPosition.Value, Updated = DateTimeOffset.UtcNow };
        }
        catch (Exception ex) { timeline = reported = default; log($"Media timeline unavailable: {ex.HResult:X8}"); }
    }

    private async Task ReadTrackAsync(Session target)
    {
        CancelRead();
        using var operation = new CancellationTokenSource();
        reading = operation;
        var token = operation.Token;
        try
        {
            var info = await target.TryGetMediaPropertiesAsync().AsTask(token);
            if (token.IsCancellationRequested) return;
            var track = new MediaTrack(info.Title, info.Artist, info.AlbumTitle, null);
            Track = track;
            ReadTimeline();
            Changed?.Invoke();
            var art = await MediaArtwork.ReadAsync(info.Thumbnail, token);
            if (!token.IsCancellationRequested && art != null)
            {
                Track = track with { Art = art };
                Changed?.Invoke();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested) log($"Media metadata or artwork unavailable: {ex.HResult:X8}");
        }
        finally { if (reading == operation) reading = null; }
    }

    public async Task<bool> CommandAsync(string action, double position = 0)
    {
        var target = session;
        if (target == null || State is not { } state) return false;
        try
        {
            var c = target.GetPlaybackInfo().Controls;
            var command = action switch
            {
                "Toggle" when c.IsPlayPauseToggleEnabled => target.TryTogglePlayPauseAsync(),
                "Toggle" when state.Playing && c.IsPauseEnabled => target.TryPauseAsync(),
                "Toggle" when !state.Playing && c.IsPlayEnabled => target.TryPlayAsync(),
                "Previous" when c.IsPreviousEnabled => target.TrySkipPreviousAsync(),
                "Next" when c.IsNextEnabled => target.TrySkipNextAsync(),
                "Seek" when c.IsPlaybackPositionEnabled && MaxSeek > MinSeek => target.TryChangePlaybackPositionAsync(
                    TimeSpan.FromSeconds(Math.Clamp(position, MinSeek, MaxSeek)).Ticks),
                "Shuffle" when c.IsShuffleEnabled => target.TryChangeShuffleActiveAsync(!state.Shuffled),
                "Repeat" when c.IsRepeatEnabled => target.TryChangeAutoRepeatModeAsync(state.Repeat switch
                {
                    MediaPlaybackAutoRepeatMode.None => MediaPlaybackAutoRepeatMode.List,
                    MediaPlaybackAutoRepeatMode.List => MediaPlaybackAutoRepeatMode.Track,
                    _ => MediaPlaybackAutoRepeatMode.None
                }),
                _ => null
            };
            if (command != null && await command)
            {
                if (action == "Seek")
                {
                    double clamped = Math.Clamp(position, MinSeek, MaxSeek);
                    pendingSeekPosition = clamped;
                    pendingSeekUntil = DateTimeOffset.UtcNow.AddSeconds(3);
                    timeline = timeline with { Position = clamped, Updated = DateTimeOffset.UtcNow };
                }
                return true;
            }
            return false;
        }
        catch (Exception ex) { log($"Media command {action} failed: {ex.HResult:X8}"); return false; }
    }

    private void SessionsChanged(Manager sender, SessionsChangedEventArgs args) =>
        dispatcher.InvokeAsync(() => { if (Equals(sender, manager)) RefreshPlayers(); });
    private void CurrentChanged(Manager sender, CurrentSessionChangedEventArgs args) =>
        dispatcher.InvokeAsync(() =>
        {
            if (!Equals(sender, manager)) return;
            if (preferred == null) manual = null;
            RefreshPlayers();
        });
    private void MetadataChanged(Session sender, MediaPropertiesChangedEventArgs args) =>
        dispatcher.InvokeAsync(() => { if (Equals(sender, session)) { Track = null; Changed?.Invoke(); _ = ReadTrackAsync(sender); } });
    private void PlaybackChanged(Session sender, PlaybackInfoChangedEventArgs args) =>
        dispatcher.InvokeAsync(() =>
        {
            if (!Equals(sender, session)) return;
            var previous = State;
            double before = Position;
            ReadPlayback();
            ReadTimeline(previous?.Playing != State?.Playing || previous?.Rate != State?.Rate ? before : null);
            Changed?.Invoke();
        });
    private void TimelineChanged(Session sender, TimelinePropertiesChangedEventArgs args) =>
        dispatcher.InvokeAsync(() => { if (Equals(sender, session)) { ReadTimeline(); Changed?.Invoke(); } });

    private void CancelRead() { reading?.Cancel(); reading = null; }

    public void Dispose()
    {
        if (suspended) return;
        suspended = true;
        lifetime++;
        starting = false;
        if (manager != null)
        {
            try
            {
                manager.SessionsChanged -= SessionsChanged;
                manager.CurrentSessionChanged -= CurrentChanged;
            }
            catch (Exception ex) { log($"Media manager disconnected: {ex.HResult:X8}"); }
        }
        manager = null;
        manual = null;
        Players = [];
        SetSession(null);
        Changed?.Invoke();
    }
}
