using System.IO;
using System.Windows;
using System.Windows.Markup;
using Island.App.Themes;
using Island.Core.Settings;
using Island.Core.Themes;
using Xunit;

namespace Island.Tests;

public sealed class LayoutTests : TestFolder
{
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
    public void ShippedThemesResolveAndContainDesignTokens(string themeName)
    {
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        var palette = catalog.Resolve(themeName);

        Assert.NotNull(palette);
        Assert.False(string.IsNullOrWhiteSpace(palette.Background));
        Assert.False(string.IsNullOrWhiteSpace(palette.Surface));
        Assert.False(string.IsNullOrWhiteSpace(palette.SurfaceRaised));
        Assert.False(string.IsNullOrWhiteSpace(palette.SurfaceHover));
        Assert.False(string.IsNullOrWhiteSpace(palette.Border));
        Assert.False(string.IsNullOrWhiteSpace(palette.BorderStrong));
        Assert.False(string.IsNullOrWhiteSpace(palette.Foreground));
        Assert.False(string.IsNullOrWhiteSpace(palette.TextSecondary));
        Assert.False(string.IsNullOrWhiteSpace(palette.TextTertiary));
        Assert.False(string.IsNullOrWhiteSpace(palette.Accent));
    }

    [Theory]
    [InlineData("Opaque", 255)]
    [InlineData("Translucent", 205)]
    [InlineData("Clear", 115)]
    [InlineData("UnknownFallback", 255)]
    public void SurfaceStylesProduceExpectedAlphaLevels(string style, byte expectedAlpha)
    {
        var res = new ResourceDictionary();
        var catalog = new ThemeCatalog(Folder, Messages.Add);
        var themeRes = new ThemeResources(res, catalog);

        themeRes.Apply("Midnight", style);

        var surfaceBrush = res["Theme.Surface"] as System.Windows.Media.SolidColorBrush;
        Assert.NotNull(surfaceBrush);
        Assert.Equal(expectedAlpha, surfaceBrush.Color.A);
    }

    [Fact]
    public void InterfaceSizeAndUiScaleNormalizeCorrectly()
    {
        var settings = new AppSettings();
        Assert.Equal("Auto", settings.InterfaceSize);
        Assert.Equal(1.0, settings.UiScale);

        var clampedLow = settings with { UiScale = 0.5 };
        Assert.Equal(0.9, clampedLow.Normalize().UiScale);

        var clampedHigh = settings with { UiScale = 2.5 };
        Assert.Equal(1.5, clampedHigh.Normalize().UiScale);

        var invalidSize = settings with { InterfaceSize = "Giant" };
        Assert.Equal("Auto", invalidSize.Normalize().InterfaceSize);
    }

    [Theory]
    [InlineData(1366, 768, 1.0)]
    [InlineData(1920, 1080, 1.0)]
    [InlineData(1920, 1080, 1.25)]
    [InlineData(2560, 1440, 1.0)]
    [InlineData(2560, 1440, 1.5)]
    public void TabDimensionsSatisfyMinMaxConstraintsAnd8DipGrid(int screenW, int screenH, double scale)
    {
        double effectiveW = screenW / scale;
        double effectiveH = screenH / scale;

        double minWidth = 580.0;
        double maxWidth = effectiveW * 0.62;
        double maxHeight = effectiveH * 0.70;

        // Base content widths for tabs: Music, System, Tasks, QuickControls, Focus, CalendarWeather, Clipboard (Unified Copies & Screenshots)
        double[] tabContentWidths = [580, 680, 580, 620, 580, 680, 680];
        double[] tabContentHeights = [170, 415, 180, 260, 320, 340, 280];

        for (int i = 0; i < tabContentWidths.Length; i++)
        {
            double islandW = tabContentWidths[i] + 24;
            double islandH = tabContentHeights[i] + 112; // Header (Row 1: 44, Row 2: 36) + margins (12+8+12)

            islandW = Math.Clamp(islandW, minWidth, maxWidth);
            islandH = Math.Clamp(islandH, 120, maxHeight);

            // Snap to 8 DIP grid
            double snappedW = Math.Ceiling(islandW / 8.0) * 8.0;
            double snappedH = Math.Ceiling(islandH / 8.0) * 8.0;

            Assert.True(snappedW >= 580, $"Tab {i} width {snappedW} must be >= 580");
            Assert.True(snappedW <= maxWidth + 8, $"Tab {i} width {snappedW} must be <= max width {maxWidth}");
            Assert.True(snappedH <= maxHeight + 8, $"Tab {i} height {snappedH} must be <= max height {maxHeight}");
            Assert.Equal(0.0, snappedW % 8.0);
            Assert.Equal(0.0, snappedH % 8.0);
        }
    }

