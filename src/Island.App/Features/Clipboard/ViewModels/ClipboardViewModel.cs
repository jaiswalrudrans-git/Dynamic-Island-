using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Island.App.Features.Clipboard.Services;
using Island.Core.Clipboard;
using Island.Core.Screenshots;
using Island.Core.Settings;

namespace Island.App.Features.Clipboard.ViewModels;

public sealed class DroppedFileItem
{
    public string FullPath { get; }
    public string FileName { get; }
    public long FileSizeBytes { get; }
    public string SizeText { get; }
    public string IconGlyph { get; }

    public DroppedFileItem(string fullPath)
    {
        FullPath = fullPath;
        FileName = Path.GetFileName(fullPath);
        long bytes = 0;
        try
        {
            if (File.Exists(fullPath))
            {
                bytes = new FileInfo(fullPath).Length;
            }
        }
        catch { }

        FileSizeBytes = bytes;
        SizeText = FormatBytes(bytes);
        IconGlyph = DetermineIconGlyph(fullPath);
    }

    private static string DetermineIconGlyph(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" => "\uEB9F",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "\uF012",
            ".cs" or ".js" or ".ts" or ".py" or ".json" or ".xml" or ".html" or ".css" => "\uE943",
            ".pdf" or ".doc" or ".docx" or ".txt" or ".md" => "\uE8A5",
            _ => "\uE838"
        };
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{((double)bytes / (1024 * 1024)).ToString("0.0", CultureInfo.InvariantCulture)} MB",
        >= 1024 => $"{((double)bytes / 1024).ToString("0.0", CultureInfo.InvariantCulture)} KB",
        _ => $"{bytes} B"
    };
}

public sealed class ClipboardRowViewModel : INotifyPropertyChanged
{
    private readonly ClipboardItem? clipboardItem;
    private readonly ScreenshotItem? screenshotItem;
    private ImageSource? thumbnail;
    private ImageSource? largePreview;
    private bool thumbnailLoaded;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ClipboardRowViewModel(ClipboardItem item)
    {
        clipboardItem = item;
        Id = item.Id;
        Timestamp = item.Timestamp;
        IsPinned = item.IsPinned;
        IsImage = item.Type == ClipboardItemType.Image;
        Type = item.Type;

        if (IsImage)
        {
            int w = 0, h = 0;
            if (item.ImageBytes != null && item.ImageBytes.Length > 0)
            {
                (w, h) = ExtractDimensions(item.ImageBytes);
            }
            ImageWidth = w;
            ImageHeight = h;
            DisplayLabel = (w > 0 && h > 0) ? $"Picture {w}x{h}" : "Picture";
        }
        else
        {
            DisplayLabel = !string.IsNullOrWhiteSpace(item.Text)
                ? item.Text.Replace("\r", " ").Replace("\n", " ").Trim()
                : item.PreviewText;
        }

        SourceAppName = item.SourceAppName;
        FilePaths = item.FilePaths;
    }

    public ClipboardRowViewModel(ScreenshotItem item)
    {
        screenshotItem = item;
        Id = item.Id;
        Timestamp = item.Timestamp;
        IsPinned = item.IsPinned;
        IsImage = true;
        Type = ClipboardItemType.Image;
        ImageWidth = item.Width;
        ImageHeight = item.Height;

        DisplayLabel = (item.Width > 0 && item.Height > 0)
            ? $"Picture {item.Width}x{item.Height}"
            : (!string.IsNullOrEmpty(item.FilePath) ? Path.GetFileName(item.FilePath) : "Picture");

        SourceAppName = "Screenshots";
        FilePath = item.FilePath;
    }

    public string Id { get; }
    public DateTime Timestamp { get; }
    public bool IsPinned { get; set; }
    public bool IsImage { get; }
    public ClipboardItemType Type { get; }
    public int ImageWidth { get; }
    public int ImageHeight { get; }
    public string DisplayLabel { get; }
    public string? SourceAppName { get; }
    public string? FilePath { get; }
    public IReadOnlyList<string>? FilePaths { get; }
    public ClipboardItem? ClipboardItem => clipboardItem;
    public ScreenshotItem? ScreenshotItem => screenshotItem;

