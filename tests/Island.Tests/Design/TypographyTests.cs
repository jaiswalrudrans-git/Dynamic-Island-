using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Island.App.Design;
using Island.App.Header.Views;
using Island.Core.Settings;
using Xunit;

namespace Island.Tests;

public sealed class TypographyTests
{
    [Fact]
    public void PresetsContainRequiredRolesAndValidDefinitions()
    {
        Assert.NotEmpty(TypographyResources.Presets);
        Assert.True(TypographyResources.Presets.Length >= 12, "Must contain at least 12 typography presets");

        foreach (var preset in TypographyResources.Presets)
        {
            Assert.False(string.IsNullOrWhiteSpace(preset.Id));
            Assert.False(string.IsNullOrWhiteSpace(preset.DisplayName));
            Assert.NotNull(preset.Display);
            Assert.NotNull(preset.Heading);
            Assert.NotNull(preset.Body);
            Assert.NotNull(preset.Label);
            Assert.NotNull(preset.Numeric);
        }
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

    [Fact]
    public void LegibilityGuardEnforcesCompanionFontForBodyAndSmallLabels()
    {
        var res = new ResourceDictionary();
        var typography = new TypographyResources(res);

        // Force a decorative font on Body and small Label with guard ON
        var settings = new AppSettings
        {
            MatchTypographyToClock = false,
            TypographyPreset = "Dot Matrix",
            FontFamilyBody = TypographyResources.DotoFamily.Source,
            FontFamilyLabel = TypographyResources.SilkscreenFamily.Source,
            LegibilityGuard = true,
            TypographySizeScale = 1.0
        };

        typography.Apply(settings);

        var bodyFamily = (FontFamily)res["Typography.Family.Body"];
        var labelFamily = (FontFamily)res["Typography.Family.Label"];

        // Body must NOT be decorative
        Assert.False(TypographyResources.IsDecorative(bodyFamily), "Body font must never be decorative under Legibility Guard");
        // Small label (< 15 px) must NOT be decorative
        Assert.False(TypographyResources.IsDecorative(labelFamily), "Label font below threshold must never be decorative under Legibility Guard");
    }

    [Fact]
    public void EveryPresetAndRoleCombinationHasSufficientGlyphCoverage()
    {
        var res = new ResourceDictionary();
        var typography = new TypographyResources(res);

        foreach (var preset in TypographyResources.Presets)
        {
            var settings = new AppSettings
            {
                MatchTypographyToClock = false,
                TypographyPreset = preset.Id,
                LegibilityGuard = true
            };

            typography.Apply(settings);

            var heading = (FontFamily)res["Typography.Family.Heading"];
            var body = (FontFamily)res["Typography.Family.Body"];
            var label = (FontFamily)res["Typography.Family.Label"];

            // Heading, Body and Label must never be decorative
            Assert.False(TypographyResources.IsDecorative(heading), $"{preset.Id} Heading font must not be decorative");
            Assert.False(TypographyResources.IsDecorative(body), $"{preset.Id} Body font must not be decorative");
            Assert.False(TypographyResources.IsDecorative(label), $"{preset.Id} Label font must not be decorative");

            // Heading, Body and Label must have general text coverage (A-Z, a-z, 0-9, basic punctuation)
            Assert.True(TypographyResources.HasGeneralTextCoverage(heading), $"{preset.Id} Heading font must cover A-Z, a-z, 0-9");
            Assert.True(TypographyResources.HasGeneralTextCoverage(body), $"{preset.Id} Body font must cover A-Z, a-z, 0-9");
            Assert.True(TypographyResources.HasGeneralTextCoverage(label), $"{preset.Id} Label font must cover A-Z, a-z, 0-9");
        }
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

    [Theory]
    [InlineData("Cyan", 0x00, 0xE5, 0xFF)]
    [InlineData("Pink", 0xFF, 0x2E, 0x97)]
    [InlineData("Amber", 0xFF, 0xB3, 0x00)]
    [InlineData("Green", 0x00, 0xE6, 0x76)]
    public void ClockFaceResolvesExpectedClockColors(string colorName, byte r, byte g, byte b)
    {
        var settings = new AppSettings { ClockColor = colorName };
        var color = ClockFace.ResolveClockColor(settings);

        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }

    [Fact]
    public void GenerateTypographyVerificationArtifacts()
    {
        string artifactDir = @"C:\Users\jaisw\.gemini\antigravity\brain\979381a5-470e-44da-991a-1c21e51cf438";
        if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

        var thread = new Thread(() =>
        {
            TypographyResources.EnsurePackSchemeRegistered();

            string[] styles = ["DotMatrix", "Segment", "Serif", "Wide", "Condensed", "Pixel", "Neon"];
            (int TabIndex, string TabName)[] tabs =
            [
                (0, "Music"),
                (1, "System"),
                (4, "Focus")
            ];

            foreach (var style in styles)
            {
                var settings = new AppSettings
                {
                    ClockStyle = style,
                    MatchTypographyToClock = true,
                    MatchAccentToClock = true
                };

                var res = new ResourceDictionary();
                var typography = new TypographyResources(res);
                var clockColor = ClockFace.ResolveClockColor(settings);
                typography.Apply(settings, clockColor);

                foreach (var (tabIndex, tabName) in tabs)
                {
                    int width = 680;
                    int height = tabIndex == 1 ? 260 : tabIndex == 4 ? 240 : 180;

                    var container = new Border
                    {
                        Width = width,
                        Height = height,
                        Background = new SolidColorBrush(Color.FromArgb(245, 10, 10, 15)),
                        CornerRadius = new CornerRadius(16),
                        Padding = new Thickness(16)
                    };

                    foreach (var key in res.Keys)
                    {
                        container.Resources[key] = res[key];
                    }

                    var header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 12)
                    };

                    var clockFace = new ClockFace { ClockStyleProp = style };
                    clockFace.ApplySettings(settings, clockColor);
                    header.Children.Add(clockFace);

                    var bodyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                    bodyStack.Children.Add(header);

                    if (tabName == "Music")
                    {
                        var title = new TextBlock
                        {
                            Text = "Menu Samjha - Diljit Dosanjh",
                            FontFamily = (FontFamily)res["Typography.Family.Heading"],
                            FontSize = (double)res["Typography.Size.SectionTitle"],
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        var artist = new TextBlock
                        {
                            Text = "Aleemrk, Umair · Playing via Spotify",
                            FontFamily = (FontFamily)res["Typography.Family.Body"],
                            FontSize = (double)res["Typography.Size.Secondary"],
                            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 4, 0, 10)
                        };
                        var timeRow = new TextBlock
                        {
                            Text = "0:19  ───────●──────────────────  3:17",
                            FontFamily = (FontFamily)res["Typography.Family.Numeric"],
                            FontSize = (double)res["Typography.Size.Label"],
                            Foreground = new SolidColorBrush(clockColor),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        bodyStack.Children.Add(title);
                        bodyStack.Children.Add(artist);
                        bodyStack.Children.Add(timeRow);
                    }
                    else if (tabName == "System")
                    {
                        var row1 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 6) };
                        row1.Children.Add(new TextBlock { Text = "CPU: 6% (16 Cores)  ·  ", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        row1.Children.Add(new TextBlock { Text = "GPU: RTX 4050 0% · 60°C  ·  ", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        row1.Children.Add(new TextBlock { Text = "RAM: 7.5 GB / 15.7 GB (48%)", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(clockColor) });

                        var row2 = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 6) };
                        row2.Children.Add(new TextBlock { Text = "↓ 147.1 KB/s   ↑ 6.7 KB/s   ·   Power: 10.7 W (Battery: 80%)", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 200)) });

                        var row3 = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
                        row3.Children.Add(new TextBlock { Text = "Top Apps (RAM): agy (458 MB) | Island (356 MB) | Antigravity IDE (280 MB)", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 180)) });

                        bodyStack.Children.Add(row1);
                        bodyStack.Children.Add(row2);
                        bodyStack.Children.Add(row3);
                    }
                    else // Focus
                    {
                        var mode = new TextBlock
                        {
                            Text = "POMODORO  ·  WORK 1 OF 4",
                            FontFamily = (FontFamily)res["Typography.Family.Heading"],
                            FontSize = (double)res["Typography.Size.Secondary"],
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(clockColor),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 4, 0, 8)
                        };
                        var timer = new TextBlock
                        {
                            Text = "25:00",
                            FontFamily = (FontFamily)res["Typography.Family.Display"],
                            FontSize = 48,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 4, 0, 10)
                        };
                        var controls = new TextBlock
                        {
                            Text = "[ Reset ]     ▶  PLAY     [ Skip ]",
                            FontFamily = (FontFamily)res["Typography.Family.Heading"],
                            FontSize = 14,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        bodyStack.Children.Add(mode);
                        bodyStack.Children.Add(timer);
                        bodyStack.Children.Add(controls);
                    }

                    container.Child = bodyStack;

                    container.Measure(new Size(width, height));
                    container.Arrange(new Rect(0, 0, width, height));
                    container.UpdateLayout();

                    var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(container);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    string filePath = Path.Combine(artifactDir, $"verify_{style}_{tabName}.png");
                    using var stream = File.Create(filePath);
                    encoder.Save(stream);
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool finished = thread.Join(TimeSpan.FromSeconds(30));
        Assert.True(finished, "Artifact generation thread timed out");

        string[] testStyles = ["DotMatrix", "Segment", "Serif", "Wide", "Condensed", "Pixel", "Neon"];
        string[] testTabs = ["Music", "System", "Focus"];
        foreach (var s in testStyles)
        {
            foreach (var t in testTabs)
            {
                string expectedFile = Path.Combine(artifactDir, $"verify_{s}_{t}.png");
                Assert.True(File.Exists(expectedFile), $"Expected artifact {expectedFile} was not generated");
                Assert.True(new FileInfo(expectedFile).Length > 1000, $"Artifact {expectedFile} is too small");
            }
        }
    }