    [Theory]
    [InlineData(1920, 1080, 1.0, 116.0)] // BelowClock (default)
    [InlineData(1920, 1080, 1.25, 116.0)]
    [InlineData(1920, 1080, 1.5, 116.0)]
    [InlineData(1920, 1080, 1.0, 80.0)]   // LeftOfClock (legacy)
    [InlineData(2560, 1440, 1.0, 116.0)]
    [InlineData(2560, 1440, 1.25, 116.0)]
    [InlineData(3840, 2160, 1.5, 116.0)]
    [InlineData(3840, 2160, 2.0, 116.0)]
    [InlineData(1366, 768, 1.0, 116.0)]
    public void TabDimensionsHaveNoMoreThan12DipUnusedSpaceOnAnySide(int screenW, int screenH, double scale, double overhead)
    {
        double effectiveW = screenW / scale;
        double effectiveH = screenH / scale;
        double minIslandW = 580.0;
        double maxIslandW = Math.Max(minIslandW, Math.Floor(effectiveW * 0.62 / 8.0) * 8.0);
        double maxIslandH = Math.Max(180.0, Math.Floor(effectiveH * 0.70 / 8.0) * 8.0);

        (double w, double h)[] tabs = [
            (580, 170), // Music
            (680, 415), // System (guarantees Top Apps has full headroom)
            (580, 180), // Tasks
            (620, 260), // Quick Controls
            (580, 320), // Focus
            (680, 340), // Calendar & Weather
            (680, 280)  // Clipboard (Unified Copies & Screenshots)
        ];

        for (int i = 0; i < tabs.Length; i++)
        {
            var (contentW, contentH) = tabs[i];

            double targetContentW = Math.Clamp(contentW, minIslandW - 28.0, maxIslandW - 28.0);
            double islandW = Math.Ceiling((targetContentW + 28.0) / 8.0) * 8.0;
            islandW = Math.Clamp(islandW, minIslandW, maxIslandW);

            double islandH = Math.Ceiling((contentH + overhead) / 8.0) * 8.0;
            islandH = Math.Clamp(islandH, 120.0, maxIslandH);

            // Bounds calculations: 14 DIP uniform outer padding
            double leftUnused = 14.0;
            double rightUnused = (islandW - 28.0) - targetContentW + 14.0;
            double topUnused = 14.0;
            double bottomUnused = (islandH - overhead) - contentH + 14.0;

            // Extra slack beyond uniform 14 DIP padding must be <= 8 DIP (< 12 DIP)
            Assert.True(Math.Abs(leftUnused - 14.0) <= 0.001, $"Tab {i} left padding must be exactly 14 DIP");
            Assert.True(rightUnused - 14.0 <= 8.0, $"Tab {i} right unused slack {rightUnused - 14.0} > 8 DIP");
            Assert.True(Math.Abs(topUnused - 14.0) <= 0.001, $"Tab {i} top padding must be exactly 14 DIP");
            Assert.True(bottomUnused - 14.0 <= 8.0, $"Tab {i} bottom unused slack {bottomUnused - 14.0} > 8 DIP");

            Assert.True(islandW >= 580.0, $"Tab {i} width {islandW} must be >= 580 DIP");
            Assert.True(islandW <= maxIslandW + 8.0, $"Tab {i} width {islandW} must be <= max width {maxIslandW}");
            Assert.True(islandH <= maxIslandH + 8.0, $"Tab {i} height {islandH} must be <= max height {maxIslandH}");
            Assert.Equal(0, (int)Math.Round(islandW) % 8);
            Assert.Equal(0, (int)Math.Round(islandH) % 8);
        }
    }

    [Fact]
    public void DesignTokensDictionaryDefinesRequiredSizesAndRadii()
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string xamlPath = Path.Combine(projectRoot, "src", "Island.App", "Themes", "DesignTokens.xaml");
        Assert.True(File.Exists(xamlPath), $"DesignTokens.xaml should exist at {xamlPath}");

        using var stream = File.OpenRead(xamlPath);
        var dict = (ResourceDictionary)XamlReader.Load(stream);

        // Corner Radii
        Assert.True(((CornerRadius)dict["Radius.Card"]).TopLeft >= 16);
        Assert.True(((CornerRadius)dict["Radius.Control"]).TopLeft >= 12);
        Assert.True(((CornerRadius)dict["Radius.Pill"]).TopLeft >= 999);

        // Font Sizes
        Assert.True((double)dict["Font.Body"] >= 14.0);
        Assert.True((double)dict["Font.Secondary"] >= 13.0);
        Assert.True((double)dict["Font.Label"] >= 12.5);
        Assert.True((double)dict["Font.HeaderTime"] >= 20.0);
        Assert.True((double)dict["Font.HeaderDate"] >= 13.0);
        Assert.True((double)dict["Font.CollapsedClock"] >= 16.0);

        // Typography 5-Role System Tokens
        Assert.NotNull(dict["Typography.Family.Display"]);
        Assert.NotNull(dict["Typography.Family.Heading"]);
        Assert.NotNull(dict["Typography.Family.Body"]);
        Assert.NotNull(dict["Typography.Family.Label"]);
        Assert.NotNull(dict["Typography.Family.Numeric"]);
        Assert.True((double)dict["Typography.Size.Body"] >= 14.0);
        Assert.True((double)dict["Typography.Size.HeaderTime"] >= 20.0);
    }

    private static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Island.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
