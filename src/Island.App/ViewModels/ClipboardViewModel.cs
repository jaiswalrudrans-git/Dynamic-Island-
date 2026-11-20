using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using Island.Core.Clipboard;
using Island.Core.Settings;

namespace Island.App.ViewModels;

public sealed class ClipboardItemViewModel
{
    private readonly ClipboardItem item;

    public ClipboardItemViewModel(ClipboardItem item)
    {
        this.item = item;
    }

    public string Id => item.Id;
    public ClipboardItemType Type => item.Type;
    public string? Text => item.Text;
    public string? HtmlText => item.HtmlText;
    public byte[]? ImageBytes => item.ImageBytes;
    public IReadOnlyList<string>? FilePaths => item.FilePaths;
    public string? SourceAppName => item.SourceAppName;
    public bool IsPinned => item.IsPinned;
    public string PinGlyph => IsPinned ? "\uE718" : "\uE77A"; // Pin / Pin outline
    public string TypeGlyph => item.Type switch
    {
        ClipboardItemType.Image => "\uEB9F",
        ClipboardItemType.Files => "\uE838",
        ClipboardItemType.RichText => "\uE8D2",
        _ => "\uE8C8"
    };

    public string PreviewText => item.PreviewText;
    public bool HasImage => item.Type == ClipboardItemType.Image && item.ImageBytes != null && item.ImageBytes.Length > 0;
    public bool HasFiles => item.Type == ClipboardItemType.Files && item.FilePaths != null && item.FilePaths.Count > 0;

    public string RelativeTime
    {
        get
        {
            var diff = DateTime.UtcNow - item.Timestamp;
            if (diff.TotalSeconds < 60) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return item.Timestamp.ToLocalTime().ToString("MMM d");
        }
    }
}

public sealed class ClipboardViewModel : INotifyPropertyChanged
{
    private readonly Dispatcher dispatcher;
    private readonly ClipboardStore store;
    private readonly Func<AppSettings> settings;
    private readonly Action<ClipboardItem, bool> pasteHandler;
    private readonly Action<ClipboardItem> copyHandler;
    private string searchQuery = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ClipboardViewModel(
        Dispatcher dispatcher,
        ClipboardStore store,
        Func<AppSettings> settings,
        Action<ClipboardItem, bool> pasteHandler,
        Action<ClipboardItem> copyHandler)
    {
        this.dispatcher = dispatcher;
        this.store = store;
        this.settings = settings;
        this.pasteHandler = pasteHandler;
        this.copyHandler = copyHandler;

        store.HistoryChanged += OnHistoryChanged;
    }

    public string SearchQuery
    {
        get => searchQuery;
        set
        {
            if (searchQuery == value) return;
            searchQuery = value;
            OnPropertyChanged(nameof(SearchQuery));
            OnPropertyChanged(nameof(DisplayedItems));
        }
    }

    public bool IsPaused
    {
        get => store.IsPaused;
        set
        {
            if (store.IsPaused == value) return;
            store.IsPaused = value;
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(PauseStatusText));
        }
    }

    public string PauseStatusText => IsPaused ? "Capture Paused" : "Capturing";

    public int TotalItemsCount => store.GetItems().Count;
    public bool ShowSearchBox => TotalItemsCount > 8;

    public IReadOnlyList<ClipboardItemViewModel> DisplayedItems
    {
        get
        {
            var all = store.GetItems();
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string q = searchQuery.Trim();
                all = all.Where(x =>
                    (!string.IsNullOrEmpty(x.Text) && x.Text.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.SourceAppName) && x.SourceAppName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (x.FilePaths != null && x.FilePaths.Any(f => f.Contains(q, StringComparison.OrdinalIgnoreCase)))
                ).ToList();
            }

            return all.Select(x => new ClipboardItemViewModel(x)).ToList();
        }
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }

    public void ClearAll()
    {
        store.ClearAll();
    }

    public void TogglePin(string id)
    {
        store.TogglePin(id);
    }

    public void Delete(string id)
    {
        store.Delete(id);
    }

    public void Paste(string id, bool plainText = false)
    {
        var item = store.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            pasteHandler(item, plainText);
        }
    }

    public void Copy(string id)
    {
        var item = store.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            copyHandler(item);
        }
    }

    private void OnHistoryChanged()
    {
        if (dispatcher.CheckAccess())
        {
            Refresh();
        }
        else
        {
            dispatcher.InvokeAsync(Refresh);
        }
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(TotalItemsCount));
        OnPropertyChanged(nameof(ShowSearchBox));
        OnPropertyChanged(nameof(DisplayedItems));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(PauseStatusText));
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