    public string PinGlyph => IsPinned ? "\uE735" : "\uE734"; // Solid star / Outline star
    public string TypeGlyph => Type switch
    {
        ClipboardItemType.Image => "\uEB9F",
        ClipboardItemType.Files => "\uE838",
        ClipboardItemType.RichText => "\uE8D2",
        _ => "\uE8C8"
    };

    public string RelativeTime
    {
        get
        {
            var diff = DateTime.UtcNow - Timestamp;
            if (diff.TotalSeconds < 60) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return Timestamp.ToLocalTime().ToString("MMM d");
        }
    }

    public ImageSource? Thumbnail
    {
        get
        {
            EnsureImagesLoaded();
            return thumbnail;
        }
    }

    public ImageSource? LargePreview
    {
        get
        {
            EnsureImagesLoaded();
            return largePreview;
        }
    }

    private void EnsureImagesLoaded()
    {
        if (thumbnailLoaded || !IsImage) return;
        thumbnailLoaded = true;

        if (screenshotItem != null)
        {
            thumbnail = CreateBitmap(screenshotItem.FilePath, screenshotItem.RawBytes, 56);
            largePreview = CreateBitmap(screenshotItem.FilePath, screenshotItem.RawBytes, 360);
        }
        else if (clipboardItem?.ImageBytes != null)
        {
            thumbnail = CreateBitmap(null, clipboardItem.ImageBytes, 56);
            largePreview = CreateBitmap(null, clipboardItem.ImageBytes, 360);
        }
    }

    private static ImageSource? CreateBitmap(string? path, byte[]? bytes, int decodeWidth)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.DecodePixelWidth = decodeWidth;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }

            if (bytes != null && bytes.Length > 0)
            {
                using var ms = new MemoryStream(bytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = ms;
                bmp.DecodePixelWidth = decodeWidth;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }
        catch { }
        return null;
    }

    private static (int w, int h) ExtractDimensions(byte[] bytes)
    {
        if (bytes.Length >= 24 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            if (w > 0 && h > 0) return (w, h);
        }

        try
        {
            using var ms = new MemoryStream(bytes);
            var frame = BitmapFrame.Create(ms, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return (0, 0);
        }
    }

    public void NotifyPinChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PinGlyph)));
    }
}

