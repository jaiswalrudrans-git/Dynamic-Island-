using System.Windows;
using System.Windows.Media;
using Island.Core.Themes;

namespace Island.App.Themes;

public sealed class ThemeResources(ResourceDictionary applicationResources, ThemeCatalog catalog)
{
    public const string AccentKey = "Theme.Accent";
    private ResourceDictionary? active;
    private ThemePalette? current;

    public void Apply(string name)
    {
        var palette = catalog.Resolve(name);
        if (palette == current) return;

        var next = new ResourceDictionary
        {
            ["Theme.Background"] = Brush(palette.Background),
            ["Theme.Surface"] = Brush(palette.Surface),
            ["Theme.Foreground"] = Brush(palette.Foreground),
            [AccentKey] = Brush(palette.Accent)
        };
        var dictionaries = applicationResources.MergedDictionaries;
        if (active is null)
            dictionaries.Add(next);
        else
            dictionaries[dictionaries.IndexOf(active)] = next;
        active = next;
        current = palette;
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
