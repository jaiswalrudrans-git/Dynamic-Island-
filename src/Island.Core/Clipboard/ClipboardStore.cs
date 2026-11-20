using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace Island.Core.Clipboard;

public sealed class ClipboardStore
{
    private const string FileName = "clipboard.dat";
    private readonly object gate = new();
    private readonly List<ClipboardItem> items = [];
    private readonly string? dataDirectory;
    private readonly Func<bool>? rememberAcrossRestarts;
    private readonly Func<int>? historySize;
    private readonly Func<int>? autoExpireHours;
    private readonly Func<bool>? filterSecrets;
    private readonly Func<IReadOnlyList<string>>? ignoredApps;
    private readonly Action<string>? log;
    private bool isPaused;

    public event Action? HistoryChanged;

    public ClipboardStore(
        string? dataDirectory = null,
        Func<bool>? rememberAcrossRestarts = null,
        Func<int>? historySize = null,
        Func<int>? autoExpireHours = null,
        Func<bool>? filterSecrets = null,
        Func<IReadOnlyList<string>>? ignoredApps = null,
        Action<string>? log = null)
    {
        this.dataDirectory = dataDirectory;
        this.rememberAcrossRestarts = rememberAcrossRestarts;
        this.historySize = historySize;
        this.autoExpireHours = autoExpireHours;
        this.filterSecrets = filterSecrets;
        this.ignoredApps = ignoredApps;
        this.log = log;

        if (!string.IsNullOrEmpty(dataDirectory) && (rememberAcrossRestarts?.Invoke() ?? false))
        {
            Load(dataDirectory);
        }
    }

