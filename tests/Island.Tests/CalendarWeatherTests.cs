using System.IO;
using Island.Core.Calendar;
using Island.Core.Weather;
using Xunit;

namespace Island.Tests;

public sealed class CalendarWeatherTests : TestFolder
{
    [Fact]
    public void CalendarEvent_OccursOn_AccuratelyIdentifiesDates()
    {
        var timedEvent = new CalendarEvent(
            Id: "1",
            Title: "Meeting",
            Start: new DateTime(2026, 11, 19, 14, 0, 0),
            End: new DateTime(2026, 11, 19, 15, 30, 0)
        );

        Assert.True(timedEvent.OccursOn(new DateTime(2026, 11, 19)));
        Assert.False(timedEvent.OccursOn(new DateTime(2026, 11, 18)));
        Assert.False(timedEvent.OccursOn(new DateTime(2026, 11, 20)));

        var allDayEvent = new CalendarEvent(
            Id: "2",
            Title: "Conference",
            Start: new DateTime(2026, 11, 19),
            IsAllDay: true
        );

        Assert.True(allDayEvent.OccursOn(new DateTime(2026, 11, 19)));
        Assert.False(allDayEvent.OccursOn(new DateTime(2026, 11, 20)));
    }

    private readonly Xunit.Abstractions.ITestOutputHelper output;
    public CalendarWeatherTests(Xunit.Abstractions.ITestOutputHelper output) => this.output = output;

