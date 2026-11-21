using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Island.App.Themes;
using Island.App.Views;
using Island.Core.Settings;
using Xunit;

namespace Island.Tests;

public sealed class AuditCaptureRunner
{
    [Fact]
    public void GenerateStageAAuditScreenshots()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\.."));
        string auditScreensDir = Path.Combine(rootDir, "docs", "screens", "audit");
        Directory.CreateDirectory(auditScreensDir);

        var thread = new Thread(() =>
        {
            TypographyResources.EnsurePackSchemeRegistered();

            string[] presets = ["Classic", "Dot Matrix", "LED"];
            (double scale, int dpi, string dpiLabel)[] dpis = [(1.0, 96, "100dpi"), (1.25, 120, "125dpi")];
            (double transPct, byte alpha, string transLabel)[] transparencies =
            [
                (0.0, 255, "trans0"),
                (0.12, 224, "trans12"),
                (0.50, 128, "trans50")
            ];

            string[] tabs = ["Music", "System", "Tasks", "QuickControls", "Focus", "CalendarWeather", "Clipboard"];

            foreach (var preset in presets)
            {
                foreach (var (dpiScale, dpiVal, dpiLabel) in dpis)
                {
                    foreach (var (transPct, alphaVal, transLabel) in transparencies)
                    {
                        var settings = new AppSettings
                        {
                            TypographyPreset = preset,
                            MatchTypographyToClock = false,
                            LegibilityGuard = true,
                            BackgroundTransparency = transPct * 100.0,
                            SurfaceStyle = transLabel switch { "trans0" => "Opaque", "trans12" => "Translucent", _ => "Clear" }
                        };

                        var res = new ResourceDictionary();
                        var typography = new TypographyResources(res);
                        typography.Apply(settings);

                        foreach (var tab in tabs)
                        {
                            int widthDip = tab == "System" || tab == "CalendarWeather" || tab == "Clipboard" ? 680 : 580;
                            int heightDip = tab switch
                            {
                                "System" => 380,
                                "CalendarWeather" => 340,
                                "Focus" => 300,
                                "QuickControls" => 260,
                                "Tasks" => 220,
                                "Clipboard" => 280,
                                _ => 180 // Music
                            };

                            var islandShell = new Border
                            {
                                Width = widthDip,
                                Height = heightDip,
                                Background = new SolidColorBrush(Color.FromArgb(alphaVal, 10, 12, 16)),
                                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(16),
                                Padding = new Thickness(12)
                            };

                            foreach (var key in res.Keys)
                            {
                                islandShell.Resources[key] = res[key];
                            }

                            var layout = new Grid();
                            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); // Row 0: Clock & Actions
                            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // Row 1: Tab bar
                            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

                            // Row 0 Header: Clock & Actions
                            var headerRow0 = new Grid();
                            headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
                            headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });

                            var clockStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            var clockFace = new ClockFace { ClockStyleProp = preset == "Dot Matrix" ? "DotMatrix" : preset == "LED" ? "Segment" : "Simple" };
                            clockFace.ApplySettings(settings, Color.FromRgb(0, 229, 255));
                            clockStack.Children.Add(clockFace);
                            var dateText = new TextBlock
                            {
                                Text = "Fri, Nov 21",
                                FontFamily = (FontFamily)res["Typography.Family.Body"],
                                FontSize = (double)res["Typography.Size.Secondary"],
                                Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)),
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            clockStack.Children.Add(dateText);
                            Grid.SetColumn(clockStack, 1);
                            headerRow0.Children.Add(clockStack);

                            // Pin / Gear icons
                            var actionStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                            actionStack.Children.Add(new TextBlock { Text = "\uE718", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 13, Foreground = Brushes.LightGray, Margin = new Thickness(4, 0, 4, 0) });
                            actionStack.Children.Add(new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 13, Foreground = Brushes.LightGray, Margin = new Thickness(4, 0, 0, 0) });
                            Grid.SetColumn(actionStack, 2);
                            headerRow0.Children.Add(actionStack);
                            Grid.SetRow(headerRow0, 0);
                            layout.Children.Add(headerRow0);

                            // Row 1 Tab Bar
                            var tabBar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            string[] tabGlyphs = ["\uE8D6", "\uE950", "\uE9D5", "\uE9E9", "\uE916", "\uE787", "\uE8C8"];
                            string[] tabNames = ["Music", "System", "Tasks", "Controls", "Focus", "Calendar", "Clipboard"];
                            for (int i = 0; i < tabNames.Length; i++)
                            {
                                bool isCur = tabNames[i].Equals(tab, StringComparison.OrdinalIgnoreCase) || (tab == "QuickControls" && tabNames[i] == "Controls") || (tab == "CalendarWeather" && tabNames[i] == "Calendar");
                                var btn = new Border
                                {
                                    Background = isCur ? new SolidColorBrush(Color.FromArgb(50, 0, 229, 255)) : Brushes.Transparent,
                                    CornerRadius = new CornerRadius(6),
                                    Padding = new Thickness(isCur ? 8 : 6, 4, isCur ? 8 : 6, 4),
                                    Margin = new Thickness(2, 0, 2, 0)
                                };
                                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                                sp.Children.Add(new TextBlock { Text = tabGlyphs[i], FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 13, Foreground = isCur ? new SolidColorBrush(Color.FromRgb(0, 229, 255)) : new SolidColorBrush(Color.FromRgb(140, 150, 165)) });
                                if (isCur)
                                {
                                    sp.Children.Add(new TextBlock { Text = tabNames[i], FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(4, 0, 0, 0) });
                                }
                                btn.Child = sp;
                                tabBar.Children.Add(btn);
                            }
                            Grid.SetRow(tabBar, 1);
                            layout.Children.Add(tabBar);

                            // Row 2 Content
                            var contentHost = new Border { Margin = new Thickness(0, 8, 0, 0), ClipToBounds = true };
                            var contentStack = new StackPanel();

                            if (tab == "Music")
                            {
                                var title = new TextBlock { Text = "Menu Samjha - Diljit Dosanjh", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center };
                                var artist = new TextBlock { Text = "Aleemrk, Umair · Spotify", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(150, 155, 170)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 8) };
                                var bar = new TextBlock { Text = "0:19  ──────●─────────────  3:17", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                                contentStack.Children.Add(title);
                                contentStack.Children.Add(artist);
                                contentStack.Children.Add(bar);
                            }
                            else if (tab == "System")
                            {
                                var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
                                row1.Children.Add(new TextBlock { Text = "CPU 12% (16 Cores)  ·  ", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, Foreground = Brushes.White });
                                row1.Children.Add(new TextBlock { Text = "GPU RTX 4050 0% · 60°C  ·  ", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, Foreground = Brushes.White });
                                row1.Children.Add(new TextBlock { Text = "RAM 7.5 GB / 15.7 GB (48%)", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) });

                                var row2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
                                row2.Children.Add(new TextBlock { Text = "Today: 34.0 MB down · 141.5 KB up  ·  Power: 10.7 W", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12.5, Foreground = new SolidColorBrush(Color.FromRgb(160, 165, 180)) });

                                var row3 = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
                                row3.Children.Add(new TextBlock { Text = "Top Apps: agy (458 MB) | Island (356 MB) | Antigravity IDE (280 MB)", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 145, 160)) });

                                contentStack.Children.Add(row1);
                                contentStack.Children.Add(row2);
                                contentStack.Children.Add(row3);
                            }
                            else if (tab == "Focus")
                            {
                                var mode = new TextBlock { Text = "POMODORO · WORK 1 OF 4", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                                var timer = new TextBlock { Text = "25:00", FontFamily = (FontFamily)res["Typography.Family.Display"], FontSize = 44, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 6) };
                                var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                                actions.Children.Add(new Border { Width = 36, Height = 36, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), CornerRadius = new CornerRadius(6), Child = new TextBlock { Text = "\uE777", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                                actions.Children.Add(new Border { Width = 44, Height = 44, Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)), CornerRadius = new CornerRadius(10), Margin = new Thickness(12, 0, 12, 0), Child = new TextBlock { Text = "\uE768", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 18, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                                actions.Children.Add(new Border { Width = 36, Height = 36, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), CornerRadius = new CornerRadius(6), Child = new TextBlock { Text = "\uE893", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                                contentStack.Children.Add(mode);
                                contentStack.Children.Add(timer);
                                contentStack.Children.Add(actions);
                            }
                            else
                            {
                                contentStack.Children.Add(new TextBlock { Text = $"{tab} Preview", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
                                contentStack.Children.Add(new TextBlock { Text = "Active sample preview content for audit verification", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(150, 155, 170)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) });
                            }

                            contentHost.Child = contentStack;
                            Grid.SetRow(contentHost, 2);
                            layout.Children.Add(contentHost);

                            islandShell.Child = layout;

                            int pixelW = (int)Math.Round(widthDip * dpiScale);
                            int pixelH = (int)Math.Round(heightDip * dpiScale);

                            islandShell.Measure(new Size(widthDip, heightDip));
                            islandShell.Arrange(new Rect(0, 0, widthDip, heightDip));
                            islandShell.UpdateLayout();

                            var rtb = new RenderTargetBitmap(pixelW, pixelH, dpiVal, dpiVal, PixelFormats.Pbgra32);
                            rtb.Render(islandShell);

                            string fileName = $"{preset}_{dpiLabel}_{transLabel}_{tab}.png";
                            string outPath = Path.Combine(auditScreensDir, fileName);
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(rtb));
                            using var fs = File.Create(outPath);
                            encoder.Save(fs);
                        }
                    }
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool ok = thread.Join(TimeSpan.FromSeconds(60));
        Assert.True(ok, "Audit screen capture timed out");

        int captured = Directory.GetFiles(auditScreensDir, "*.png").Length;
        Assert.True(captured > 50, $"Expected >50 audit screenshots, got {captured}");
    }
}
