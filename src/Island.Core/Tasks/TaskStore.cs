using System.IO;
using System.Text.Json;

namespace Island.Core.Tasks;

public sealed class TaskStore : IDisposable
{
    private readonly string filePath;
    private readonly Action<string>? log;
    private readonly List<TaskItem> items = new();
    private readonly object gate = new();
    private readonly Timer reminderTimer;
    private readonly Func<DateTime> clock;
    private bool disposed;

    public IReadOnlyList<TaskItem> Items
    {
        get
        {
            lock (gate)
            {
                return items.OrderBy(t => t.Order).ToList();
            }
        }
    }

    public event Action<TaskItem>? ReminderFired;
    public event Action? ItemsChanged;

    public TaskStore(string directory, Action<string>? log = null, Func<DateTime>? clock = null)
    {
        this.log = log;
        this.clock = clock ?? (() => DateTime.Now);
        filePath = Path.Combine(directory, "tasks.json");
        Load();
        reminderTimer = new Timer(CheckReminders, null, 1000, 2000);
    }

    public void Add(string title, TaskPriority priority = TaskPriority.Medium, DateTime? dueDate = null, DateTime? reminderTime = null, ReminderRepeat repeat = ReminderRepeat.None)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        lock (gate)
        {
            int maxOrder = items.Count > 0 ? items.Max(t => t.Order) + 1 : 0;
            var item = new TaskItem(
                Id: Guid.NewGuid().ToString("N"),
                Title: title.Trim(),
                IsCompleted: false,
                Priority: priority,
                DueDate: dueDate,
                ReminderTime: reminderTime,
                Repeat: repeat,
                Order: maxOrder
            );
            items.Add(item);
            Save();
        }
        ItemsChanged?.Invoke();
    }

    public void ToggleComplete(string id)
    {
        lock (gate)
        {
            int idx = items.FindIndex(t => t.Id == id);
            if (idx < 0) return;
            var old = items[idx];
            items[idx] = old with { IsCompleted = !old.IsCompleted };
            Save();
        }
        ItemsChanged?.Invoke();
    }

    public void Delete(string id)
    {
        lock (gate)
        {
            int removed = items.RemoveAll(t => t.Id == id);
            if (removed > 0) Save();
        }
        ItemsChanged?.Invoke();
    }

    public void Move(string id, int delta)
    {
        lock (gate)
        {
            var ordered = items.OrderBy(t => t.Order).ToList();
            int idx = ordered.FindIndex(t => t.Id == id);
            if (idx < 0) return;

            int targetIdx = Math.Clamp(idx + delta, 0, ordered.Count - 1);
            if (targetIdx == idx) return;

            var item = ordered[idx];
            ordered.RemoveAt(idx);
            ordered.Insert(targetIdx, item);

            for (int i = 0; i < ordered.Count; i++)
            {
                int originalIdx = items.FindIndex(t => t.Id == ordered[i].Id);
                if (originalIdx >= 0) items[originalIdx] = items[originalIdx] with { Order = i };
            }
            Save();
        }
        ItemsChanged?.Invoke();
    }

    public void Snooze(string id, TimeSpan duration)
    {
        lock (gate)
        {
            int idx = items.FindIndex(t => t.Id == id);
            if (idx < 0) return;
            var old = items[idx];
            items[idx] = old with { ReminderTime = clock().Add(duration) };
            Save();
        }
        ItemsChanged?.Invoke();
    }

    private void CheckReminders(object? state)
    {
        List<TaskItem> dueReminders = new();
        lock (gate)
        {
            if (disposed) return;
            var now = clock();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.IsCompleted || !item.ReminderTime.HasValue || item.ReminderTime.Value > now)
                    continue;

                dueReminders.Add(item);

                DateTime? nextReminder = item.Repeat switch
                {
                    ReminderRepeat.Daily => item.ReminderTime.Value.AddDays(1),
                    ReminderRepeat.Weekdays => NextWeekday(item.ReminderTime.Value),
                    ReminderRepeat.Weekly => item.ReminderTime.Value.AddDays(7),
                    _ => null
                };

                items[i] = item with { ReminderTime = nextReminder };
            }

            if (dueReminders.Count > 0)
            {
                Save();
            }
        }

        foreach (var due in dueReminders)
        {
            ReminderFired?.Invoke(due);
        }

        if (dueReminders.Count > 0)
        {
            ItemsChanged?.Invoke();
        }
    }

    private static DateTime NextWeekday(DateTime from)
    {
        var next = from.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            next = next.AddDays(1);
        }
        return next;
    }

    private void Load()
    {
        lock (gate)
        {
            items.Clear();
            if (!File.Exists(filePath)) return;
            try
            {
                string json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<List<TaskItem>>(json);
                if (loaded != null)
                {
                    items.AddRange(loaded);
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Tasks load failed: {ex.Message}");
            }
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            string tmp = filePath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            log?.Invoke($"Tasks save failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            reminderTimer.Dispose();
        }
    }
}
