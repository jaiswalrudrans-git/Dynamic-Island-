using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Island.App.Services;

internal static class ProcessIconCache
{
    private static readonly ConcurrentDictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? GetIcon(string processName, int pid)
    {
        if (string.IsNullOrWhiteSpace(processName)) return null;

        if (Cache.TryGetValue(processName, out var cached))
            return cached;

        ImageSource? icon = ExtractIcon(pid, processName);
        Cache[processName] = icon;
        return icon;
    }

    private static ImageSource? ExtractIcon(int pid, string processName)
    {
        try
        {
            string? filePath = null;
            if (pid > 0)
            {
                try
                {
                    using var proc = Process.GetProcessById(pid);
                    filePath = proc.MainModule?.FileName;
                }
                catch
                {
                    // Access denied or architecture mismatch
                }
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                string sys32Path = Path.Combine(Environment.SystemDirectory, processName + ".exe");
                if (File.Exists(sys32Path))
                {
                    filePath = sys32Path;
                }
            }

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                using var sysIcon = Icon.ExtractAssociatedIcon(filePath);
                if (sysIcon != null)
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        sysIcon.Handle,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(16, 16));
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
            }
        }
        catch
        {
            // Silently fallback on GDI/Win32 errors
        }

        return null;
    }
}
