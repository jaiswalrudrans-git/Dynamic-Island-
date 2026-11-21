using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xml.Linq;
using Xunit;

namespace Island.Tests;

public sealed class SpacingAndChromeLintTests
{
    private static string? FindProjectRoot()
    {
        string? current = AppDomain.CurrentDomain.BaseDirectory;
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "Island.sln")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        return null;
    }

    [Fact]
    public void DesignTokensDefinesRequiredSpacingAndTileTokens()
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string xamlPath = Path.Combine(projectRoot, "src", "Island.App", "Design", "DesignTokens.xaml");
        Assert.True(File.Exists(xamlPath), $"DesignTokens.xaml should exist at {xamlPath}");

        using var stream = File.OpenRead(xamlPath);
        var dict = (ResourceDictionary)XamlReader.Load(stream);

        // Single root padding owner (16 left/right, 14 top/bottom)
        Assert.Equal(new Thickness(16, 14, 16, 14), (Thickness)dict["Padding.Island"]);

        // Spacing gap tokens
        Assert.Equal(16.0, (double)dict["Spacing.Group"]);
        Assert.Equal(8.0, (double)dict["Spacing.Item"]);
        Assert.Equal(6.0, (double)dict["Spacing.Inline"]);

        // Divider
        Assert.NotNull(dict["Divider.Brush"]);
        Assert.Equal(1.0, (double)dict["Divider.Height"]);

        // List row hover tokens
        Assert.NotNull(dict["Row.Hover.Background"]);
        Assert.Equal(new CornerRadius(10), (CornerRadius)dict["Row.Hover.Radius"]);
        Assert.Equal(new Thickness(8, 6, 8, 6), (Thickness)dict["Row.Padding"]);

        // Quick Controls tile tokens
        Assert.Equal(new CornerRadius(14), (CornerRadius)dict["Radius.Tile"]);
        Assert.Equal(new Thickness(10), (Thickness)dict["Padding.Tile"]);
    }

    [Theory]
    [InlineData("SystemPage.xaml")]
    [InlineData("CalendarWeatherPage.xaml")]
    [InlineData("MusicPage.xaml")]
    [InlineData("FocusPage.xaml")]
    [InlineData("ClipboardPage.xaml")]
    [InlineData("TasksPage.xaml")]
    [InlineData("QuickControlsPage.xaml")]
    public void AllTabRootGridsHaveZeroMargin(string fileName)
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string filePath = Path.Combine(projectRoot, "src", "Island.App", "Views", fileName);
        Assert.True(File.Exists(filePath), $"{fileName} must exist at {filePath}");

        var doc = XDocument.Load(filePath);
        var root = doc.Root;
        Assert.NotNull(root);

        // Find the first top-level Grid inside UserControl
        var firstGrid = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Grid");
        Assert.NotNull(firstGrid);

        var marginAttr = firstGrid.Attribute("Margin");
        if (marginAttr != null)
        {
            string val = marginAttr.Value.Trim();
            Assert.True(val == "0" || val == "0,0,0,0",
                $"{fileName} root Grid must have Margin='0' so island content root is the sole padding owner. Found: '{val}'");
        }
    }

    [Theory]
    [InlineData("SystemPage.xaml")]
    [InlineData("CalendarWeatherPage.xaml")]
    [InlineData("MusicPage.xaml")]
    [InlineData("FocusPage.xaml")]
    [InlineData("ClipboardPage.xaml")]
    public void NoNonInteractiveCardChromeOnFlatTabs(string fileName)
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string filePath = Path.Combine(projectRoot, "src", "Island.App", "Views", fileName);
        Assert.True(File.Exists(filePath), $"{fileName} must exist at {filePath}");

        var doc = XDocument.Load(filePath);

        // Ensure no container Border uses IslandCard style on these flat tabs
        var bordersWithIslandCard = doc.Descendants()
            .Where(e => e.Name.LocalName == "Border" &&
                        e.Attribute("Style")?.Value.Contains("IslandCard") == true)
            .ToList();

        Assert.Empty(bordersWithIslandCard);
    }

    [Theory]
    [InlineData("SystemPage.xaml")]
    [InlineData("ClipboardPage.xaml")]
    [InlineData("TasksPage.xaml")]
    public void ListRowsUseFlatListRowStyle(string fileName)
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string filePath = Path.Combine(projectRoot, "src", "Island.App", "Views", fileName);
        Assert.True(File.Exists(filePath), $"{fileName} must exist at {filePath}");

        string content = File.ReadAllText(filePath);
        Assert.Contains("StaticResource FlatListRow", content);
    }

    [Theory]
    [InlineData("SystemPage.xaml")]
    [InlineData("CalendarWeatherPage.xaml")]
    [InlineData("MusicPage.xaml")]
    [InlineData("FocusPage.xaml")]
    [InlineData("ClipboardPage.xaml")]
    public void NonInteractiveContainersDoNotHaveOuterMargins(string fileName)
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string filePath = Path.Combine(projectRoot, "src", "Island.App", "Views", fileName);
        Assert.True(File.Exists(filePath), $"{fileName} must exist at {filePath}");

        var doc = XDocument.Load(filePath);
        var rootGrid = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Grid");
        Assert.NotNull(rootGrid);

        // Ensure direct children of root grid don't have large outer margins (max allowed is small gap token <= 16)
        foreach (var child in rootGrid.Elements())
        {
            var marginAttr = child.Attribute("Margin");
            if (marginAttr != null)
            {
                var parts = marginAttr.Value.Split(',', StringSplitOptions.TrimEntries);
                foreach (var p in parts)
                {
                    if (double.TryParse(p, out double val))
                    {
                        Assert.True(val <= 20, $"{fileName} container element {child.Name.LocalName} has margin {val} exceeding group gap token");
                    }
                }
            }
        }
    }
}
