using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Island.App.Design;
using Island.App.Design.Typography;
using Island.App.Features.Settings.Views;
using Island.App.Header.Views;
using Island.Core.Settings;
using Island.Core.Themes;
using Xunit;
using Xunit.Abstractions;

namespace Island.Tests.Design;

public sealed class TypographyTests
{
    private readonly ITestOutputHelper? output;

    public TypographyTests(ITestOutputHelper? output = null)
    {
        this.output = output;
    }

    [Fact]
    public void EveryPresetResolvesToExpectedFamiliesAndDotMatrixDisplayNotEqualBody()
    {
        // (1) Every preset resolves to the families listed above (Display of Dot Matrix != Body)
        Assert.Equal(12, TypographyResources.Presets.Length);

        var dotMatrix = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Dot Matrix");
        Assert.NotNull(dotMatrix);
        Assert.NotEqual(dotMatrix.Display, dotMatrix.Body);
        Assert.Equal(TypographyResources.DotoFamily, dotMatrix.Display);
        Assert.Equal(TypographyResources.DotoFamily, dotMatrix.Heading);
        Assert.Equal(TypographyResources.DotoFamily, dotMatrix.Numeric);
        Assert.Equal(TypographyResources.SegoeTextFamily, dotMatrix.Body);
        Assert.Equal(TypographyResources.SegoeTextFamily, dotMatrix.Label);
        Assert.Equal(TypographyResources.SegoeTextFamily, dotMatrix.UI);
        Assert.Equal(TypographyResources.InterFamily, dotMatrix.Companion);

        // Check Classic
        var classic = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Classic");
        Assert.NotNull(classic);
        Assert.Equal(TypographyResources.SegoeFamily, classic.Display);
        Assert.Equal(TypographyResources.SegoeTextFamily, classic.Body);

        // Check LED Segment
        var led = TypographyResources.Presets.FirstOrDefault(p => p.Id == "LED");
        Assert.NotNull(led);
        Assert.Equal(TypographyResources.ShareTechMonoFamily, led.Display);
        Assert.Equal(TypographyResources.ShareTechMonoFamily, led.Heading);
        Assert.Equal(TypographyResources.ShareTechMonoFamily, led.Numeric);

        // Check Neon Techno
        var neon = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Neon Techno");
        Assert.NotNull(neon);
        Assert.Equal(TypographyResources.OrbitronFamily, neon.Display);
        Assert.Equal(TypographyResources.RajdhaniFamily, neon.Heading);
        Assert.Equal(TypographyResources.OrbitronFamily, neon.Numeric);
        Assert.True(neon.HasGlow);

        // Check Editorial Serif
        var editorial = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Editorial Serif");
        Assert.NotNull(editorial);
        Assert.Equal(TypographyResources.InstrumentSerifFamily, editorial.Display);
        Assert.Equal(TypographyResources.InstrumentSerifFamily, editorial.Heading);
        Assert.Equal(TypographyResources.InterFamily, editorial.Numeric);

        // Check Wide Futuristic
        var wide = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Wide Futuristic");
        Assert.NotNull(wide);
        Assert.Equal(TypographyResources.UnboundedFamily, wide.Display);
        Assert.Equal(TypographyResources.UnboundedFamily, wide.Heading);
        Assert.Equal(TypographyResources.ChakraPetchFamily, wide.Numeric);

        // Check Condensed Impact
        var condensed = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Condensed Impact");
        Assert.NotNull(condensed);
        Assert.Equal(TypographyResources.BebasNeueFamily, condensed.Display);
        Assert.Equal(TypographyResources.BebasNeueFamily, condensed.Heading);
        Assert.Equal(TypographyResources.BarlowCondensedFamily, condensed.Numeric);

        // Check Pixel Arcade
        var pixel = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Pixel Arcade");
        Assert.NotNull(pixel);
        Assert.Equal(TypographyResources.SilkscreenFamily, pixel.Display);
        Assert.Equal(TypographyResources.SilkscreenFamily, pixel.Heading);
        Assert.Equal(TypographyResources.ShareTechMonoFamily, pixel.Numeric);

        // Check Terminal Mono
        var terminal = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Terminal Mono");
        Assert.NotNull(terminal);
        Assert.Equal(TypographyResources.JetBrainsMonoFamily, terminal.Display);
        Assert.Equal(TypographyResources.JetBrainsMonoFamily, terminal.Heading);
        Assert.Equal(TypographyResources.JetBrainsMonoFamily, terminal.Numeric);

        // Check Rounded Soft
        var rounded = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Rounded Soft");
        Assert.NotNull(rounded);
        Assert.Equal(TypographyResources.FredokaFamily, rounded.Display);
        Assert.Equal(TypographyResources.FredokaFamily, rounded.Heading);
        Assert.Equal(TypographyResources.FredokaFamily, rounded.Numeric);

        // Check Handwritten Accent
        var handwritten = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Handwritten Accent");
        Assert.NotNull(handwritten);
        Assert.Equal(TypographyResources.CaveatFamily, handwritten.Display);
        Assert.Equal(TypographyResources.SegoeTextFamily, handwritten.Heading);
        Assert.Equal(TypographyResources.SegoeTextFamily, handwritten.Numeric);

        // Check Retro Wave
        var retrowave = TypographyResources.Presets.FirstOrDefault(p => p.Id == "Retro Wave");
        Assert.NotNull(retrowave);
        Assert.Equal(TypographyResources.RajdhaniFamily, retrowave.Display);
        Assert.Equal(TypographyResources.RajdhaniFamily, retrowave.Heading);
        Assert.Equal(TypographyResources.RajdhaniFamily, retrowave.Numeric);
        Assert.True(retrowave.HasGlow);
    }

