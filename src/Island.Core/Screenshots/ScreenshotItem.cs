using System;

namespace Island.Core.Screenshots;

public sealed record ScreenshotItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? FilePath { get; init; }
    public string? ThumbnailPath { get; init; }
    public byte[]? RawBytes { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSizeBytes { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsPinned { get; init; }
    public bool IsClipboardOnly => string.IsNullOrEmpty(FilePath);

    public string DimensionsText => Width > 0 && Height > 0 ? $"{Width} × {Height}" : "";

    public string SizeText => FileSizeBytes switch
    {
        >= 1024 * 1024 => $"{((double)FileSizeBytes / (1024 * 1024)).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} MB",
        >= 1024 => $"{((double)FileSizeBytes / 1024).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} KB",
        _ => $"{FileSizeBytes} B"
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
}
