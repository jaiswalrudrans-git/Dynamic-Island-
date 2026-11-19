using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Island.Core.Calendar;

public sealed class CalendarStore : IDisposable
{
    private static readonly HttpClient HttpClient = new();
    private readonly string filePath;
    private readonly Action<string>? log;
    private readonly List<CalendarEvent> events = new();
    private readonly object gate = new();
    private readonly Timer reminderTimer;
    private readonly Func<DateTime> clock;
    private bool disposed;

    public IReadOnlyList<CalendarEvent> Events
    {
        get
        {
            lock (gate) return events.OrderBy(e => e.Start).ToList();
        }
    }

    public event Action? EventsChanged;
    public event Action<CalendarEvent>? ReminderFired;

    public CalendarStore(string directory, Action<string>? log = null, Func<DateTime>? clock = null)
    {
        this.log = log;
        this.clock = clock ?? (() => DateTime.Now);
        filePath = Path.Combine(directory, "calendar.json");
        Load();
        reminderTimer = new Timer(CheckReminders, null, 1000, 30000); // Every 30 seconds
    }

    public CalendarEvent? Add(string title, DateTime start, DateTime? end = null, bool isAllDay = false,
        string? location = null, string? description = null, int? reminderMinutes = 15)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        CalendarEvent item;
        lock (gate)
        {
            item = new CalendarEvent(
                Id: Guid.NewGuid().ToString("N"),
                Title: title.Trim(),
                Start: start,
                End: end,
                IsAllDay: isAllDay,
                Location: location?.Trim(),
                Description: description?.Trim(),
                Source: "Local",
                ReminderMinutes: reminderMinutes,
                HasFiredReminder: false
            );
            events.Add(item);
            Save();
        }
        EventsChanged?.Invoke();
        return item;
    }

    public void Delete(string id)
    {
        lock (gate)
        {
            int removed = events.RemoveAll(e => e.Id == id);
            if (removed > 0) Save();
        }
        EventsChanged?.Invoke();
    }

    public IReadOnlyList<CalendarEvent> GetEventsForDay(DateTime day)
    {
        lock (gate)
        {
            return events.Where(e => e.OccursOn(day)).OrderBy(e => e.Start).ToList();
        }
    }

    public IReadOnlyList<CalendarEvent> GetEventsForMonth(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        lock (gate)
        {
            return events.Where(e => e.Start < end && e.EffectiveEnd >= start).OrderBy(e => e.Start).ToList();
        }
    }

    public HashSet<int> GetDaysWithEventsInMonth(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var set = new HashSet<int>();
        lock (gate)
        {
            foreach (var ev in events)
            {
                for (int d = 1; d <= daysInMonth; d++)
                {
                    var date = new DateTime(year, month, d);
                    if (ev.OccursOn(date))
                    {
                        set.Add(d);
                    }
                }
            }
        }
        return set;
    }

    public int ImportIcsText(string icsText, string sourceName = "ICS")
    {
        var parsed = IcsParser.Parse(icsText, sourceName);
        if (parsed.Count == 0) return 0;

        int added = 0;
        lock (gate)
        {
            foreach (var item in parsed)
            {
                int existing = events.FindIndex(e => e.Id == item.Id || (e.Title == item.Title && e.Start == item.Start));
                if (existing >= 0)
                {
                    events[existing] = item;
                }
                else
                {
                    events.Add(item);
                    added++;
                }
            }
            Save();
        }
        EventsChanged?.Invoke();
        return added;
    }

    public int ImportIcsFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return 0;
            string text = File.ReadAllText(path);
            return ImportIcsText(text, Path.GetFileNameWithoutExtension(path));
        }
        catch (Exception ex)
        {
            log?.Invoke($"ICS file import error: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> ImportIcsUrlAsync(string url)
    {
        try
        {
            if (url.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url[9..];
            }
            string text = await HttpClient.GetStringAsync(url);
            return ImportIcsText(text, "Remote Calendar");
        }
        catch (Exception ex)
        {
            log?.Invoke($"ICS URL import error: {ex.Message}");
            return 0;
        }
    }

    private void CheckReminders(object? state)
    {
        var now = clock();
        List<CalendarEvent> due = new();

        lock (gate)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                if (ev.HasFiredReminder || !ev.ReminderMinutes.HasValue) continue;

                var reminderTime = ev.Start.AddMinutes(-ev.ReminderMinutes.Value);
                if (now >= reminderTime && now < ev.Start.AddMinutes(30))
                {
                    events[i] = ev with { HasFiredReminder = true };
                    due.Add(events[i]);
                }
            }
            if (due.Count > 0) Save();
        }

        foreach (var ev in due)
        {
            ReminderFired?.Invoke(ev);
        }
    }

    private void Load()
    {
        lock (gate)
        {
            events.Clear();
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var items = JsonSerializer.Deserialize<List<CalendarEvent>>(json);
                if (items != null) events.AddRange(items);
            }
            catch (Exception ex)
            {
                log?.Invoke($"Failed to load calendar events: {ex.Message}");
            }
        }
    }

    private void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(dir);
            string tmp = filePath + ".tmp";
            string json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tmp, json);
            File.Move(tmp, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            log?.Invoke($"Failed to save calendar events: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        reminderTimer.Dispose();
    }
}