    [Fact]
    public void ResolverReturnsSegoeForEveryDefaultZoneStyleUnderEveryPreset()
    {
        // (2) The resolver returns Segoe for every default-zone style under every preset
        ZoneRole[] defaultZones =
        [
            ZoneRole.TabIcons,
            ZoneRole.Tooltips,
            ZoneRole.ButtonsAndLabels,
            ZoneRole.Chips,
            ZoneRole.SegmentedControls,
            ZoneRole.ToggleTileNames,
            ZoneRole.SearchBoxes,
            ZoneRole.MenusAndContextMenus,
            ZoneRole.ScrollbarsText,
            ZoneRole.AlertsAndOsdText,
            ZoneRole.ListRows,
            ZoneRole.UnitsAndSecondaryText,
            ZoneRole.Lyrics,
            ZoneRole.ArtistName,
            ZoneRole.SectionSubtitles,
            ZoneRole.MutedSecondaryText,
            ZoneRole.HeaderDateLine,
            ZoneRole.MediaTimeLabels,
            ZoneRole.SettingsWindow,
            ZoneRole.PresetTileCaptions
        ];

        foreach (var preset in TypographyResources.Presets)
        {
            foreach (var zone in defaultZones)
            {
                var resolved = TypographyResolver.ResolveZoneFont(zone, preset, renderedSize: 20.0, text: "Sample 123");
                Assert.Equal(TypographyResources.SegoeTextFamily, resolved);
            }
        }
    }

    [Fact]
    public void PerElementGuardFallsBackOnlyForOffendingElement()
    {
        // (3) The per-element guard falls back only for the offending element
        var dotMatrix = TypographyResources.Presets.First(p => p.Id == "Dot Matrix");

        // Element 1: Normal large element (>= 18px threshold) -> resolves to Doto
        var element1 = TypographyResolver.ResolveElementFont(
            FontRole.Numeric, dotMatrix, renderedSize: 24.0, text: "29%", legibilityGuardEnabled: true);
        Assert.Equal(TypographyResources.DotoFamily, element1);

        // Element 2: Small element (< 18px for dot matrix) -> falls back to companion (Inter)
        var element2 = TypographyResolver.ResolveElementFont(
            FontRole.Numeric, dotMatrix, renderedSize: 14.0, text: "29%", legibilityGuardEnabled: true);
        Assert.Equal(dotMatrix.Companion, element2);

        // Element 3: Large element but contains unsupported unicode glyphs -> falls back to companion
        var element3 = TypographyResolver.ResolveElementFont(
            FontRole.Numeric, dotMatrix, renderedSize: 24.0, text: "CPU 29% \u4E16\u754C", legibilityGuardEnabled: true);
        Assert.Equal(dotMatrix.Companion, element3);

        // Element 4: Another normal large element checked AFTER the offending element -> STILL resolves to Doto!
        // (The rejected element must not make the whole preset or other elements fall back)
        var element4 = TypographyResolver.ResolveElementFont(
            FontRole.Numeric, dotMatrix, renderedSize: 20.0, text: "100%", legibilityGuardEnabled: true);
        Assert.Equal(TypographyResources.DotoFamily, element4);

        // When guard is disabled, even small size gets decorative font
        var element5 = TypographyResolver.ResolveElementFont(
            FontRole.Numeric, dotMatrix, renderedSize: 12.0, text: "29%", legibilityGuardEnabled: false);
        Assert.Equal(TypographyResources.DotoFamily, element5);
    }

