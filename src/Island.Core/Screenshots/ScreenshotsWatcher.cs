using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Island.Core.Screenshots;

public sealed class ScreenshotsWatcher : IDisposable
{
    private static readonly Guid FolderIdScreenshots = new("b7bede81-1724-4726-9925-824131dc7740");

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken);

    private readonly ScreenshotsStore store;
    private readonly string thumbsDirectory;
    private readonly Func<IReadOnlyList<string>>? watchedFolders;
    private readonly Func<bool>? includeClipboard;
    private readonly Action<string>? log;
    private readonly List<FileSystemWatcher> watchers = [];
    private bool disposed;

    public ScreenshotsWatcher(
        ScreenshotsStore store,
        string appDataDirectory,
        Func<IReadOnlyList<string>>? watchedFolders = null,
        Func<bool>? includeClipboard = null,
        Action<string>? log = null)
    {
        this.store = store;
        this.watchedFolders = watchedFolders;
        this.includeClipboard = includeClipboard;
        this.log = log;
        thumbsDirectory = Path.Combine(appDataDirectory, "thumbs");
        try { Directory.CreateDirectory(thumbsDirectory); } catch { }
    }

    public void ProcessClipboardSnip(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return;
        var (w, h) = ParsePngDimensions(bytes);
        AddClipboardScreenshot(bytes, w, h, false);
    }

    private static (int width, int height) ParsePngDimensions(byte[] bytes)
    {
        if (bytes.Length >= 24 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return (w, h);
        }
        return (0, 0);
    }

    public static string GetWindowsScreenshotsFolder()
    {
        try
        {
            string path = SHGetKnownFolderPath(FolderIdScreenshots, 0, IntPtr.Zero);
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                return path;
        }
        catch
        {
            // Fallback to Pictures\Screenshots
        }

        string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
        return fallback;
    }

    public void Start(IEnumerable<string>? extraFolders = null)
    {
        Stop();

        var foldersToWatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string defaultFolder = GetWindowsScreenshotsFolder();
        if (Directory.Exists(defaultFolder))
        {
            foldersToWatch.Add(defaultFolder);
        }

        var allExtras = extraFolders ?? watchedFolders?.Invoke();
        if (allExtras != null)
        {
            foreach (var f in allExtras)
            {
                if (!string.IsNullOrWhiteSpace(f) && Directory.Exists(f))
                    foldersToWatch.Add(f);
            }
        }

        foreach (var folder in foldersToWatch)
        {
            try
            {
                // Scan initial recent items (newest 30)
                ScanExistingFolder(folder);

                var watcher = new FileSystemWatcher(folder)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    Filter = "*.*",
                    EnableRaisingEvents = true
                };

                watcher.Created += (_, e) => OnFileCreated(e.FullPath);
                watcher.Renamed += (_, e) => OnFileCreated(e.FullPath);
                watchers.Add(watcher);
            }
            catch (Exception ex)
            {
                log?.Invoke($"ScreenshotsWatcher start error for {folder}: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        foreach (var w in watchers)
        {
            try
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            catch { }
        }
        watchers.Clear();
    }

    private void ScanExistingFolder(string folder)
    {
        try
        {
            var di = new DirectoryInfo(folder);
            var files = di.EnumerateFiles("*.*")
                .Where(f => IsImageExtension(f.Extension))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(40);

            foreach (var file in files)
            {
                ProcessImageFile(file.FullName, file.Length, file.LastWriteTimeUtc);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"ScanExistingFolder error: {ex.Message}");
        }
    }

    private void OnFileCreated(string filePath)
    {
        if (!IsImageExtension(Path.GetExtension(filePath))) return;

        // Give file time to finish writing
        _ = Task.Run(async () =>
        {
            await Task.Delay(400).ConfigureAwait(false);
            try
            {
                if (!File.Exists(filePath)) return;
                var fi = new FileInfo(filePath);
                ProcessImageFile(fi.FullName, fi.Length, fi.LastWriteTimeUtc);
            }
            catch (Exception ex)
            {
                log?.Invoke($"OnFileCreated error for {filePath}: {ex.Message}");
            }
        });
    }

    private void ProcessImageFile(string filePath, long sizeBytes, DateTime timestampUtc)
    {
        var item = new ScreenshotItem
        {
            FilePath = filePath,
            FileSizeBytes = sizeBytes,
            Timestamp = timestampUtc
        };

        store.Add(item);
    }

    public ScreenshotItem AddClipboardScreenshot(byte[] rawBytes, int width, int height, bool saveToFolder, string? customFolder = null)
    {
        string? savedPath = null;
        if (saveToFolder)
        {
            try
            {
                string targetDir = !string.IsNullOrWhiteSpace(customFolder) && Directory.Exists(customFolder)
                    ? customFolder
                    : GetWindowsScreenshotsFolder();
                Directory.CreateDirectory(targetDir);
                string filename = $"Screenshot {DateTime.Now:yyyy-MM-dd HHmmss}.png";
                savedPath = Path.Combine(targetDir, filename);
                File.WriteAllBytes(savedPath, rawBytes);
            }
            catch (Exception ex)
            {
                log?.Invoke($"AddClipboardScreenshot save error: {ex.Message}");
            }
        }

        var item = new ScreenshotItem
        {
            FilePath = savedPath,
            RawBytes = savedPath == null ? rawBytes : null,
            Width = width,
            Height = height,
            FileSizeBytes = rawBytes.Length,
            Timestamp = DateTime.UtcNow
        };

        store.Add(item);
        return item;
    }

    private static bool IsImageExtension(string? ext)
    {
        if (string.IsNullOrEmpty(ext)) return false;
        return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
    }
}
