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

    public void Apply(string name, string surfaceStyle = "Opaque", double? backgroundTransparency = null, double? collapsedTransparency = null)
    {
        var palette = catalog.Resolve(name);
        double bgTransp = backgroundTransparency ?? (surfaceStyle == "Clear" ? 55.0 : surfaceStyle == "Translucent" ? 20.0 : 0.0);
        double colTransp = collapsedTransparency ?? 0.0;
        if (palette == current && surfaceStyle == currentStyle && bgTransp == currentBgTransp && colTransp == currentCollapsedTransp) return;

        byte alpha = surfaceStyle switch
        {
            "Clear" when !backgroundTransparency.HasValue => 115,
            "Translucent" when !backgroundTransparency.HasValue => 205,
            _ => (byte)Math.Clamp(Math.Round(255.0 * (1.0 - bgTransp / 100.0)), 60, 255)
        };
        byte collapsedAlpha = (byte)Math.Clamp(Math.Round(255.0 * (1.0 - colTransp / 100.0)), 60, 255);
        byte cardAlpha = palette.IsDark ? (byte)14 : (byte)24; // ~5.5% white on dark, subtle tint on light

        var next = new ResourceDictionary
        {
            ["Theme.Background"] = Brush(palette.Background),
            ["Theme.Surface"] = BrushWithAlpha(palette.Surface, alpha),
            ["Theme.CollapsedSurface"] = BrushWithAlpha(palette.Surface, collapsedAlpha),
            ["Theme.SurfaceRaised"] = palette.IsDark ? BrushWithAlpha("#FFFFFF", cardAlpha) : BrushWithAlpha(palette.EffectiveSurfaceRaised, alpha),
            ["Theme.SurfaceHover"] = palette.IsDark ? BrushWithAlpha("#FFFFFF", 25) : BrushWithAlpha(palette.EffectiveSurfaceHover, alpha),
            ["Theme.Border"] = Brush(palette.EffectiveBorder),
            ["Theme.BorderStrong"] = Brush(palette.EffectiveBorderStrong),
            ["Theme.Foreground"] = Brush(palette.Foreground),
            ["Theme.TextPrimary"] = Brush(palette.Foreground),
            ["Theme.TextSecondary"] = Brush(palette.EffectiveTextSecondary),
            ["Theme.TextTertiary"] = Brush(palette.EffectiveTextTertiary),
            [AccentKey] = Brush(palette.Accent),
            ["Theme.AccentSoft"] = Brush(palette.EffectiveAccentSoft),
            ["Theme.Success"] = Brush(palette.EffectiveSuccess),
            ["Theme.Warning"] = Brush(palette.EffectiveWarning),
            ["Theme.Danger"] = Brush(palette.EffectiveDanger),
            ["Theme.Shadow"] = Brush(palette.EffectiveShadow),
            ["Theme.InnerRim"] = Brush("#14FFFFFF"),
            ["Theme.CardHighlight"] = Brush("#18FFFFFF")
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