public sealed class ClipboardViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ObservableCollection<DroppedFileItem> droppedFiles = [];
    private readonly Dispatcher dispatcher;
    private readonly ClipboardStore clipboardStore;
    private readonly ScreenshotsStore screenshotsStore;
    private readonly Func<AppSettings> settings;
    private readonly Action<ClipboardItem, bool> pasteClipboardHandler;
    private readonly Action<ClipboardItem> copyClipboardHandler;
    private readonly Action<ScreenshotItem> pasteScreenshotHandler;
    private readonly Action<ScreenshotItem> copyScreenshotHandler;
    private readonly Action snipHandler;
    private readonly ClipboardListener? listener;

    private bool showFavoritesOnly;
    private bool isSearchOpen;
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
        Action snipHandler,
        bool enableListener = true)
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

        if (enableListener)
        {
            try
            {
                listener = new ClipboardListener(clipboardStore, () => IsPaused, _ => dispatcher.InvokeAsync(Refresh));
            }
            catch { }
        }
    }

    public bool ShowFavoritesOnly
    {
        get => showFavoritesOnly;
        set
        {
            if (showFavoritesOnly == value) return;
            showFavoritesOnly = value;
            OnPropertyChanged(nameof(ShowFavoritesOnly));
            OnPropertyChanged(nameof(DisplayedRows));
            OnPropertyChanged(nameof(HasClipboardItems));
            OnPropertyChanged(nameof(BothEmpty));
        }
    }

    public bool IsSearchOpen
    {
        get => isSearchOpen;
        set
        {
            if (isSearchOpen == value) return;
            isSearchOpen = value;
            if (!isSearchOpen && !string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = "";
                OnPropertyChanged(nameof(SearchQuery));
                OnPropertyChanged(nameof(DisplayedRows));
                OnPropertyChanged(nameof(HasClipboardItems));
                OnPropertyChanged(nameof(BothEmpty));
            }
            OnPropertyChanged(nameof(IsSearchOpen));
        }
    }

    public string SearchQuery
    {
        get => searchQuery;
        set
        {
            if (searchQuery == value) return;
            searchQuery = value;
            OnPropertyChanged(nameof(SearchQuery));
            OnPropertyChanged(nameof(DisplayedRows));
            OnPropertyChanged(nameof(HasClipboardItems));
            OnPropertyChanged(nameof(BothEmpty));
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
            OnPropertyChanged(nameof(PauseGlyph));
        }
    }

    public string PauseStatusText => IsPaused ? "Capture paused (click to resume)" : "Capture active (click to pause)";
    public string PauseGlyph => IsPaused ? "\uE768" : "\uE769"; // Play / Pause glyph

    public ObservableCollection<DroppedFileItem> DroppedFiles => droppedFiles;
    public bool HasDroppedFiles => DroppedFiles.Count > 0;

    public IReadOnlyList<ClipboardRowViewModel> DisplayedRows
    {
        get
        {
            var rows = new List<ClipboardRowViewModel>();

            // Ordinary clipboard items
            foreach (var item in clipboardStore.GetItems())
            {
                rows.Add(new ClipboardRowViewModel(item));
            }

            // Ordinary image rows from screenshots folder watcher
            if (settings().ScreenshotsIncludeClipboard || settings().ScreenshotsEnabled)
            {
                foreach (var sc in screenshotsStore.GetItems().Where(s => !string.IsNullOrEmpty(s.FilePath)))
                {
                    // Avoid duplicate if already in clipboard store
                    if (!rows.Any(r => r.IsImage && string.Equals(r.FilePath, sc.FilePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        rows.Add(new ClipboardRowViewModel(sc));
                    }
                }
            }

            // Order chronologically descending
            IEnumerable<ClipboardRowViewModel> query = rows.OrderByDescending(r => r.Timestamp);

            // Filter: All vs Favorites
            if (showFavoritesOnly)
            {
                query = query.Where(r => r.IsPinned);
            }

            // Filter: Search query
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string q = searchQuery.Trim();
                query = query.Where(r =>
                    (!string.IsNullOrEmpty(r.DisplayLabel) && r.DisplayLabel.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(r.SourceAppName) && r.SourceAppName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (r.FilePaths != null && r.FilePaths.Any(f => f.Contains(q, StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrEmpty(r.FilePath) && r.FilePath.Contains(q, StringComparison.OrdinalIgnoreCase)));
            }

            return query.ToList();
        }
    }

    public bool HasClipboardItems => DisplayedRows.Count > 0;
    public bool BothEmpty => !HasClipboardItems && !HasDroppedFiles;

    // Backward compatibility helpers for other consumers/tests
    public IReadOnlyList<ClipboardItemViewModel> DisplayedCopies =>
        clipboardStore.GetItems().Select(x => new ClipboardItemViewModel(x)).ToList();
    public IReadOnlyList<ScreenshotItemViewModel> DisplayedScreenshots =>
        screenshotsStore.GetItems().Select(x => new ScreenshotItemViewModel(x)).ToList();
    public int TotalCopiesCount => clipboardStore.GetItems().Count;
    public int TotalScreenshotsCount => screenshotsStore.GetItems().Count;
    public string CountsText => $"{TotalCopiesCount} items";

    public void TogglePause() => IsPaused = !IsPaused;
    public void ClearAll()
    {
        clipboardStore.ClearAll(); // Clears unpinned, keeps favorites
        Refresh();
    }

    public void ToggleFavorite(ClipboardRowViewModel row)
    {
        if (row.ClipboardItem != null)
        {
            clipboardStore.TogglePin(row.Id);
            row.IsPinned = !row.IsPinned;
            row.NotifyPinChanged();
        }
        else if (row.ScreenshotItem != null)
        {
            screenshotsStore.TogglePin(row.Id);
            row.IsPinned = !row.IsPinned;
            row.NotifyPinChanged();
        }
        Refresh();
    }

    public void DeleteRow(ClipboardRowViewModel row)
    {
        if (row.ClipboardItem != null)
        {
            clipboardStore.Delete(row.Id);
        }
        else if (row.ScreenshotItem != null)
        {
            screenshotsStore.Delete(row.Id);
        }
        Refresh();
    }

    public void CopyRow(ClipboardRowViewModel row)
    {
        ClipboardListener.IgnoreCurrentOrNextSequence();
        if (row.ClipboardItem != null)
        {
            copyClipboardHandler(row.ClipboardItem);
        }
        else if (row.ScreenshotItem != null)
        {
            copyScreenshotHandler(row.ScreenshotItem);
        }
    }

    public void PasteRow(ClipboardRowViewModel row, bool plainText = false)
    {
        ClipboardListener.IgnoreCurrentOrNextSequence();
        if (row.ClipboardItem != null)
        {
            pasteClipboardHandler(row.ClipboardItem, plainText);
        }
        else if (row.ScreenshotItem != null)
        {
            pasteScreenshotHandler(row.ScreenshotItem);
        }
    }

    public void OnRowClicked(ClipboardRowViewModel row)
    {
        if (string.Equals(settings().ClipboardClickAction, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            CopyRow(row);
        }
        else
        {
            PasteRow(row, plainText: false);
        }
    }

    // File transfer shelf methods
    public void AddDroppedFiles(IEnumerable<string> paths)
    {
        void Action()
        {
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                if (DroppedFiles.Any(f => string.Equals(f.FullPath, path, StringComparison.OrdinalIgnoreCase))) continue;

                if (File.Exists(path) || Directory.Exists(path))
                {
                    DroppedFiles.Add(new DroppedFileItem(path));
                }
            }
            OnPropertyChanged(nameof(HasDroppedFiles));
            OnPropertyChanged(nameof(BothEmpty));
        }

        if (dispatcher.CheckAccess()) Action();
        else dispatcher.Invoke(Action);
    }

    public void RemoveDroppedFile(DroppedFileItem file)
    {
        void Action()
        {
            DroppedFiles.Remove(file);
            OnPropertyChanged(nameof(HasDroppedFiles));
            OnPropertyChanged(nameof(BothEmpty));
        }

        if (dispatcher.CheckAccess()) Action();
        else dispatcher.Invoke(Action);
    }

    public void ClearDroppedFiles()
    {
        void Action()
        {
            DroppedFiles.Clear();
            OnPropertyChanged(nameof(HasDroppedFiles));
            OnPropertyChanged(nameof(BothEmpty));
        }

        if (dispatcher.CheckAccess()) Action();
        else dispatcher.Invoke(Action);
    }

    private void OnHistoryChanged()
    {
        if (dispatcher.CheckAccess()) Refresh();
        else dispatcher.InvokeAsync(Refresh);
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(DisplayedRows));
        OnPropertyChanged(nameof(HasClipboardItems));
        OnPropertyChanged(nameof(BothEmpty));
        OnPropertyChanged(nameof(TotalCopiesCount));
        OnPropertyChanged(nameof(TotalScreenshotsCount));
        OnPropertyChanged(nameof(CountsText));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(PauseStatusText));
        OnPropertyChanged(nameof(PauseGlyph));
    }

    public void Dispose()
    {
        listener?.Dispose();
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Retained for backward compatibility
public sealed class ClipboardItemViewModel
{
    private readonly ClipboardItem item;
    public ClipboardItemViewModel(ClipboardItem item) => this.item = item;
    public string Id => item.Id;
    public ClipboardItemType Type => item.Type;
    public string? Text => item.Text;
    public string? HtmlText => item.HtmlText;
    public byte[]? ImageBytes => item.ImageBytes;
    public IReadOnlyList<string>? FilePaths => item.FilePaths;
    public string? SourceAppName => item.SourceAppName;
    public bool IsPinned => item.IsPinned;
    public string PinGlyph => IsPinned ? "\uE735" : "\uE734";
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
