using System.Windows;
using System.Windows.Media;
using Island.Core.Themes;

namespace Island.App.Themes;

public sealed class ThemeResources(ResourceDictionary applicationResources, ThemeCatalog catalog)
{
    public const string AccentKey = "Theme.Accent";
    private ResourceDictionary? active;
    private ThemePalette? current;
    private string currentStyle = "Opaque";
    private double currentBgTransp = -1;
    private double currentCollapsedTransp = -1;
    private double currentNeonIntensity = -1;
    private Color? currentAccentOverride;

    public void Apply(string name, string surfaceStyle = "Opaque", double? backgroundTransparency = null, double? collapsedTransparency = null, double? neonIntensity = null, Color? accentOverride = null)
    {
        var palette = catalog.Resolve(name);
        double bgTransp = backgroundTransparency ?? (surfaceStyle == "Clear" ? 55.0 : surfaceStyle == "Translucent" ? 20.0 : 0.0);
        double colTransp = collapsedTransparency ?? 0.0;
        double intensity = Math.Clamp(neonIntensity ?? 0.6, 0.0, 1.0);
        if (palette == current && surfaceStyle == currentStyle && bgTransp == currentBgTransp && colTransp == currentCollapsedTransp && intensity == currentNeonIntensity && accentOverride == currentAccentOverride) return;

        byte alpha = surfaceStyle switch
        {
            "Clear" when !backgroundTransparency.HasValue => 115,
            "Translucent" when !backgroundTransparency.HasValue => 205,
            _ => (byte)Math.Clamp(Math.Round(255.0 * (1.0 - bgTransp / 100.0)), 60, 255)
        };
        byte collapsedAlpha = (byte)Math.Clamp(Math.Round(255.0 * (1.0 - colTransp / 100.0)), 60, 255);
        byte cardAlpha = palette.IsDark ? (byte)14 : (byte)24; // ~5.5% white on dark, subtle tint on light

        var accentColor = accentOverride ?? (Color)ColorConverter.ConvertFromString(palette.Accent);
        var dataColor = accentOverride ?? (Color)ColorConverter.ConvertFromString(palette.EffectiveData);

        var glow = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = accentColor,
            BlurRadius = 14 * intensity,
            ShadowDepth = 0,
            Opacity = 0.85 * intensity
        };
        glow.Freeze();

        var dataGlow = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = dataColor,
            BlurRadius = 12 * intensity,
            ShadowDepth = 0,
            Opacity = 0.8 * intensity
        };
        dataGlow.Freeze();

        var accentBrush = new SolidColorBrush(accentColor);
        accentBrush.Freeze();
        byte accentAlpha = (byte)Math.Clamp(Math.Round(255.0 * 0.20), 40, 60); // 20% opacity accent-soft
        var accentSoftBrush = new SolidColorBrush(Color.FromArgb(accentAlpha, accentColor.R, accentColor.G, accentColor.B));
        accentSoftBrush.Freeze();
        var dataBrush = new SolidColorBrush(dataColor);
        dataBrush.Freeze();

        var next = new ResourceDictionary
        {
            ["Theme.Background"] = Brush(palette.Background),
            ["Theme.Surface"] = BrushWithAlpha(palette.Surface, alpha),
            ["Theme.CollapsedSurface"] = BrushWithAlpha(palette.Surface, collapsedAlpha),
            ["Theme.SurfaceRaised"] = palette.IsDark ? BrushWithAlpha("#FFFFFF", cardAlpha) : BrushWithAlpha(palette.EffectiveSurfaceRaised, alpha),
            ["Theme.SurfaceHover"] = palette.IsDark ? BrushWithAlpha("#FFFFFF", 25) : BrushWithAlpha(palette.EffectiveSurfaceHover, alpha),
            ["Theme.Border"] = Brush(palette.EffectiveBorder),
            ["Theme.BorderStrong"] = Brush(palette.EffectiveBorderStrong),
            ["Theme.CardRestBorder"] = Brushes.Transparent,
            ["Theme.Foreground"] = Brush(palette.Foreground),
            ["Theme.TextPrimary"] = Brush(palette.Foreground),
            ["Theme.TextSecondary"] = Brush(palette.EffectiveTextSecondary),
            ["Theme.TextTertiary"] = Brush(palette.EffectiveTextTertiary),
            [AccentKey] = accentBrush,
            ["Theme.AccentSoft"] = accentSoftBrush,
            ["Theme.Data"] = dataBrush,
            ["Theme.DataSoft"] = BrushWithAlpha(palette.EffectiveData, 45),
            ["Theme.NeonLine"] = Brush(palette.EffectiveNeonLine),
            ["Theme.Success"] = Brush(palette.EffectiveSuccess),
            ["Theme.Warning"] = Brush(palette.EffectiveWarning),
            ["Theme.Danger"] = Brush(palette.EffectiveDanger),
            ["Theme.Shadow"] = Brush(palette.EffectiveShadow),
            ["Theme.InnerRim"] = Brush("#14FFFFFF"),
            ["Theme.CardHighlight"] = Brush("#18FFFFFF"),
            ["Theme.NeonGlow"] = glow,
            ["Theme.DataGlow"] = dataGlow
        };

        var dictionaries = applicationResources.MergedDictionaries;
        if (active is null)
            dictionaries.Add(next);
        else
            dictionaries[dictionaries.IndexOf(active)] = next;

        active = next;
        current = palette;
        currentStyle = surfaceStyle;
        currentBgTransp = bgTransp;
        currentCollapsedTransp = colTransp;
        currentNeonIntensity = intensity;
        currentAccentOverride = accentOverride;
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush BrushWithAlpha(string hex, byte alpha)
    {
        var c = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
        brush.Freeze();
        return brush;
    }
}
