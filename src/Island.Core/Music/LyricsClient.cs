using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Island.Core.Music;

public sealed record TrackSignature(string Title, string Artist, string Album, int Duration);
public sealed record LyricsResult(SyncedLyrics Lyrics, string Status);

public sealed class LyricsClient(HttpClient client)
{
    private readonly Dictionary<TrackSignature, LyricsResult> cache = new();
    private DateTimeOffset retryAfter;
    private static LyricsResult Empty(string status) => new(SyncedLyrics.Parse(null), status);

    public async Task<LyricsResult> GetAsync(TrackSignature track, CancellationToken cancel)
    {
        if (cache.TryGetValue(track, out var cached)) return cached;
        if (DateTimeOffset.UtcNow < retryAfter) return Empty("Lyrics service is busy");
        string query = $"track_name={Uri.EscapeDataString(track.Title)}&artist_name={Uri.EscapeDataString(track.Artist)}";
        if (!string.IsNullOrWhiteSpace(track.Album)) query += "&album_name=" + Uri.EscapeDataString(track.Album);
        if (track.Duration is > 0 and <= 3600) query += "&duration=" + track.Duration.ToString(CultureInfo.InvariantCulture);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://lrclib.net/api/get?" + query);
            request.Headers.UserAgent.ParseAdd(AppIdentity.Name + "/0.3");
            using var response = await client.SendAsync(request, cancel);
            LyricsResult result;
            if (response.StatusCode == HttpStatusCode.NotFound) result = Empty("No synced lyrics available");
            else if (!response.IsSuccessStatusCode)
            {
                var retry = response.Headers.RetryAfter;
                if (retry != null || response.StatusCode == HttpStatusCode.TooManyRequests)
                    retryAfter = retry?.Date ?? DateTimeOffset.UtcNow.Add(retry?.Delta ?? TimeSpan.FromMinutes(1));
                return Empty("Lyrics service unavailable");
            }
            else
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancel));
                var root = json.RootElement;
                var lyrics = SyncedLyrics.Parse(root.TryGetProperty("syncedLyrics", out var lrc) ? lrc.GetString() : null);
                bool instrumental = root.TryGetProperty("instrumental", out var flag) && flag.ValueKind == JsonValueKind.True;
                result = new(lyrics, lyrics.Lines.Count > 0 ? "" : instrumental ? "Instrumental" : "No synced lyrics available");
            }
            cancel.ThrowIfCancellationRequested();
            if (cache.Count >= 48) cache.Remove(cache.Keys.First());
            cache[track] = result;
            return result;
        }
        catch (OperationCanceledException) when (!cancel.IsCancellationRequested) { return Empty("Lyrics request timed out"); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or UriFormatException)
        { return Empty("Lyrics service unavailable"); }
    }
}
