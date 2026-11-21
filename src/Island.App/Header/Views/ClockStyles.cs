using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Island.Core.Settings;

namespace Island.App.Header.Views;

public sealed partial class ClockFace
{
    private static readonly FontFamily Orbitron = new(new Uri("pack://application:,,,/Island.App;component/"), "./Assets/Fonts/#Orbitron, Segoe UI Variable Display, Segoe UI");
    private static readonly FontFamily InstrumentSerif = new(new Uri("pack://application:,,,/Island.App;component/"), "./Assets/Fonts/#Instrument Serif, Georgia, Times New Roman");
    private static readonly FontFamily Unbounded = new(new Uri("pack://application:,,,/Island.App;component/"), "./Assets/Fonts/#Unbounded, Trebuchet MS, Arial");
    private static readonly FontFamily BebasNeue = new(new Uri("pack://application:,,,/Island.App;component/"), "./Assets/Fonts/#Bebas Neue, Bahnschrift, Arial");
    private static readonly FontFamily Fredoka = new(new Uri("pack://application:,,,/Island.App;component/"), "./Assets/Fonts/#Fredoka, Century Gothic, Segoe UI");
    private static readonly FontFamily TerminalFont = new("Cascadia Code, Consolas, Courier New");
    private static readonly FontFamily StandardFont = new("Segoe UI Variable Display, Segoe UI");

    // 5x7 dot matrix definition for 0-9 and colon
    private static readonly Dictionary<char, string[]> DotMatrix = new()
    {
        ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
        ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        ['2'] = ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
        ['3'] = ["11111", "00010", "00100", "00010", "00001", "10001", "01110"],
        ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
        ['5'] = ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
        ['6'] = ["00110", "01000", "10000", "11110", "10001", "10001", "01110"],
        ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
        ['9'] = ["01110", "10001", "10001", "01111", "00001", "00010", "01100"],
        [':'] = ["0", "0", "1", "0", "1", "0", "0"]
    };

    // 7-segment display layout
    private static readonly Dictionary<char, string> SevenSegMap = new()
    {
        ['0'] = "abcdef",
        ['1'] = "bc",
        ['2'] = "abged",
        ['3'] = "abgcd",
        ['4'] = "fgbc",
        ['5'] = "afgcd",
        ['6'] = "afgedc",
        ['7'] = "abc",
        ['8'] = "abcdefg",
        ['9'] = "abcdfg"
    };

    public static Color ResolveClockColor(AppSettings settings, Color? coverColor = null) => settings.ClockColor switch
    {
        "Amber" => Color.FromRgb(0xFF, 0xB3, 0x00),
        "Cyan" => Color.FromRgb(0x00, 0xE5, 0xFF),
        "Pink" => Color.FromRgb(0xFF, 0x2E, 0x97),
        "Green" => Color.FromRgb(0x00, 0xE6, 0x76),
        "Violet" => Color.FromRgb(0xB3, 0x88, 0xFF),
        "Cover" => coverColor ?? Color.FromRgb(0xF5, 0xF5, 0xF7),
        "Custom" when !string.IsNullOrEmpty(settings.ClockCustomColor) => ParseColor(settings.ClockCustomColor),
        _ => Color.FromRgb(0xF5, 0xF5, 0xF7)
    };

    public Color ResolveColor() => ResolveClockColor(new AppSettings { ClockColor = colorName, ClockCustomColor = customColorHex }, coverColor);

