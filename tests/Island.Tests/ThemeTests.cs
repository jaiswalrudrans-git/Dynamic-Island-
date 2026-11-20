using System.IO;
using System.Windows;
using System.Windows.Media;
using Island.App.Themes;
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

    private void WriteCustom(string input)
    {
        Directory.CreateDirectory(Path.Combine(Folder, "themes"));
        File.WriteAllText(Path.Combine(Folder, "themes", "custom.json"), input);
    }
}