    [Fact]
    public void DesignTokensDoesNotContainImplicitTextBlockStyleToProtectIconButtons()
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string xamlPath = Path.Combine(projectRoot, "src", "Island.App", "Design", "DesignTokens.xaml");
        string xaml = File.ReadAllText(xamlPath);

        // An implicit style <Style TargetType="TextBlock"> overrides button icon textblocks and breaks glyphs!
        Assert.DoesNotContain("<Style TargetType=\"TextBlock\">", xaml);
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
        Assert.Contains("Typography.Family.Label", xaml);
        Assert.Contains("Typography.Size.SectionTitle", xaml);
        Assert.Contains("Typography.Size.Value", xaml);
        Assert.Contains("BALANCED BENTO GRID", xaml);
    }

    [Fact]
    public void FocusPageProtectsButtonGlyphsAndGracefullyConstrainsTimer()
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string focusXamlPath = Directory.GetFiles(Path.Combine(projectRoot, "src", "Island.App"), "FocusPage.xaml", SearchOption.AllDirectories).First();
        string focusXaml = File.ReadAllText(focusXamlPath);

        // Buttons must use Segoe Fluent Icons text blocks directly
        Assert.Contains("&#xE777;", focusXaml); // Reset glyph
        Assert.Contains("Segoe Fluent Icons", focusXaml);
        Assert.Contains("Property=\"MaxWidth\" Value=\"92\"", focusXaml); // Safe margin inside ring
        Assert.Contains("Property=\"MaxHeight\" Value=\"32\"", focusXaml);
        Assert.Contains("IsPomodoro", focusXaml); // Subtitle hidden in timer/stopwatch
    }

    [Fact]
    public void OverlayWindowIconButtonUsesContentPresenterForDirectUIRendering()
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string overlayXamlPath = Directory.GetFiles(Path.Combine(projectRoot, "src", "Island.App"), "OverlayWindow.xaml", SearchOption.AllDirectories).First();
        string overlayXaml = File.ReadAllText(overlayXamlPath);

        // IconButton must use ContentPresenter so WPF renders UIElement iconBlock directly without .ToString()
        Assert.Contains("<ContentPresenter HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"/>", overlayXaml);
        Assert.Contains("&#xE718;", overlayXaml); // Pin
        Assert.Contains("&#xE713;", overlayXaml); // Settings
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
