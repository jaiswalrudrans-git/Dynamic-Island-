using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Island.Core.Clipboard;
using Island.Core.Screenshots;
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
    private readonly ClipboardStore clipboardStore;
    private readonly ScreenshotsStore screenshotsStore;
    private readonly Func<AppSettings> settings;
    private readonly Action<ClipboardItem, bool> pasteClipboardHandler;
    private readonly Action<ClipboardItem> copyClipboardHandler;
    private readonly Action<ScreenshotItem> pasteScreenshotHandler;
    private readonly Action<ScreenshotItem> copyScreenshotHandler;
    private readonly Action snipHandler;
    private string searchQuery = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ClipboardViewModel(
        Dispatcher dispatcher,
        ClipboardStore clipboardStore,
        ScreenshotsStore screenshotsStore,
        Func<AppSettings> settings,
        Action<ClipboardItem, bool> pasteClipboardHandler,
        Action<ClipboardItem> copyClipboardHandler,
        Action<ScreenshotItem> pasteScreenshotHandler,
        Action<ScreenshotItem> copyScreenshotHandler,
        Action snipHandler)
    {
        this.dispatcher = dispatcher;
        this.clipboardStore = clipboardStore;
        this.screenshotsStore = screenshotsStore;
        this.settings = settings;
        this.pasteClipboardHandler = pasteClipboardHandler;
        this.copyClipboardHandler = copyClipboardHandler;
        this.pasteScreenshotHandler = pasteScreenshotHandler;
        this.copyScreenshotHandler = copyScreenshotHandler;
        this.snipHandler = snipHandler;

        clipboardStore.HistoryChanged += OnHistoryChanged;
        screenshotsStore.HistoryChanged += OnHistoryChanged;
    }

    public string SearchQuery
    {
        get => searchQuery;
        set
        {
            if (searchQuery == value) return;
            searchQuery = value;
            OnPropertyChanged(nameof(SearchQuery));
            OnPropertyChanged(nameof(DisplayedCopies));
            OnPropertyChanged(nameof(DisplayedScreenshots));
            OnPropertyChanged(nameof(DisplayedItems)); // Backward compat
            OnPropertyChanged(nameof(HasCopies));
            OnPropertyChanged(nameof(HasScreenshots));
            OnPropertyChanged(nameof(BothEmpty));
            OnPropertyChanged(nameof(HasBoth));
            OnPropertyChanged(nameof(OnlyCopies));
            OnPropertyChanged(nameof(OnlyScreenshots));
        }
    }

    public bool IsPaused
    {
        get => clipboardStore.IsPaused;
        set
        {
            if (clipboardStore.IsPaused == value) return;
            clipboardStore.IsPaused = value;
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(PauseStatusText));
        }
    }

    public string PauseStatusText => IsPaused ? "Paused" : "Capture Active";

    public int TotalCopiesCount => clipboardStore.GetItems().Count;
    public int TotalScreenshotsCount => screenshotsStore.GetItems().Count;
    public int TotalItemsCount => TotalCopiesCount + TotalScreenshotsCount;
    public string CountsText => $"{TotalCopiesCount} copied · {TotalScreenshotsCount} screenshots";

    public IReadOnlyList<ClipboardItemViewModel> DisplayedCopies
    {
        get
        {
            var all = clipboardStore.GetItems();
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

    public IReadOnlyList<ClipboardItemViewModel> DisplayedItems => DisplayedCopies;

    public IReadOnlyList<ScreenshotItemViewModel> DisplayedScreenshots
    {
        get
        {
            var all = screenshotsStore.GetItems();
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string q = searchQuery.Trim();
                all = all.Where(x =>
                    (!string.IsNullOrEmpty(x.FilePath) && Path.GetFileName(x.FilePath).Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    x.DimensionsText.Contains(q, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            return all.Select(x => new ScreenshotItemViewModel(x)).ToList();
        }
    }

    public bool HasCopies => DisplayedCopies.Count > 0;
    public bool HasScreenshots => DisplayedScreenshots.Count > 0;
    public bool BothEmpty => !HasCopies && !HasScreenshots;
    public bool HasBoth => HasCopies && HasScreenshots;
    public bool OnlyCopies => HasCopies && !HasScreenshots;
    public bool OnlyScreenshots => !HasCopies && HasScreenshots;

    public void TogglePause() => IsPaused = !IsPaused;
    public void ClearAll() => clipboardStore.ClearAll();
    public void TriggerSnip() => snipHandler();

    // Clipboard Item Actions
    public void TogglePin(string id) => clipboardStore.TogglePin(id);
    public void Delete(string id) => clipboardStore.Delete(id);

    public void Paste(string id, bool plainText = false)
    {
        var item = clipboardStore.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null) pasteClipboardHandler(item, plainText);
    }

    public void Copy(string id)
    {
        var item = clipboardStore.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null) copyClipboardHandler(item);
    }

    // Screenshot Item Actions
    public void TogglePinScreenshot(string id) => screenshotsStore.TogglePin(id);
    public void DeleteScreenshot(string id) => screenshotsStore.Delete(id);

    public void PasteScreenshot(string id)
    {
        var item = screenshotsStore.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null) pasteScreenshotHandler(item);
    }

    public void CopyScreenshot(string id)
    {
        var item = screenshotsStore.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null) copyScreenshotHandler(item);
    }

    public void OpenScreenshot(string id)
    {
        var item = screenshotsStore.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null && !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
            catch { }
        }
    }

    public void ShowScreenshotInFolder(string id)
    {
        var item = screenshotsStore.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null && !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{item.FilePath}\"");
            }
            catch { }
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
        OnPropertyChanged(nameof(TotalCopiesCount));
        OnPropertyChanged(nameof(TotalScreenshotsCount));
        OnPropertyChanged(nameof(TotalItemsCount));
        OnPropertyChanged(nameof(CountsText));
        OnPropertyChanged(nameof(DisplayedCopies));
        OnPropertyChanged(nameof(DisplayedScreenshots));
        OnPropertyChanged(nameof(DisplayedItems));
        OnPropertyChanged(nameof(HasCopies));
        OnPropertyChanged(nameof(HasScreenshots));
        OnPropertyChanged(nameof(BothEmpty));
        OnPropertyChanged(nameof(HasBoth));
        OnPropertyChanged(nameof(OnlyCopies));
        OnPropertyChanged(nameof(OnlyScreenshots));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(PauseStatusText));
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
