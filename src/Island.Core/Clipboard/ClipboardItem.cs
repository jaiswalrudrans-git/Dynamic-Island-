using System;
using System.Collections.Generic;

namespace Island.Core.Clipboard;

public sealed record ClipboardItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public ClipboardItemType Type { get; init; } = ClipboardItemType.Text;
    public string? Text { get; init; }
    public string? HtmlText { get; init; }
    public byte[]? ImageBytes { get; init; }
    public IReadOnlyList<string>? FilePaths { get; init; }
    public string? SourceAppName { get; init; }
    public string? SourceProcessName { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsPinned { get; init; }
    public long ByteSize { get; init; }

    public string PreviewText
    {
        get
        {
            if (Type == ClipboardItemType.Files && FilePaths != null && FilePaths.Count > 0)
            {
                return FilePaths.Count == 1
                    ? System.IO.Path.GetFileName(FilePaths[0])
                    : $"{FilePaths.Count} files: {string.Join(", ", FilePaths.Take(3).Select(System.IO.Path.GetFileName))}";
            }

            if (Type == ClipboardItemType.Image)
            {
                return $"Image ({FormatBytes(ByteSize)})";
            }

            if (!string.IsNullOrWhiteSpace(Text))
            {
                return Text.Trim();
            }

            return $"[{Type}]";
        }
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{((double)bytes / (1024 * 1024)).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} MB",
        >= 1024 => $"{((double)bytes / 1024).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} KB",
        _ => $"{bytes} B"
    };
}
