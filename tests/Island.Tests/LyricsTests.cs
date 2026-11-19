using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Island.Core.Music;
using Xunit;

namespace Island.Tests;

public sealed class LyricsTests
{
    [Fact]
    public void LinesFollowOffsetsRepeatedTimestampsAndBackwardSeeks()
    {
        var lyrics = SyncedLyrics.Parse("[offset:200]\n[00:03.20][00:05.20]Second\n[00:01.2]First\n[00:04.20]\n[99:99]invalid");
        Assert.Equal(["First", "Second", "", "Second"], lyrics.Lines.Select(l => l.Text));
        Assert.Equal(-1, lyrics.At(.9));
        Assert.Equal(1, lyrics.At(3));
        Assert.Equal(2, lyrics.At(4.5));
        Assert.Equal(3, lyrics.At(9));
        Assert.Equal(0, lyrics.At(1.1));
    }

    [Fact]
    public async Task LookupEncodesTheTrackAndReusesItsResult()
    {
        using var handler = new Stub(_ => new(HttpStatusCode.OK)
            { Content = new StringContent("""{"syncedLyrics":"[00:01.00]Hello","instrumental":false}""") });
        using var http = new HttpClient(handler);
        var client = new LyricsClient(http);
        var track = new TrackSignature("One & Two", "A/B", "Album", 220);
        var result = await client.GetAsync(track, default);
        Assert.Equal("Hello", Assert.Single(result.Lyrics.Lines).Text);
        Assert.Same(result, await client.GetAsync(track, default));
        Assert.Equal(1, handler.Count);
        Assert.Contains("One%20%26%20Two", handler.Url);
        Assert.Contains("A%2FB", handler.Url);
        Assert.Contains("duration=220", handler.Url);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, """{"instrumental":true,"syncedLyrics":null}""", "Instrumental")]
    [InlineData(HttpStatusCode.OK, """{"plainLyrics":"untimed"}""", "No synced lyrics available")]
    [InlineData(HttpStatusCode.NotFound, "{}", "No synced lyrics available")]
    [InlineData(HttpStatusCode.OK, "bad json", "Lyrics service unavailable")]
    public async Task MissingAndInvalidLyricsHaveExplicitStates(HttpStatusCode code, string body, string status)
    {
        using var handler = new Stub(_ => new(code) { Content = new StringContent(body) });
        using var http = new HttpClient(handler);
        var result = await new LyricsClient(http).GetAsync(new("Title", "Artist", "", 0), default);
        Assert.Empty(result.Lyrics.Lines);
        Assert.Equal(status, result.Status);
    }

    [Fact]
    public async Task RateLimitAppliesAcrossTracks()
    {
        using var handler = new Stub(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });
        using var http = new HttpClient(handler);
        var client = new LyricsClient(http);
        await client.GetAsync(new("First", "Artist", "", 0), default);
        var second = await client.GetAsync(new("Second", "Artist", "", 0), default);
        Assert.Equal(1, handler.Count);
        Assert.Equal("Lyrics service is busy", second.Status);
    }

    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Count { get; private set; }
        public string? Url { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();
            Count++;
            Url = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(reply(request));
        }
    }
}