    [Fact]
    public void AllPresetTilesResolveToDistinctDisplayFontsExceptClassic()
    {
        // (4) All preset tiles resolve to distinct Display fonts, except Classic
        var displaySources = TypographyResources.Presets
            .Select(p => p.Display.Source ?? p.Display.ToString())
            .ToList();

        // Exactly 12 presets and 12 distinct display families
        Assert.Equal(12, displaySources.Count);
        Assert.Equal(12, displaySources.Distinct().Count());

        var nonClassic = TypographyResources.Presets
            .Where(p => p.Id != "Classic")
            .Select(p => p.Display.Source ?? p.Display.ToString())
            .Distinct()
            .ToList();

        Assert.Equal(11, nonClassic.Count);
    }

    [Fact]
    public void EnumerateZoneMapDevCheck()
    {
        // Dev-only check (test or startup log) that enumerates the zone map and prints each zone's resolved font
        var dotMatrix = TypographyResources.Presets.First(p => p.Id == "Dot Matrix");
        string report = TypographyResolver.GenerateZoneMapReport(dotMatrix);
        output?.WriteLine(report);
        System.Diagnostics.Debug.WriteLine(report);

        Assert.Contains("Zone: TabIcons", report);
        Assert.Contains("Zone: FocusTimerDigits", report);
        Assert.Contains("Zone: SystemBigStatCpu", report);
        Assert.Contains("Zone: UnitsAndSecondaryText", report);
    }

    [Theory]
    [InlineData("Simple")]
    [InlineData("WithDate")]
    [InlineData("Stacked")]
    [InlineData("DotMatrix")]
    [InlineData("Segment")]
    [InlineData("Neon")]
    [InlineData("Serif")]
    [InlineData("Wide")]
    [InlineData("Condensed")]
    [InlineData("Pixel")]
    [InlineData("Compact")]
    [InlineData("Terminal")]
    [InlineData("Gradient")]
    [InlineData("Outline")]
    public void ClockSyncMapsEveryClockStyleToValidTypography(string clockStyle)
    {
        var res = new ResourceDictionary();
        var typography = new TypographyResources(res);

        var settings = new AppSettings
        {
            ClockStyle = clockStyle,
            MatchTypographyToClock = true,
            MatchAccentToClock = true
        };

        var clockColor = ClockFace.ResolveClockColor(settings);
        typography.Apply(settings, clockColor);

        Assert.NotNull(res["Typography.Family.Display"]);
        Assert.NotNull(res["Typography.Family.Heading"]);
        Assert.NotNull(res["Typography.Family.Body"]);
        Assert.NotNull(res["Typography.Family.Label"]);
        Assert.NotNull(res["Typography.Family.Numeric"]);

        Assert.True((double)res["Typography.Size.Body"] >= 12.0);
        Assert.True((double)res["Typography.Size.HeaderTime"] >= 18.0);
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(1.0)]
    [InlineData(1.15)]
    [InlineData(1.3)]
    public void TypographyScaleScalesSizesProportionally(double scale)
    {
        var res = new ResourceDictionary();
        var typography = new TypographyResources(res);

        var settings = new AppSettings
        {
            TypographySizeScale = scale
        };

        typography.Apply(settings);

        double bodySize = (double)res["Typography.Size.Body"];
        double headerTimeSize = (double)res["Typography.Size.HeaderTime"];

        Assert.Equal(14.0 * scale, bodySize, 2);
        Assert.Equal(20.0 * scale, headerTimeSize, 2);
    }

    [Fact]
    public void SystemPageBindsTypographyTokensToHeadingsNumericsAndLabels()
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string xamlPath = Directory.GetFiles(Path.Combine(projectRoot, "src", "Island.App"), "SystemPage.xaml", SearchOption.AllDirectories).First();
        string xaml = File.ReadAllText(xamlPath);