    private static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Color.FromRgb(0xF5, 0xF5, 0xF7); }
    }

    public Brush ResolveBrush(double width)
    {
        if (colorName == "Gradient")
        {
            var stops = new GradientStopCollection
            {
                new(Color.FromRgb(0x00, 0xE5, 0xFF), 0.0),
                new(Color.FromRgb(0xFF, 0x2E, 0x97), 0.5),
                new(Color.FromRgb(0xFF, 0xB3, 0x00), 1.0)
            };
            var grad = new LinearGradientBrush(stops, new Point(0, 0), new Point(width, 0))
            {
                MappingMode = BrushMappingMode.Absolute
            };
            grad.Freeze();
            return grad;
        }

        var brush = new SolidColorBrush(ResolveColor());
        brush.Freeze();
        return brush;
    }

    private double DrawDotMatrix(DrawingContext? dc, string text, double x, double y, Brush brush)
    {
        double dotSize = 1.6 * sizeScale;
        double pitch = 2.2 * sizeScale;
        double charGap = 2.0 * sizeScale;
        double totalH = DefaultHeight * sizeScale;
        double matrixH = 6 * pitch + dotSize;
        double top = y + (totalH - matrixH) / 2.0;

        double curX = x;
        foreach (char ch in text)
        {
            if (DotMatrix.TryGetValue(ch, out var rows))
            {
                if (dc != null)
                {
                    for (int r = 0; r < rows.Length; r++)
                    {
                        for (int c = 0; c < rows[r].Length; c++)
                        {
                            if (rows[r][c] == '1')
                            {
                                dc.DrawEllipse(brush, null, new Point(curX + c * pitch + dotSize / 2, top + r * pitch + dotSize / 2), dotSize / 2, dotSize / 2);
                            }
                        }
                    }
                }
                curX += (rows[0].Length - 1) * pitch + dotSize + charGap;
            }
            else
            {
                curX += 4 * sizeScale;
            }
        }

        return curX - x;
    }

    private double DrawPixelMatrix(DrawingContext? dc, string text, double x, double y, Brush brush)
    {
        double blockSize = 1.8 * sizeScale;
        double charGap = 2.2 * sizeScale;
        double totalH = DefaultHeight * sizeScale;
        double matrixH = 7 * blockSize;
        double top = y + (totalH - matrixH) / 2.0;

        double curX = x;
        foreach (char ch in text)
        {
            if (DotMatrix.TryGetValue(ch, out var rows))
            {
                if (dc != null)
                {
                    for (int r = 0; r < rows.Length; r++)
                    {
                        for (int c = 0; c < rows[r].Length; c++)
                        {
                            if (rows[r][c] == '1')
                            {
                                dc.DrawRectangle(brush, null, new Rect(curX + c * blockSize, top + r * blockSize, blockSize, blockSize));
                            }
                        }
                    }
                }
                curX += rows[0].Length * blockSize + charGap;
            }
            else
            {
                curX += 4 * sizeScale;
            }
        }

        return curX - x;
    }

    private double DrawSevenSegment(DrawingContext? dc, string text, double x, double y, Brush brush)
    {
        double stroke = 1.4 * sizeScale;
        double segLen = 4.2 * sizeScale;
        double glyphW = segLen + stroke * 2;
        double glyphH = segLen * 2 + stroke * 3;
        double gap = 2.0 * sizeScale;
        double totalH = DefaultHeight * sizeScale;
        double top = y + (totalH - glyphH) / 2.0;

        double totalW = text.Sum(c => c == ':' || c == ' ' ? stroke * 2 + gap : glyphW + gap);
        if (dc == null) return totalW;

        dc.PushTransform(new SkewTransform(-7, 0, x + totalW / 2, y + totalH / 2));

        double curX = x;
        foreach (char ch in text)
        {
            if (ch == ':')
            {
                double dot = stroke * 1.2;
                dc.DrawRectangle(brush, null, new Rect(curX, top + glyphH * 0.3, dot, dot));
                dc.DrawRectangle(brush, null, new Rect(curX, top + glyphH * 0.7, dot, dot));
                curX += stroke * 2 + gap;
                continue;
            }
            if (ch == ' ')
            {
                curX += stroke * 2 + gap;
                continue;
            }

            if (SevenSegMap.TryGetValue(ch, out var activeSegs))
            {
                if (activeSegs.Contains('a')) dc.DrawRectangle(brush, null, new Rect(curX + stroke, top, segLen, stroke));
                if (activeSegs.Contains('b')) dc.DrawRectangle(brush, null, new Rect(curX + stroke + segLen, top + stroke, stroke, segLen));
                if (activeSegs.Contains('c')) dc.DrawRectangle(brush, null, new Rect(curX + stroke + segLen, top + stroke * 2 + segLen, stroke, segLen));
                if (activeSegs.Contains('d')) dc.DrawRectangle(brush, null, new Rect(curX + stroke, top + stroke * 2 + segLen * 2, segLen, stroke));
                if (activeSegs.Contains('e')) dc.DrawRectangle(brush, null, new Rect(curX, top + stroke * 2 + segLen, stroke, segLen));
                if (activeSegs.Contains('f')) dc.DrawRectangle(brush, null, new Rect(curX, top + stroke, stroke, segLen));
                if (activeSegs.Contains('g')) dc.DrawRectangle(brush, null, new Rect(curX + stroke, top + stroke + segLen, segLen, stroke));
            }

            curX += glyphW + gap;
        }

        dc.Pop();
        return totalW;
    }
}
