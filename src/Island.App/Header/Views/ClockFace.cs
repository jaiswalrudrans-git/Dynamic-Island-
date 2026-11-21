using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Island.Core.Settings;

namespace Island.App.Header.Views;

public sealed partial class ClockFace : FrameworkElement
{
    public const double DefaultHeight = 24.0;

    private DateTime currentTime = DateTime.Now;
    private string style = "Simple";
    private string colorName = "White";
    private string customColorHex = "#00E5FF";
    private Color? coverColor;
    private bool use24Hour = true;
    private bool showSeconds;
    private bool blinkingColon;
    private bool leadingZero = true;
    private double sizeScale = 1.0;
    private double neonIntensity = 0.6;
    private DropShadowEffect? neonEffect;

    public ClockFace()
    {
        IsHitTestVisible = false;
        UpdateGlowEffect();
    }

    public DateTime Time
    {
        get => currentTime;
        set
        {
            if (currentTime == value) return;
            currentTime = value;
            InvalidateVisual();
        }
    }

    public string ClockStyleProp
    {
        get => style;
        set
        {
            if (style == value) return;
            style = value;
            UpdateGlowEffect();
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    public string ClockColorProp
    {
        get => colorName;
        set
        {
            if (colorName == value) return;
            colorName = value;
            UpdateGlowEffect();
            InvalidateVisual();
        }
    }

    public string CustomColorHex
    {
        get => customColorHex;
        set
        {
            if (customColorHex == value) return;
            customColorHex = value;
            UpdateGlowEffect();
            InvalidateVisual();
        }
    }

    public Color? CoverColor
    {
        get => coverColor;
        set
        {
            if (coverColor == value) return;
            coverColor = value;
            InvalidateVisual();
        }
    }

    public bool Use24Hour
    {
        get => use24Hour;
        set
        {
            if (use24Hour == value) return;
            use24Hour = value;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    public bool ShowSecondsProp
    {
        get => showSeconds;
        set
        {
            if (showSeconds == value) return;
            showSeconds = value;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    public bool BlinkingColon
    {
        get => blinkingColon;
        set
        {
            if (blinkingColon == value) return;
            blinkingColon = value;
            InvalidateVisual();
        }
    }

    public bool LeadingZero
    {
        get => leadingZero;
        set
        {
            if (leadingZero == value) return;
            leadingZero = value;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    public double SizeScale
    {
        get => sizeScale;
        set
        {
            double next = Math.Clamp(value, 0.5, 3.0);
            if (Math.Abs(sizeScale - next) < 0.001) return;
            sizeScale = next;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    public double NeonIntensity
    {
        get => neonIntensity;
        set
        {
            neonIntensity = Math.Clamp(value, 0.0, 1.0);
            UpdateGlowEffect();
            InvalidateVisual();
        }
    }

    public void ApplySettings(AppSettings settings, Color? cover = null)
    {
        style = settings.ClockStyle;
        colorName = settings.ClockColor;
        customColorHex = settings.ClockCustomColor;
        use24Hour = settings.Use24HourClock;
        showSeconds = settings.ShowSeconds;
        blinkingColon = settings.ClockBlinkingColon;
        leadingZero = settings.ClockLeadingZero;
        sizeScale = settings.ClockSizeScale;
        neonIntensity = settings.NeonIntensity;
        coverColor = cover;
        UpdateGlowEffect();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void UpdateGlowEffect()
    {
        if (style is "Neon" or "Segment" && neonIntensity > 0.05)
        {
            Color glow = ResolveColor();
            neonEffect = new DropShadowEffect
            {
                Color = glow,
                ShadowDepth = 0,
                BlurRadius = (style == "Neon" ? 8 : 4) * sizeScale * neonIntensity,
                Opacity = (style == "Neon" ? 0.85 : 0.6) * neonIntensity
            };
            Effect = neonEffect;
        }
        else
        {
            Effect = null;
            neonEffect = null;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = MeasureWidth();
        double h = DefaultHeight * sizeScale;
        return new Size(Math.Ceiling(w), Math.Ceiling(h));
    }

    private double MeasureWidth()
    {
        return LayoutClock(null, 0, 0, Brushes.White);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double measured = MeasureWidth();
        double targetW = Math.Max(ActualWidth, measured);
        double x = (targetW - measured) / 2.0;
        double y = (ActualHeight - DefaultHeight * sizeScale) / 2.0;

        Brush brush = ResolveBrush(measured);
        LayoutClock(dc, x, y, brush);
    }

    private double LayoutClock(DrawingContext? dc, double x, double y, Brush brush)
    {
        int hourVal = currentTime.Hour;
        if (!use24Hour)
        {
            hourVal %= 12;
            if (hourVal == 0) hourVal = 12;
        }

        string hourStr = leadingZero ? hourVal.ToString("00", CultureInfo.InvariantCulture) : hourVal.ToString(CultureInfo.InvariantCulture);
        string minStr = currentTime.ToString("mm", CultureInfo.InvariantCulture);
        string secStr = currentTime.ToString("ss", CultureInfo.InvariantCulture);

        bool colonVisible = !blinkingColon || (currentTime.Second % 2 == 0);
        string colon = colonVisible ? ":" : " ";
        string ampm = use24Hour ? "" : (currentTime.Hour < 12 ? " AM" : " PM");

        string timeText = showSeconds ? $"{hourStr}{colon}{minStr}:{secStr}" : $"{hourStr}{colon}{minStr}";

        return style switch
        {
            "DotMatrix" => DrawDotMatrix(dc, timeText + ampm, x, y, brush),
            "Pixel" => DrawPixelMatrix(dc, timeText + ampm, x, y, brush),
            "Segment" => DrawSevenSegment(dc, timeText + ampm, x, y, brush),
            "Stacked" => DrawStacked(dc, hourStr, minStr, secStr, ampm, x, y, brush),
            "WithDate" => DrawWithDate(dc, timeText, ampm, x, y, brush),
            "Neon" => DrawText(dc, timeText + ampm, Orbitron, FontWeights.SemiBold, FontStyles.Normal, 14 * sizeScale, x, y, brush),
            "Serif" => DrawText(dc, timeText + ampm, InstrumentSerif, FontWeights.Normal, FontStyles.Italic, 20 * sizeScale, x, y, brush),
            "Wide" => DrawText(dc, timeText + ampm, Unbounded, FontWeights.SemiBold, FontStyles.Normal, 12 * sizeScale, x, y, brush),
            "Condensed" => DrawText(dc, timeText + ampm, BebasNeue, FontWeights.Normal, FontStyles.Normal, 22 * sizeScale, x, y, brush),
            "Compact" => DrawText(dc, timeText + ampm, Fredoka, FontWeights.SemiBold, FontStyles.Normal, 14 * sizeScale, x, y, brush),
            "Terminal" => DrawText(dc, timeText + ampm, TerminalFont, FontWeights.Bold, FontStyles.Normal, 14 * sizeScale, x, y, brush),
            "Outline" => DrawOutline(dc, timeText + ampm, StandardFont, FontWeights.Bold, 15 * sizeScale, x, y, brush),
            _ => DrawText(dc, timeText + ampm, StandardFont, FontWeights.SemiBold, FontStyles.Normal, 15 * sizeScale, x, y, brush)
        };
    }

    private double DrawText(DrawingContext? dc, string text, FontFamily font, FontWeight weight, FontStyle fontStyle, double size, double x, double y, Brush brush)
    {
        var tf = new Typeface(font, fontStyle, weight, FontStretches.Normal);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, brush, dpi);

        double totalH = DefaultHeight * sizeScale;
        double offsetY = y + (totalH - ft.Height) / 2.0;

        if (dc != null)
        {
            dc.DrawText(ft, new Point(x, offsetY));
        }

        return ft.WidthIncludingTrailingWhitespace;
    }

    private double DrawOutline(DrawingContext? dc, string text, FontFamily font, FontWeight weight, double size, double x, double y, Brush strokeBrush)
    {
        var tf = new Typeface(font, FontStyles.Normal, weight, FontStretches.Normal);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, strokeBrush, dpi);

        double totalH = DefaultHeight * sizeScale;
        double offsetY = y + (totalH - ft.Height) / 2.0;

        if (dc != null)
        {
            var geom = ft.BuildGeometry(new Point(x, offsetY));
            geom.Freeze();
            var pen = new Pen(strokeBrush, 1.2 * sizeScale);
            pen.Freeze();
            dc.DrawGeometry(null, pen, geom);
        }

        return ft.WidthIncludingTrailingWhitespace;
    }

    private double DrawStacked(DrawingContext? dc, string hour, string min, string sec, string ampm, double x, double y, Brush brush)
    {
        var tf = new Typeface(StandardFont, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        double fontSize = 11 * sizeScale;

        var ftH = new FormattedText(hour, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, fontSize, brush, dpi);
        var ftM = new FormattedText(min, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, fontSize, brush, dpi);

        double colW = Math.Max(ftH.Width, ftM.Width);
        double totalH = DefaultHeight * sizeScale;
        double topH = y + (totalH / 2.0) - ftH.Height + 1;
        double topM = y + (totalH / 2.0) - 1;

        if (dc != null)
        {
            dc.DrawText(ftH, new Point(x + (colW - ftH.Width) / 2.0, topH));
            dc.DrawText(ftM, new Point(x + (colW - ftM.Width) / 2.0, topM));
        }

        double width = colW;
        if (showSeconds)
        {
            var ftS = new FormattedText(sec, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 8.5 * sizeScale, brush, dpi);
            if (dc != null) dc.DrawText(ftS, new Point(x + colW + 3, y + (totalH - ftS.Height) / 2.0));
            width += 3 + ftS.Width;
        }

        if (!string.IsNullOrEmpty(ampm))
        {
            var ftA = new FormattedText(ampm.Trim(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 8 * sizeScale, brush, dpi);
            if (dc != null) dc.DrawText(ftA, new Point(x + width + 2, y + (totalH - ftA.Height) / 2.0));
            width += 2 + ftA.Width;
        }

        return width;
    }

    private double DrawWithDate(DrawingContext? dc, string timeText, string ampm, double x, double y, Brush brush)
    {
        var tf = new Typeface(StandardFont, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var ftT = new FormattedText(timeText + ampm, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 11 * sizeScale, brush, dpi);

        string dateText = currentTime.ToString("ddd M/d", CultureInfo.InvariantCulture);
        var secBrush = new SolidColorBrush(Color.FromArgb(170, 255, 255, 255));
        secBrush.Freeze();
        var ftD = new FormattedText(dateText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9 * sizeScale, secBrush, dpi);

        double width = Math.Max(ftT.Width, ftD.Width);
        double totalH = DefaultHeight * sizeScale;
        double topT = y + 1;
        double topD = y + totalH - ftD.Height - 1;

        if (dc != null)
        {
            dc.DrawText(ftT, new Point(x + (width - ftT.Width) / 2.0, topT));
            dc.DrawText(ftD, new Point(x + (width - ftD.Width) / 2.0, topD));
        }

        return width;
    }
}
