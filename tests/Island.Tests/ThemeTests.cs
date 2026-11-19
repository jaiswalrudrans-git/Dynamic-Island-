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
    [InlineData("Cyberpunk", "#FF58D4")]
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

    private void WriteCustom(string input)
    {
        Directory.CreateDirectory(Path.Combine(Folder, "themes"));
        File.WriteAllText(Path.Combine(Folder, "themes", "custom.json"), input);
    }
}
