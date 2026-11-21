using System.IO;
using System.Windows;
using System.Windows.Media;
using Island.App.Design;
using Island.Core.Themes;
using Xunit;

namespace Island.Tests;

public sealed class ThemeTests : TestFolder
{
    [Theory]
    [InlineData("Dark", "#9BC5FF")]
    [InlineData("Light", "#315BA5")]
    [InlineData("One Dark Pro", "#61AFEF")]
    [InlineData("GitHub Dark", "#58A6FF")]
    [InlineData("Dracula", "#BD93F9")]
    [InlineData("Night Owl", "#82AAFF")]
    [InlineData("Cyberpunk", "#FF2E97")]
    [InlineData("Aurora", "#7B61FF")]
    [InlineData("Amber Terminal", "#FFD54F")]
    [InlineData("Mono White", "#00E5FF")]
    [InlineData("Sakura", "#FF6584")]
    public void BundledPaletteHasExpectedAccent(string name, string accent)
    {
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        Assert.Equal(accent, catalog.Resolve(name).Accent);
        Assert.Empty(Messages);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{")]
    [InlineData("""{"background":"#000000","surface":"#222222","foreground":"white","accent":"#FF0000"}""")]
    public void MissingOrInvalidCustomThemeFallsBackToDark(string? input)
    {
        if (input != null) WriteCustom(input);
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        Assert.Equal(catalog.Resolve("Dark"), catalog.Resolve("Custom"));
        Assert.Single(Messages);
    }

    [Fact]
    public void ValidCustomPaletteAppliesAndSwitchingThemeReplacesOnlyItsBrushes()
    {
        WriteCustom("""{"background":"#000000","surface":"#102030","foreground":"#FFFFFF","accent":"#ABCDEF"}""");
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        var root = new ResourceDictionary();
        var existing = new ResourceDictionary { ["Existing"] = "retained" };
        root.MergedDictionaries.Add(existing);
        var resources = new ThemeResources(root, catalog);

        resources.Apply("Custom");
        var accent = Assert.IsType<SolidColorBrush>(root["Theme.Accent"]);
        Assert.True(accent.IsFrozen);
        Assert.Equal(Color.FromRgb(0xAB, 0xCD, 0xEF), accent.Color);
        resources.Apply("Unknown");
        Assert.Equal(2, root.MergedDictionaries.Count);
        Assert.Same(existing, root.MergedDictionaries[0]);
        Assert.Equal("#FF9BC5FF", root["Theme.Accent"].ToString());
    }

    [Fact]
    public void TransparencyAndInnerRimAppliedCorrectly()
    {
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        var root = new ResourceDictionary();
        var resources = new ThemeResources(root, catalog);

        // 12% background transparency and 0% collapsed transparency
        resources.Apply("Dark", "Opaque", 12.0, 0.0);

        var surface = Assert.IsType<SolidColorBrush>(root["Theme.Surface"]);
        Assert.True(surface.IsFrozen);
        // 255 * (1 - 0.12) = 224.4 -> 224
        Assert.Equal(224, surface.Color.A);

        var collapsed = Assert.IsType<SolidColorBrush>(root["Theme.CollapsedSurface"]);
        Assert.True(collapsed.IsFrozen);
        Assert.Equal(255, collapsed.Color.A);

        var innerRim = Assert.IsType<SolidColorBrush>(root["Theme.InnerRim"]);
        Assert.True(innerRim.IsFrozen);
        Assert.Equal(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF), innerRim.Color);

        var cardFill = Assert.IsType<SolidColorBrush>(root["Theme.SurfaceRaised"]);
        Assert.True(cardFill.IsFrozen);
        Assert.True(cardFill.Color.A <= 25); // Subtle card overlay
    }

    [Fact]
    public void CyberpunkThemeProvidesDataTokensAndGlowScalesWithIntensity()
    {
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        var root = new ResourceDictionary();
        var resources = new ThemeResources(root, catalog);

        // Cyberpunk with 80% neon intensity
        resources.Apply("Cyberpunk", "Opaque", 12.0, 0.0, 0.8);

        var dataBrush = Assert.IsType<SolidColorBrush>(root["Theme.Data"]);
        Assert.True(dataBrush.IsFrozen);
        Assert.Equal(Color.FromRgb(0x00, 0xE5, 0xFF), dataBrush.Color);

        var neonLine = Assert.IsType<SolidColorBrush>(root["Theme.NeonLine"]);
        Assert.True(neonLine.IsFrozen);
        Assert.Equal(Color.FromRgb(0xFF, 0x2E, 0x97), neonLine.Color);

        var glow = Assert.IsType<System.Windows.Media.Effects.DropShadowEffect>(root["Theme.NeonGlow"]);
        Assert.True(glow.IsFrozen);
        Assert.Equal(Color.FromRgb(0xFF, 0x2E, 0x97), glow.Color);
        Assert.Equal(14 * 0.8, glow.BlurRadius, 2);
    }

    [Theory]
    [InlineData("Midnight")]
    [InlineData("Dark")]
    [InlineData("Graphite")]
    [InlineData("Pure Black")]
    [InlineData("Light")]
    [InlineData("One Dark Pro")]
    [InlineData("GitHub Dark")]
    [InlineData("Dracula")]
    [InlineData("Night Owl")]
    [InlineData("Cyberpunk")]
    [InlineData("Aurora")]
    [InlineData("Amber Terminal")]
    [InlineData("Mono White")]
    [InlineData("Sakura")]
    public void ShippedThemeSatisfiesContrastRequirements(string themeName)
    {
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        var palette = catalog.Resolve(themeName);

        static double Channel(double c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        static double Lum(Color color) =>
            0.2126 * Channel(color.R / 255.0) +
            0.7152 * Channel(color.G / 255.0) +
            0.0722 * Channel(color.B / 255.0);

        static double Contrast(Color c1, Color c2)
        {
            double l1 = Lum(c1);
            double l2 = Lum(c2);
            if (l1 < l2) (l1, l2) = (l2, l1);
            return (l1 + 0.05) / (l2 + 0.05);
        }

        static Color ParseHex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
        static Color Blend(Color top, double alpha, Color bottom) => Color.FromRgb(
            (byte)Math.Clamp(Math.Round(top.R * alpha + bottom.R * (1 - alpha)), 0, 255),
            (byte)Math.Clamp(Math.Round(top.G * alpha + bottom.G * (1 - alpha)), 0, 255),
            (byte)Math.Clamp(Math.Round(top.B * alpha + bottom.B * (1 - alpha)), 0, 255));

        var bg = ParseHex(palette.Background);
        var surf = ParseHex(palette.Surface);
        var fg = ParseHex(palette.Foreground);
        var sec = ParseHex(palette.EffectiveTextSecondary);

        // 0% transparency: text on surface
        double primContrast0 = Contrast(fg, surf);
        double secContrast0 = Contrast(sec, surf);

        // 50% transparency: surface blended over background
        var surf50 = Blend(surf, 0.5, bg);
        double primContrast50 = Contrast(fg, surf50);
        double secContrast50 = Contrast(sec, surf50);

        Assert.True(primContrast0 >= 7.0, $"{themeName} Primary text contrast at 0% ({primContrast0:F1}:1) must be >= 7:1");
        Assert.True(primContrast50 >= 7.0, $"{themeName} Primary text contrast at 50% ({primContrast50:F1}:1) must be >= 7:1");
        Assert.True(secContrast0 >= 4.5, $"{themeName} Secondary text contrast at 0% ({secContrast0:F1}:1) must be >= 4.5:1");
        Assert.True(secContrast50 >= 4.5, $"{themeName} Secondary text contrast at 50% ({secContrast50:F1}:1) must be >= 4.5:1");
    }

    private void WriteCustom(string input)
    {
        Directory.CreateDirectory(Path.Combine(Folder, "themes"));
        File.WriteAllText(Path.Combine(Folder, "themes", "custom.json"), input);
    }
}
