using System.Globalization;
using System.Text.RegularExpressions;

namespace Island.Core.Music;

public sealed record LyricLine(double Seconds, string Text);

public sealed partial class SyncedLyrics
{
    public IReadOnlyList<LyricLine> Lines { get; }
    private SyncedLyrics(List<LyricLine> lines) => Lines = lines;

    [GeneratedRegex(@"\[(\d{1,4}):([0-5]\d(?:\.\d{1,3})?)\]")]
    private static partial Regex Timestamp();
    [GeneratedRegex(@"(?im)^\[offset:\s*([+-]?\d+)\]")]
    private static partial Regex Offset();

    public static SyncedLyrics Parse(string? lrc)
    {
        lrc ??= "";
        int.TryParse(Offset().Match(lrc).Groups[1].Value, CultureInfo.InvariantCulture, out int offsetMs);
        var lines = new List<LyricLine>();
        foreach (string row in lrc.Split('\n'))
        {
            var matches = Timestamp().Matches(row);
            if (matches.Count == 0) continue;
            var last = matches[^1];
            string words = row[(last.Index + last.Length)..].Trim();
            foreach (Match tag in matches)
            {
                double time = int.Parse(tag.Groups[1].Value, CultureInfo.InvariantCulture) * 60
                    + double.Parse(tag.Groups[2].Value, CultureInfo.InvariantCulture) - offsetMs / 1000d;
                lines.Add(new(Math.Max(0, time), words));
            }
        }
        return new(lines.OrderBy(line => line.Seconds).ToList());
    }

    public int At(double seconds)
    {
        int lo = 0, hi = Lines.Count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (Lines[mid].Seconds <= seconds) lo = mid + 1;
            else hi = mid;
        }
        return lo - 1;
    }
}