    public ClipboardStore(Action<string>? log) : this(null, null, null, null, null, null, log)
    {
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(dataDirectory) && (rememberAcrossRestarts?.Invoke() ?? false))
        {
            Save(dataDirectory);
        }
    }

    public bool AddText(string text, string? sourceAppName = null)
    {
        if (filterSecrets?.Invoke() == true && ClipboardPrivacyFilter.LooksLikeSecret(text))
            return false;

        int limit = historySize?.Invoke() ?? 50;
        bool added = Add(new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            Text = text,
            SourceAppName = sourceAppName
        }, limit);

        if (added && (rememberAcrossRestarts?.Invoke() ?? false) && !string.IsNullOrEmpty(dataDirectory))
        {
            Save(dataDirectory);
        }
        return added;
    }

    public bool AddRichText(string text, string html, string? sourceAppName = null)
    {
        if (filterSecrets?.Invoke() == true && ClipboardPrivacyFilter.LooksLikeSecret(text))
            return false;

        int limit = historySize?.Invoke() ?? 50;
        bool added = Add(new ClipboardItem
        {
            Type = ClipboardItemType.RichText,
            Text = text,
            HtmlText = html,
            SourceAppName = sourceAppName
        }, limit);

        if (added && (rememberAcrossRestarts?.Invoke() ?? false) && !string.IsNullOrEmpty(dataDirectory))
        {
            Save(dataDirectory);
        }
        return added;
    }

    public bool AddImage(byte[] imageBytes, string? sourceAppName = null)
    {
        int limit = historySize?.Invoke() ?? 50;
        bool added = Add(new ClipboardItem
        {
            Type = ClipboardItemType.Image,
            ImageBytes = imageBytes,
            SourceAppName = sourceAppName
        }, limit);

        if (added && (rememberAcrossRestarts?.Invoke() ?? false) && !string.IsNullOrEmpty(dataDirectory))
        {
            Save(dataDirectory);
        }
        return added;
    }

    public bool AddFiles(IReadOnlyList<string> filePaths, string? sourceAppName = null)
    {
        int limit = historySize?.Invoke() ?? 50;
        bool added = Add(new ClipboardItem
        {
            Type = ClipboardItemType.Files,
            FilePaths = filePaths,
            SourceAppName = sourceAppName
        }, limit);

        if (added && (rememberAcrossRestarts?.Invoke() ?? false) && !string.IsNullOrEmpty(dataDirectory))
        {
            Save(dataDirectory);
        }
        return added;
    }

    public bool IsPaused
    {
        get { lock (gate) return isPaused; }
        set
        {
            lock (gate)
            {
                if (isPaused == value) return;
                isPaused = value;
            }
            HistoryChanged?.Invoke();
        }
    }

    public IReadOnlyList<ClipboardItem> GetItems()
    {
        lock (gate)
        {
            return items
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.Timestamp)
                .ToList();
        }
    }

    public bool Add(ClipboardItem item, int maxItems = 50)
    {
        lock (gate)
        {
            if (isPaused) return false;

            // Enforce maximum stored size: 2 MB text, 10 MB images
            if (item.Type is ClipboardItemType.Text or ClipboardItemType.RichText && (item.Text?.Length ?? 0) > 2_000_000)
                return false;
            if (item.Type == ClipboardItemType.Image && (item.ImageBytes?.Length ?? 0) > 10_000_000)
                return false;

            // Deduplication: check top (most recent) item
            var top = items.OrderByDescending(x => x.Timestamp).FirstOrDefault();
            if (top != null && IsEquivalent(top, item))
            {
                return false;
            }

            items.Insert(0, item);

            // Trim oldest unpinned items if exceeding maxItems
            TrimExcess(maxItems);
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

    public void Delete(string id)
    {
        lock (gate)
        {
            items.RemoveAll(x => x.Id == id);
        }
        HistoryChanged?.Invoke();
    }

    public void ClearAll()
    {
        lock (gate)
        {
            // Pinned items survive Clear all!
            items.RemoveAll(x => !x.IsPinned);
        }
        HistoryChanged?.Invoke();
    }

    public void PruneExpired(TimeSpan maxAge)
    {
        if (maxAge <= TimeSpan.Zero) return;
        DateTime cutoff = DateTime.UtcNow - maxAge;
        bool changed = false;

        lock (gate)
        {
            int removed = items.RemoveAll(x => !x.IsPinned && x.Timestamp < cutoff);
            changed = removed > 0;
        }

        if (changed) HistoryChanged?.Invoke();
    }

    public void Save(string dataDirectory)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            List<ClipboardItem> snapshot;
            lock (gate)
            {
                snapshot = items.ToList();
            }

            string path = Path.Combine(dataDirectory, FileName);
            if (snapshot.Count == 0)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }

            byte[] json = JsonSerializer.SerializeToUtf8Bytes(snapshot);
            byte[] encrypted = ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(path, encrypted);
        }
        catch (Exception ex)
        {
            log?.Invoke($"ClipboardStore save error: {ex.Message}");
        }
    }

    public void Load(string dataDirectory)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            string path = Path.Combine(dataDirectory, FileName);
            if (!File.Exists(path)) return;

            byte[] encrypted = File.ReadAllBytes(path);
            byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var loaded = JsonSerializer.Deserialize<List<ClipboardItem>>(decrypted);

            if (loaded != null)
            {
                lock (gate)
                {
                    items.Clear();
                    items.AddRange(loaded);
                }
                HistoryChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"ClipboardStore load error: {ex.Message}");
        }
    }

    private void TrimExcess(int maxItems)
    {
        int limit = Math.Clamp(maxItems, 10, 200);
        while (items.Count > limit)
        {
            int removeIdx = items.FindLastIndex(x => !x.IsPinned);
            if (removeIdx < 0) break; // all are pinned
            items.RemoveAt(removeIdx);
        }
    }

    private static bool IsEquivalent(ClipboardItem a, ClipboardItem b)
    {
        if (a.Type != b.Type) return false;
        return a.Type switch
        {
            ClipboardItemType.Text or ClipboardItemType.RichText => string.Equals(a.Text, b.Text, StringComparison.Ordinal),
            ClipboardItemType.Files when a.FilePaths != null && b.FilePaths != null => a.FilePaths.SequenceEqual(b.FilePaths, StringComparer.OrdinalIgnoreCase),
            ClipboardItemType.Image when a.ImageBytes != null && b.ImageBytes != null => a.ImageBytes.SequenceEqual(b.ImageBytes),
            _ => false
        };
    }
}
