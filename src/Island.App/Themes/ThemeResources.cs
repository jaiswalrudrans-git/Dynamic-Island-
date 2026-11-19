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

    public void Apply(string name, string surfaceStyle = "Opaque")
    {
        var palette = catalog.Resolve(name);
        if (palette == current && surfaceStyle == currentStyle) return;

        byte alpha = surfaceStyle switch
        {
            "Clear" => 115,
            "Translucent" => 205,
            _ => 255
        };

        var next = new ResourceDictionary
        {
            ["Theme.Background"] = Brush(palette.Background),
            ["Theme.Surface"] = BrushWithAlpha(palette.Surface, alpha),
            ["Theme.SurfaceRaised"] = BrushWithAlpha(palette.EffectiveSurfaceRaised, alpha),
            ["Theme.SurfaceHover"] = BrushWithAlpha(palette.EffectiveSurfaceHover, alpha),
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
            ["Theme.Shadow"] = Brush(palette.EffectiveShadow)
        };

        var dictionaries = applicationResources.MergedDictionaries;
        if (active is null)
            dictionaries.Add(next);
        else
            dictionaries[dictionaries.IndexOf(active)] = next;

        active = next;
        current = palette;
        currentStyle = surfaceStyle;
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
