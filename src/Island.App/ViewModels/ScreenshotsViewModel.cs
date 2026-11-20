using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Island.Core.Screenshots;
using Island.Core.Settings;

namespace Island.App.ViewModels;

public sealed class ScreenshotItemViewModel
{
    private readonly ScreenshotItem item;

    public ScreenshotItemViewModel(ScreenshotItem item)
    {
        this.item = item;
    }

    public string Id => item.Id;
    public string? FilePath => item.FilePath;
    public byte[]? RawBytes => item.RawBytes;
    public int Width => item.Width;
    public int Height => item.Height;
    public long FileSizeBytes => item.FileSizeBytes;
    public DateTime Timestamp => item.Timestamp;
    public bool IsPinned => item.IsPinned;
    public string PinGlyph => IsPinned ? "\uE718" : "\uE77A";
    public string DimensionsText => item.DimensionsText;
    public string SizeText => item.SizeText;
    public string RelativeTime => item.RelativeTime;
    public string DisplayTitle => !string.IsNullOrEmpty(item.FilePath)
        ? Path.GetFileName(item.FilePath)
        : $"Snip ({item.DimensionsText})";
}

public sealed class ScreenshotsViewModel : INotifyPropertyChanged
{
    private readonly Dispatcher dispatcher;
    private readonly ScreenshotsStore store;
    private readonly Func<AppSettings> settings;
    private readonly Action<ScreenshotItem> pasteHandler;
    private readonly Action<ScreenshotItem> copyHandler;
    private readonly Action snipHandler;
    private ScreenshotItemViewModel? previewItem;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ScreenshotsViewModel(
        Dispatcher dispatcher,
        ScreenshotsStore store,
        Func<AppSettings> settings,
        Action<ScreenshotItem> pasteHandler,
        Action<ScreenshotItem> copyHandler,
        Action snipHandler)
    {
        this.dispatcher = dispatcher;
        this.store = store;
        this.settings = settings;
        this.pasteHandler = pasteHandler;
        this.copyHandler = copyHandler;
        this.snipHandler = snipHandler;

        store.HistoryChanged += OnHistoryChanged;
    }

    public int TotalCount => store.GetItems().Count;

    public IReadOnlyList<ScreenshotItemViewModel> DisplayedItems
    {
        get
        {
            return store.GetItems()
                .Select(x => new ScreenshotItemViewModel(x))
                .ToList();
        }
    }

    public ScreenshotItemViewModel? PreviewItem
    {
        get => previewItem;
        set
        {
            if (previewItem == value) return;
            previewItem = value;
            OnPropertyChanged(nameof(PreviewItem));
            OnPropertyChanged(nameof(HasPreview));
        }
    }

    public bool HasPreview => previewItem != null;

    public void Snip() => snipHandler();

    public void Paste(string id)
    {
        var item = store.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null) pasteHandler(item);
    }

    public void Copy(string id)
    {
        var item = store.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null) copyHandler(item);
    }

    public void TogglePin(string id) => store.TogglePin(id);

    public void Delete(string id)
    {
        var deleted = store.Delete(id);
        if (deleted != null && !string.IsNullOrEmpty(deleted.FilePath) && File.Exists(deleted.FilePath))
        {
            try
            {
                // Move to Recycle Bin
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    deleted.FilePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
            catch
            {
                try { File.Delete(deleted.FilePath); } catch { }
            }
        }
    }

    public void Open(string id)
    {
        var item = store.GetItems().FirstOrDefault(x => x.Id == id);
        if (item != null && !string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
            catch { }
        }
    }

    public void ShowInFolder(string id)
    {
        var item = store.GetItems().FirstOrDefault(x => x.Id == id);
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
        if (dispatcher.CheckAccess()) Refresh();
        else dispatcher.InvokeAsync(Refresh);
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(DisplayedItems));
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