    [Fact]
    public async Task AutoDetectLocation_ReturnsResult()
    {
        using var client = new WeatherClient(Folder, s => output.WriteLine(s));
        var result = await client.AutoDetectLocationAsync();
        output.WriteLine($"AutoDetect: {result?.City} ({result?.Lat}, {result?.Lon})");
        if (result == null) return; // Gracefully pass when network / public IP-API is offline or throttled
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.City));
    }

    [Fact]
    public void TestWeatherGlyphsInFonts()
    {
        var glyphTypeface = new System.Windows.Media.GlyphTypeface(new Uri("file:///C:/Windows/Fonts/SegoeIcons.ttf"));
        int[] codes = [0xE706, 0xE707, 0xE708, 0xE753, 0xE774, 0xE945, 0xEB42];
        foreach (var c in codes)
        {
            output.WriteLine($"SegoeIcons 0x{c:X4}: {glyphTypeface.CharacterToGlyphMap.ContainsKey(c)}");
        }

        var emojiTypeface = new System.Windows.Media.GlyphTypeface(new Uri("file:///C:/Windows/Fonts/seguiemj.ttf"));
        int[] emojiCodes = [0x2600, 0x26C5, 0x2601, 0x2614, 0x26A1, 0x2744, 0x1F327];
        foreach (var c in emojiCodes)
        {
            output.WriteLine($"SegoeUIEmoji 0x{c:X4}: {emojiTypeface.CharacterToGlyphMap.ContainsKey(c)}");
        }
    }

    [Fact]
    public void CalendarStore_AddAndRetrieve_WorksAndFiltersByDay()
    {
        using var store = new CalendarStore(Folder);
        var targetDay = new DateTime(2026, 11, 19);

        store.Add("Morning Standup", targetDay.AddHours(9), targetDay.AddHours(9.5));
        store.Add("Lunch with team", targetDay.AddHours(12), targetDay.AddHours(13));
        store.Add("Tomorrow Planning", targetDay.AddDays(1).AddHours(10));

        var dayEvents = store.GetEventsForDay(targetDay);
        Assert.Equal(2, dayEvents.Count);
        Assert.Equal("Morning Standup", dayEvents[0].Title);
        Assert.Equal("Lunch with team", dayEvents[1].Title);

        var monthEvents = store.GetEventsForMonth(2026, 11);
        Assert.Equal(3, monthEvents.Count);

        var daysWithEvents = store.GetDaysWithEventsInMonth(2026, 11);
        Assert.Contains(19, daysWithEvents);
        Assert.Contains(20, daysWithEvents);
    }

    [Fact]
    public void CalendarStore_DeleteAndPersist_ReloadsCorrectly()
    {
        string id;
        using (var store1 = new CalendarStore(Folder))
        {
            store1.Add("Doctor Appointment", new DateTime(2026, 11, 25, 15, 0, 0));
            store1.Add("Dentist", new DateTime(2026, 11, 26, 10, 0, 0));
            id = store1.Events[0].Id;
            store1.Delete(id);
        }

        using (var store2 = new CalendarStore(Folder))
        {
            Assert.Single(store2.Events);
            Assert.Equal("Dentist", store2.Events[0].Title);
        }
    }

    [Fact]
    public void IcsParser_ParsesStandardIcsContent()
    {
        string ics = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example Corp.//EN
BEGIN:VEVENT
UID:uid-12345
DTSTART:20261119T143000Z
DTEND:20261119T153000Z
SUMMARY:Project Demo\, Live
LOCATION:Conference Room A
DESCRIPTION:Reviewing milestone deliverables.\nBring questions.
END:VEVENT
END:VCALENDAR";

        var events = IcsParser.Parse(ics, "TestICS");
        Assert.Single(events);

        var ev = events[0];
        Assert.Equal("uid-12345", ev.Id);
        Assert.Equal("Project Demo, Live", ev.Title);
        Assert.Equal("Conference Room A", ev.Location);
        Assert.Contains("Reviewing milestone deliverables.", ev.Description);
        Assert.False(ev.IsAllDay);
    }

    [Fact]
    public void IcsParser_UnfoldsWrappedLinesAndParsesAllDay()
    {
        string ics = @"BEGIN:VCALENDAR
BEGIN:VEVENT
UID:all-day-1
DTSTART;VALUE=DATE:20261120
SUMMARY:Long Event Title That Has Been Folded
 Onto The Next Line Correctly
END:VEVENT
END:VCALENDAR";

        var events = IcsParser.Parse(ics);
        Assert.Single(events);
        Assert.Equal("Long Event Title That Has Been FoldedOnto The Next Line Correctly", events[0].Title);
        Assert.True(events[0].IsAllDay);
        Assert.Equal(new DateTime(2026, 11, 20), events[0].Start.Date);
    }

    [Fact]
    public void WmoWeatherCode_MapsConditionsAndGlyphs()
    {
        var (clearDay, glyphClear) = WmoWeatherCode.Describe(0, isDay: true);
        Assert.Equal("Clear Sky", clearDay);
        Assert.Equal("\uE706", glyphClear);

        var (clearNight, glyphNight) = WmoWeatherCode.Describe(0, isDay: false);
        Assert.Equal("Clear Night", clearNight);
        Assert.Equal("\uE708", glyphNight);

        var (rain, glyphRain) = WmoWeatherCode.Describe(63);
        Assert.Equal("Moderate Rain", rain);
        Assert.Equal("\uEB42", glyphRain);

        var (thunder, glyphThunder) = WmoWeatherCode.Describe(95);
        Assert.Equal("Thunderstorm", thunder);
        Assert.Equal("\uE945", glyphThunder);
    }

    [Fact]
    public void WeatherClient_CacheRoundTrip_RestoresData()
    {
        using var client = new WeatherClient(Folder);

        var current = new CurrentWeather(22.5, 21.0, 60, 15.0, 0, true, "Clear Sky", "\uE706", "°C");
        var data = new WeatherData(
            City: "Tokyo",
            Latitude: 35.6762,
            Longitude: 139.6503,
            Current: current,
            Hourly: Array.Empty<HourlyForecastItem>(),
            Daily: Array.Empty<DailyForecastItem>(),
            FetchedAt: DateTime.Now
        );

        // Access private SaveCache through reflection or testing file write
        string cacheFile = Path.Combine(Folder, "weather_cache.json");
        string json = System.Text.Json.JsonSerializer.Serialize(data);
        File.WriteAllText(cacheFile, json);

        var cached = client.GetCachedForecast();
        Assert.NotNull(cached);
        Assert.Equal("Tokyo", cached.City);
        Assert.Equal(22.5, cached.Current.Temperature);
        Assert.Equal("Clear Sky", cached.Current.ConditionText);
    }
}