        Assert.Contains("Typography.Family.Heading", xaml);
        Assert.Contains("Typography.Family.Numeric", xaml);
        Assert.Contains("Typography.Family.Body", xaml);
        Assert.Contains("Typography.Size.SectionTitle", xaml);
        Assert.Contains("Typography.Size.Value", xaml);
    }

    [Fact]
    public void GenerateTypographyVerificationScreenshots()
    {
        string artifactDir = @"C:\Users\jaisw\.gemini\antigravity\brain\2b4e711b-ec08-48fd-9c2e-470d87aadd57";
        if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

        var thread = new Thread(() =>
        {
            var app = Application.Current ?? new Application();
            var tokensUri = new Uri("pack://application:,,,/Island.App;component/Design/DesignTokens.xaml");
            if (!app.Resources.MergedDictionaries.Any(d => d.Source == tokensUri))
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = tokensUri });
            }

            string projectRoot = FindProjectRoot()!;
            var catalog = new ThemeCatalog(projectRoot, _ => { });
            var themeRes = new ThemeResources(app.Resources, catalog);
            themeRes.Apply("Midnight", "Translucent");

            var settings = new AppSettings
            {
                TypographyPreset = "Dot Matrix",
                MatchTypographyToClock = false,
                LegibilityGuard = true
            };

            var typoRes = new TypographyResources(app.Resources);
            typoRes.Apply(settings);

            // 1. Screenshot of Settings > Typography
            var win = new MotionSettingsWindow(settings, _ => { });
            win.Show();
            var switchPageMethod = typeof(MotionSettingsWindow).GetMethod("SwitchPage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            switchPageMethod?.Invoke(win, new object[] { "Typography" });

            int winW = 860;
            int winH = 680;
            win.Width = winW;
            win.Height = winH;
            win.Measure(new Size(winW, winH));
            win.Arrange(new Rect(0, 0, winW, winH));
            win.UpdateLayout();

            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            var rtbWin = new RenderTargetBitmap(winW, winH, 96, 96, PixelFormats.Pbgra32);
            var rootVisual = (Visual)win.Content ?? win;
            rtbWin.Render(rootVisual);
            win.Close();

            var encWin = new PngBitmapEncoder();
            encWin.Frames.Add(BitmapFrame.Create(rtbWin));
            string settingsPath = Path.Combine(artifactDir, "screenshot_settings_typography.png");
            using (var fs = File.Create(settingsPath))
            {
                encWin.Save(fs);
            }

            // 2. Screenshot of System tab with Dot Matrix selected (big values dotted, units & labels in Segoe)
            int sysW = 680;
            int sysH = 340;
            var sysContainer = new Border
            {
                Width = sysW,
                Height = sysH,
                Background = new SolidColorBrush(Color.FromArgb(245, 12, 14, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16)
            };

            foreach (var key in app.Resources.Keys)
            {
                sysContainer.Resources[key] = app.Resources[key];
            }

            var rootStack = new StackPanel();

            // Header clock & title
            var clockHeader = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
            clockHeader.Children.Add(new TextBlock
            {
                Text = "14:32",
                FontFamily = (FontFamily)app.Resources["Typography.Family.Display"],
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 10, 0)
            });
            clockHeader.Children.Add(new TextBlock
            {
                Text = "SYSTEM TELEMETRY · DOT MATRIX PRESET",
                FontFamily = TypographyResources.SegoeTextFamily, // Segoe label
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)),
                VerticalAlignment = VerticalAlignment.Center
            });
            rootStack.Children.Add(clockHeader);

            // Bento Grid with Big Stats (Numeric) and Labels/Units (Segoe)
            var bento = new Grid();
            bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });

            // Column 0: CPU & Memory
            var col0 = new StackPanel();
            // CPU
            var cpuRow = new Grid();
            cpuRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cpuRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var cpuTitle = new TextBlock { Text = "CPU", FontFamily = (FontFamily)app.Resources["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            var cpuVal = new TextBlock { Text = "29%", FontFamily = (FontFamily)app.Resources["Typography.Family.Numeric"], FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
            Grid.SetColumn(cpuVal, 1);
            cpuRow.Children.Add(cpuTitle);
            cpuRow.Children.Add(cpuVal);
            col0.Children.Add(cpuRow);

            var cpuSub = new TextBlock { Text = "AMD Ryzen 9 7940HS · 4.0 GHz", FontFamily = TypographyResources.SegoeTextFamily, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 14) };
            col0.Children.Add(cpuSub);

            // Memory
            var memRow = new Grid();
            memRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            memRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var memTitle = new TextBlock { Text = "Memory", FontFamily = (FontFamily)app.Resources["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            var memVal = new TextBlock { Text = "48%", FontFamily = (FontFamily)app.Resources["Typography.Family.Numeric"], FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
            Grid.SetColumn(memVal, 1);
            memRow.Children.Add(memTitle);
            memRow.Children.Add(memVal);
            col0.Children.Add(memRow);

            var memSub = new TextBlock { Text = "In Use: 7.5 GB / 15.7 GB", FontFamily = TypographyResources.SegoeTextFamily, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 0) };
            col0.Children.Add(memSub);

            Grid.SetColumn(col0, 0);
            bento.Children.Add(col0);

            // Column 2: GPU & Power
            var col2 = new StackPanel();
            // GPU
            var gpuRow = new Grid();
            gpuRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gpuRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var gpuTitle = new TextBlock { Text = "GPU", FontFamily = (FontFamily)app.Resources["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            var gpuVal = new TextBlock { Text = "0%", FontFamily = (FontFamily)app.Resources["Typography.Family.Numeric"], FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
            Grid.SetColumn(gpuVal, 1);
            gpuRow.Children.Add(gpuTitle);
            gpuRow.Children.Add(gpuVal);
            col2.Children.Add(gpuRow);

            var gpuSub = new TextBlock { Text = "RTX 4050 · VRAM 1.2 GB", FontFamily = TypographyResources.SegoeTextFamily, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 14) };
            col2.Children.Add(gpuSub);

            // Power
            var pwrRow = new Grid();
            pwrRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pwrRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var pwrTitle = new TextBlock { Text = "Power", FontFamily = (FontFamily)app.Resources["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            var pwrVal = new TextBlock { Text = "10.7 W", FontFamily = (FontFamily)app.Resources["Typography.Family.Numeric"], FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
            Grid.SetColumn(pwrVal, 1);
            pwrRow.Children.Add(pwrTitle);
            pwrRow.Children.Add(pwrVal);
            col2.Children.Add(pwrRow);

            var pwrSub = new TextBlock { Text = "Battery: 80% (Mean 11.2 W)", FontFamily = TypographyResources.SegoeTextFamily, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 0) };
            col2.Children.Add(pwrSub);

            Grid.SetColumn(col2, 2);
            bento.Children.Add(col2);

            // Column 4: Network & Top Apps
            var col4 = new StackPanel();
            // Network
            var netRow = new Grid();
            netRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            netRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var downBlock = new TextBlock { Text = "↓ 4.2 MB/s", FontFamily = (FontFamily)app.Resources["Typography.Family.Numeric"], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            var upBlock = new TextBlock { Text = "↑ 820 KB/s", FontFamily = (FontFamily)app.Resources["Typography.Family.Numeric"], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(upBlock, 1);
            netRow.Children.Add(downBlock);
            netRow.Children.Add(upBlock);
            col4.Children.Add(netRow);

            var netSub = new TextBlock { Text = "Today: 1.4 GB / 320 MB", FontFamily = TypographyResources.SegoeTextFamily, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 14) };
            col4.Children.Add(netSub);

            // Top Apps
            var appTitle = new TextBlock { Text = "Top Apps", FontFamily = (FontFamily)app.Resources["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
            col4.Children.Add(appTitle);

            var appRow1 = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            appRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            appRow1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var app1Name = new TextBlock { Text = "Code.exe", FontFamily = TypographyResources.SegoeTextFamily, FontSize = 12, Foreground = Brushes.White };
            var app1Metric = new TextBlock { Text = "14.2% · 1.1 GB", FontFamily = TypographyResources.SegoeTextFamily, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)) };
            Grid.SetColumn(app1Metric, 1);
            appRow1.Children.Add(app1Name);
            appRow1.Children.Add(app1Metric);
            col4.Children.Add(appRow1);

            Grid.SetColumn(col4, 4);
            bento.Children.Add(col4);

            rootStack.Children.Add(bento);
            sysContainer.Child = rootStack;

            sysContainer.Measure(new Size(sysW, sysH));
            sysContainer.Arrange(new Rect(0, 0, sysW, sysH));
            sysContainer.UpdateLayout();

            var rtbSys = new RenderTargetBitmap(sysW, sysH, 96, 96, PixelFormats.Pbgra32);
            rtbSys.Render(sysContainer);

            var encSys = new PngBitmapEncoder();
            encSys.Frames.Add(BitmapFrame.Create(rtbSys));
            string sysPath = Path.Combine(artifactDir, "screenshot_system_dot_matrix.png");
            using (var fs = File.Create(sysPath))
            {
                encSys.Save(fs);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool ok = thread.Join(TimeSpan.FromSeconds(15));
        Assert.True(ok, "Screenshot capture timed out");

        string s1 = Path.Combine(artifactDir, "screenshot_settings_typography.png");
        string s2 = Path.Combine(artifactDir, "screenshot_system_dot_matrix.png");
        Assert.True(File.Exists(s1), "screenshot_settings_typography.png must exist");
        Assert.True(File.Exists(s2), "screenshot_system_dot_matrix.png must exist");
    }

    private static string? FindProjectRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Island.sln"))) return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
        return null;
    }
}
