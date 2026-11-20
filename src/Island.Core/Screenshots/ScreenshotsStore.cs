using System;
using System.Collections.Generic;
using System.Linq;

namespace Island.Core.Screenshots;

public sealed class ScreenshotsStore
{
    private readonly object gate = new();
    private readonly List<ScreenshotItem> items = [];
    private readonly Action<string>? log;

    public event Action? HistoryChanged;

    public ScreenshotsStore(Action<string>? log = null)
    {
        this.log = log;
    }

    public IReadOnlyList<ScreenshotItem> GetItems()
    {
        lock (gate)
        {
            return items
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.Timestamp)
                .ToList();
        }
    }

    public bool Add(ScreenshotItem item, int maxItems = 100)
    {
        lock (gate)
        {
            // Deduplication: check if already present
            if (!string.IsNullOrEmpty(item.FilePath) && items.Any(x => string.Equals(x.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            items.Insert(0, item);

            // Trim oldest unpinned
            while (items.Count > maxItems)
            {
                int removeIdx = items.FindLastIndex(x => !x.IsPinned);
                if (removeIdx < 0) break;
                items.RemoveAt(removeIdx);
            }
        }

        HistoryChanged?.Invoke();
        return true;
    }

    public void TogglePin(string id)
    {
        lock (gate)
        {
            var idx = items.FindIndex(x => x.Id == id);
            if (idx >= 0)
            {
                items[idx] = items[idx] with { IsPinned = !items[idx].IsPinned };
            }
        }
        HistoryChanged?.Invoke();
    }

    public ScreenshotItem? Delete(string id)
    {
        ScreenshotItem? removed = null;
        lock (gate)
        {
            var idx = items.FindIndex(x => x.Id == id);
            if (idx >= 0)
            {
                removed = items[idx];
                items.RemoveAt(idx);
            }
        }
        if (removed != null) HistoryChanged?.Invoke();
        return removed;
    }

    public void ClearAll()
    {
        lock (gate)
        {
            items.RemoveAll(x => !x.IsPinned);
        }
        HistoryChanged?.Invoke();
    }
}
