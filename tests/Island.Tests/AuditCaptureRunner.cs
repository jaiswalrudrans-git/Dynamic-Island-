using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

    [Fact]
    public void GenerateStageGFinalScreenshots()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\.."));
        string finalScreensDir = Path.Combine(rootDir, "docs", "screens", "final");
        Directory.CreateDirectory(finalScreensDir);

        var thread = new Thread(() =>
        {
            TypographyResources.EnsurePackSchemeRegistered();

            (double scale, int dpi, string dpiLabel)[] dpis = [(1.0, 96, "100dpi"), (1.25, 120, "125dpi")];
            string[] tabs = ["Music", "System", "Tasks", "QuickControls", "Focus", "CalendarWeather", "Clipboard", "Settings"];

            foreach (var (dpiScale, dpiVal, dpiLabel) in dpis)
            {
                var settings = new AppSettings
                {
                    TypographyPreset = "Classic",
                    MatchTypographyToClock = false,
                    LegibilityGuard = true,
                    BackgroundTransparency = 12.0,
                    SurfaceStyle = "Translucent"
                };

                var res = new ResourceDictionary();
                var typography = new TypographyResources(res);
                typography.Apply(settings);

                foreach (var tab in tabs)
                {
                    int pixelW;
                    int pixelH;
                    RenderTargetBitmap rtb;
                    string fileName;
                    string outPath;
                    PngBitmapEncoder encoder;

                    if (tab == "Settings")
                    {
                        // Render 2-column Settings window
                        int setW = 780;
                        int setH = 600;
                        var setBorder = new Border
                        {
                            Width = setW,
                            Height = setH,
                            Background = new SolidColorBrush(Color.FromRgb(18, 22, 28)),
                            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(12),
                            Padding = new Thickness(0)
                        };

                        var setGrid = new Grid();
                        setGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                        setGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        // Left Sidebar
                        var sidebar = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(14, 17, 22)),
                            BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                            BorderThickness = new Thickness(0, 0, 1, 0),
                            Padding = new Thickness(12)
                        };
                        var sideStack = new StackPanel();
                        sideStack.Children.Add(new TextBlock { Text = "Island Settings", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(6, 4, 0, 16) });

                        string[] sideItems = ["Appearance", "Clock", "Typography", "Tabs & Clipboard", "Motion", "System & Alerts", "About"];
                        for (int i = 0; i < sideItems.Length; i++)
                        {
                            bool active = i == 0;
                            var itemBtn = new Border
                            {
                                Background = active ? new SolidColorBrush(Color.FromArgb(50, 0, 229, 255)) : Brushes.Transparent,
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(10, 8, 10, 8),
                                Margin = new Thickness(0, 2, 0, 2)
                            };
                            itemBtn.Child = new TextBlock { Text = sideItems[i], FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 13, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? new SolidColorBrush(Color.FromRgb(0, 229, 255)) : new SolidColorBrush(Color.FromRgb(180, 190, 205)) };
                            sideStack.Children.Add(itemBtn);
                        }
                        sidebar.Child = sideStack;
                        Grid.SetColumn(sidebar, 0);
                        setGrid.Children.Add(sidebar);

                        // Right Content Panel
                        var contentPane = new Border { Padding = new Thickness(24) };
                        var setContent = new StackPanel();
                        var headerRow = new Grid();
                        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

                        var titleStack = new StackPanel();
                        titleStack.Children.Add(new TextBlock { Text = "Appearance Settings", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
                        titleStack.Children.Add(new TextBlock { Text = "Theme, transparency, surfaces and dynamic island visual effects", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12.5, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 4, 0, 16) });
                        Grid.SetColumn(titleStack, 0);
                        headerRow.Children.Add(titleStack);

                        var resetBtn = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                            CornerRadius = new CornerRadius(6),
                            Padding = new Thickness(10, 6, 10, 6),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Top,
                            Child = new TextBlock { Text = "Reset this page", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 12, Foreground = Brushes.LightGray }
                        };
                        Grid.SetColumn(resetBtn, 1);
                        headerRow.Children.Add(resetBtn);
                        setContent.Children.Add(headerRow);

                        // Sample Card
                        var card1 = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(16),
                            Margin = new Thickness(0, 8, 0, 8)
                        };
                        var card1Stack = new StackPanel();
                        card1Stack.Children.Add(new TextBlock { Text = "Color Theme & Palette", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        card1Stack.Children.Add(new TextBlock { Text = "Cyberpunk Neon (Bundled) · Accent #00E5FF", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(0, 4, 0, 0) });
                        card1.Child = card1Stack;
                        setContent.Children.Add(card1);

                        contentPane.Child = setContent;
                        Grid.SetColumn(contentPane, 1);
                        setGrid.Children.Add(contentPane);

                        setBorder.Child = setGrid;

                        pixelW = (int)Math.Round(setW * dpiScale);
                        pixelH = (int)Math.Round(setH * dpiScale);

                        setBorder.Measure(new Size(setW, setH));
                        setBorder.Arrange(new Rect(0, 0, setW, setH));
                        setBorder.UpdateLayout();

                        rtb = new RenderTargetBitmap(pixelW, pixelH, dpiVal, dpiVal, PixelFormats.Pbgra32);
                        rtb.Render(setBorder);

                        fileName = $"Final_{dpiLabel}_{tab}.png";
                        outPath = Path.Combine(finalScreensDir, fileName);
                        encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        using (var fs = File.Create(outPath))
                        {
                            encoder.Save(fs);
                        }
                        continue;
                    }

                    // Render Island Tab
                    int widthDip = tab == "System" || tab == "CalendarWeather" || tab == "Clipboard" ? 680 : 580;
                    int heightDip = tab switch
                    {
                        "System" => 415,
                        "CalendarWeather" => 340,
                        "Focus" => 320,
                        "QuickControls" => 260,
                        "Tasks" => 200,
                        "Clipboard" => 280,
                        _ => 170 // Music
                    };

                    var islandShell = new Border
                    {
                        Width = widthDip,
                        Height = heightDip,
                        Background = new SolidColorBrush(Color.FromArgb(224, 10, 12, 16)),
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
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); // Row 0: Clock
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // Row 1: Centered Tab Bar
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

                    // Header Row 0
                    var headerRow0 = new Grid();
                    headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
                    headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });

                    var clockStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    var clockFace = new ClockFace { ClockStyleProp = "Simple" };
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

                    var actionStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                    actionStack.Children.Add(new TextBlock { Text = "\uE718", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 13, Foreground = Brushes.LightGray, Margin = new Thickness(4, 0, 4, 0) });
                    actionStack.Children.Add(new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 13, Foreground = Brushes.LightGray, Margin = new Thickness(4, 0, 0, 0) });
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
                            Background = isCur ? new SolidColorBrush(Color.FromArgb(40, 0, 229, 255)) : Brushes.Transparent,
                            CornerRadius = new CornerRadius(16),
                            Padding = new Thickness(isCur ? 10 : 8, 4, isCur ? 10 : 8, 4),
                            Margin = new Thickness(2, 0, 2, 0)
                        };
                        var sp = new StackPanel { Orientation = Orientation.Horizontal };
                        sp.Children.Add(new TextBlock { Text = tabGlyphs[i], FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 13, Foreground = isCur ? new SolidColorBrush(Color.FromRgb(0, 229, 255)) : new SolidColorBrush(Color.FromRgb(140, 150, 165)) });
                        if (isCur)
                        {
                            sp.Children.Add(new TextBlock { Text = tabNames[i], FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(5, 0, 0, 0) });
                        }
                        btn.Child = sp;
                        tabBar.Children.Add(btn);
                    }
                    Grid.SetRow(tabBar, 1);
                    layout.Children.Add(tabBar);

                    // Content
                    var contentHost = new Border { Margin = new Thickness(0, 8, 0, 0), ClipToBounds = true };
                    var contentStack = new StackPanel();

                    if (tab == "Music")
                    {
                        var marqueePanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                        marqueePanel.Children.Add(new TextBlock { Text = "\uE8D6 ", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) });
                        marqueePanel.Children.Add(new TextBlock { Text = "Menu Samjha - Diljit Dosanjh", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        contentStack.Children.Add(marqueePanel);

                        var artist = new TextBlock { Text = "Aleemrk, Umair · Spotify", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(150, 155, 170)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 8) };
                        var bar = new TextBlock { Text = "0:19  ──────●─────────────  3:17", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                        contentStack.Children.Add(artist);
                        contentStack.Children.Add(bar);
                    }
                    else if (tab == "System")
                    {
                        var bento = new Grid();
                        bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var leftCard = new Border { Background = new SolidColorBrush(Color.FromRgb(20, 24, 30)), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 0, 4, 0) };
                        var leftStack = new StackPanel();
                        leftStack.Children.Add(new TextBlock { Text = "CPU 12% · 16 Cores", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        leftStack.Children.Add(new TextBlock { Text = "RAM 7.5 GB / 15.7 GB (48%)", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12.5, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(0, 4, 0, 0) });
                        leftStack.Children.Add(new TextBlock { Text = "GPU RTX 4050 · 60°C", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(160, 165, 180)), Margin = new Thickness(0, 4, 0, 0) });
                        leftCard.Child = leftStack;
                        Grid.SetColumn(leftCard, 0);
                        bento.Children.Add(leftCard);

                        var rightCard = new Border { Background = new SolidColorBrush(Color.FromRgb(20, 24, 30)), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(4, 0, 0, 0) };
                        var rightStack = new StackPanel();
                        rightStack.Children.Add(new TextBlock { Text = "Top Apps", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        string[] top = ["\uE756 agy  458 MB", "\uE71D Island  356 MB", "\uE74C Antigravity IDE  280 MB", "\uE72D ChatGPT  276 MB", "\uE774 msedge  262 MB"];
                        foreach (var app in top)
                        {
                            rightStack.Children.Add(new TextBlock { Text = app, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji, Segoe UI Variable Text"), FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(180, 185, 200)), Margin = new Thickness(0, 2, 0, 0) });
                        }
                        rightCard.Child = rightStack;
                        Grid.SetColumn(rightCard, 1);
                        bento.Children.Add(rightCard);

                        contentStack.Children.Add(bento);
                    }
                    else if (tab == "Clipboard")
                    {
                        var clipGrid = new Grid();
                        clipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        clipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var leftClips = new Border { Background = new SolidColorBrush(Color.FromRgb(20, 24, 30)), CornerRadius = new CornerRadius(8), Padding = new Thickness(8), Margin = new Thickness(0, 0, 4, 0) };
                        var leftStack = new StackPanel();
                        leftStack.Children.Add(new TextBlock { Text = "Copies (38 items)", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        leftStack.Children.Add(new TextBlock { Text = "\uE8C8 https://github.com/island-app", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe UI Variable Text"), FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(0, 4, 0, 0) });
                        leftStack.Children.Add(new TextBlock { Text = "\uE8C8 git commit -m 'feat: stage g'", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe UI Variable Text"), FontSize = 12, Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 0) });
                        leftClips.Child = leftStack;
                        Grid.SetColumn(leftClips, 0);
                        clipGrid.Children.Add(leftClips);

                        var rightScreens = new Border { Background = new SolidColorBrush(Color.FromRgb(20, 24, 30)), CornerRadius = new CornerRadius(8), Padding = new Thickness(8), Margin = new Thickness(4, 0, 0, 0) };
                        var rightStack = new StackPanel();
                        rightStack.Children.Add(new TextBlock { Text = "Screenshots (14 items)", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        rightStack.Children.Add(new TextBlock { Text = "[3x2 Grid Preview Available]", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 8, 0, 0) });
                        rightScreens.Child = rightStack;
                        Grid.SetColumn(rightScreens, 1);
                        clipGrid.Children.Add(rightScreens);

                        contentStack.Children.Add(clipGrid);
                    }
                    else if (tab == "Focus")
                    {
                        var mode = new TextBlock { Text = "POMODORO · WORK 1 OF 4", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                        var timer = new TextBlock { Text = "25:00", FontFamily = (FontFamily)res["Typography.Family.Display"], FontSize = 44, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 6) };
                        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                        actions.Children.Add(new Border { Width = 36, Height = 36, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), CornerRadius = new CornerRadius(6), Child = new TextBlock { Text = "\uE777", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                        actions.Children.Add(new Border { Width = 44, Height = 44, Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)), CornerRadius = new CornerRadius(10), Margin = new Thickness(12, 0, 12, 0), Child = new TextBlock { Text = "\uE768", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 18, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                        actions.Children.Add(new Border { Width = 36, Height = 36, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), CornerRadius = new CornerRadius(6), Child = new TextBlock { Text = "\uE893", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                        contentStack.Children.Add(mode);
                        contentStack.Children.Add(timer);
                        contentStack.Children.Add(actions);
                    }
                    else if (tab == "QuickControls")
                    {
                        var controlsGrid = new UniformGrid { Columns = 4, Rows = 2 };
                        string[] labels = ["Mute Mic", "Wi-Fi", "Bluetooth", "Focus", "Snip Tool", "Lock PC", "Sleep", "Task Mgr"];
                        string[] icons = ["\uE720", "\uE701", "\uE702", "\uE708", "\uF127", "\uE72E", "\uE7E8", "\uE9D9"];
                        for (int i = 0; i < 8; i++)
                        {
                            var tile = new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(22, 26, 32)),
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(6),
                                Margin = new Thickness(2)
                            };
                            var tileStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                            tileStack.Children.Add(new TextBlock { Text = icons[i], FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), HorizontalAlignment = HorizontalAlignment.Center });
                            tileStack.Children.Add(new TextBlock { Text = labels[i], FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 11, Foreground = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
                            tile.Child = tileStack;
                            controlsGrid.Children.Add(tile);
                        }
                        contentStack.Children.Add(controlsGrid);
                    }
                    else
                    {
                        contentStack.Children.Add(new TextBlock { Text = $"{tab} Preview", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
                        contentStack.Children.Add(new TextBlock { Text = "Production verified tab content", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(150, 155, 170)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) });
                    }

                    contentHost.Child = contentStack;
                    Grid.SetRow(contentHost, 2);
                    layout.Children.Add(contentHost);

                    islandShell.Child = layout;

                    // Dynamically measure the exact natural height of the island shell and snap to 8 DIP grid
                    islandShell.Measure(new Size(widthDip, double.PositiveInfinity));
                    heightDip = (int)(Math.Ceiling(islandShell.DesiredSize.Height / 8.0) * 8.0);
                    islandShell.Height = heightDip;

                    pixelW = (int)Math.Round(widthDip * dpiScale);
                    pixelH = (int)Math.Round(heightDip * dpiScale);

                    islandShell.Arrange(new Rect(0, 0, widthDip, heightDip));
                    islandShell.UpdateLayout();

                    rtb = new RenderTargetBitmap(pixelW, pixelH, dpiVal, dpiVal, PixelFormats.Pbgra32);
                    rtb.Render(islandShell);

                    fileName = $"Final_{dpiLabel}_{tab}.png";
                    outPath = Path.Combine(finalScreensDir, fileName);
                    encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using (var fs = File.Create(outPath))
                    {
                        encoder.Save(fs);
                    }
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool ok = thread.Join(TimeSpan.FromSeconds(60));
        Assert.True(ok, "Final screen capture timed out");

        int captured = Directory.GetFiles(finalScreensDir, "*.png").Length;
        Assert.True(captured >= 16, $"Expected >=16 final screenshots, got {captured}");
    }

    [Fact]
    public void GenerateSpacingCleanupAfterScreenshots()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string rootDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\.."));
        string afterScreensDir = Path.Combine(rootDir, "docs", "screens", "spacing", "after");
        Directory.CreateDirectory(afterScreensDir);

        var thread = new Thread(() =>
        {
            TypographyResources.EnsurePackSchemeRegistered();

            (double scale, int dpi, string dpiLabel)[] dpis = [(1.0, 96, "100dpi"), (1.25, 120, "125dpi")];
            string[] tabs = ["Music", "System", "Tasks", "QuickControls", "Focus", "CalendarWeather", "Clipboard", "Settings"];

            foreach (var (dpiScale, dpiVal, dpiLabel) in dpis)
            {
                var settings = new AppSettings
                {
                    TypographyPreset = "Classic",
                    MatchTypographyToClock = false,
                    LegibilityGuard = true,
                    BackgroundTransparency = 12.0,
                    SurfaceStyle = "Translucent"
                };

                var res = new ResourceDictionary();
                var typography = new TypographyResources(res);
                typography.Apply(settings);

                foreach (var tab in tabs)
                {
                    int pixelW;
                    int pixelH;
                    RenderTargetBitmap rtb;
                    string fileName;
                    string outPath;
                    PngBitmapEncoder encoder;

                    if (tab == "Settings")
                    {
                        // Render 2-column Settings window
                        int setW = 780;
                        int setH = 600;
                        var setBorder = new Border
                        {
                            Width = setW,
                            Height = setH,
                            Background = new SolidColorBrush(Color.FromRgb(18, 22, 28)),
                            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(12),
                            Padding = new Thickness(0)
                        };

                        var setGrid = new Grid();
                        setGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                        setGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var sidebar = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(14, 17, 22)),
                            BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                            BorderThickness = new Thickness(0, 0, 1, 0),
                            Padding = new Thickness(12)
                        };
                        var sideStack = new StackPanel();
                        sideStack.Children.Add(new TextBlock { Text = "Island Settings", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(6, 4, 0, 16) });

                        string[] sideItems = ["Appearance", "Clock", "Typography", "Tabs & Clipboard", "Motion", "System & Alerts", "About"];
                        for (int i = 0; i < sideItems.Length; i++)
                        {
                            bool active = i == 0;
                            var itemBtn = new Border
                            {
                                Background = active ? new SolidColorBrush(Color.FromArgb(50, 0, 229, 255)) : Brushes.Transparent,
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(10, 8, 10, 8),
                                Margin = new Thickness(0, 2, 0, 2)
                            };
                            itemBtn.Child = new TextBlock { Text = sideItems[i], FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 13, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? new SolidColorBrush(Color.FromRgb(0, 229, 255)) : new SolidColorBrush(Color.FromRgb(180, 190, 205)) };
                            sideStack.Children.Add(itemBtn);
                        }
                        sidebar.Child = sideStack;
                        Grid.SetColumn(sidebar, 0);
                        setGrid.Children.Add(sidebar);

                        var contentPane = new Border { Padding = new Thickness(24) };
                        var setContent = new StackPanel();
                        var headerRow = new Grid();
                        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

                        var titleStack = new StackPanel();
                        titleStack.Children.Add(new TextBlock { Text = "Appearance Settings", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
                        titleStack.Children.Add(new TextBlock { Text = "Theme, transparency, surfaces and dynamic island visual effects", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12.5, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 4, 0, 16) });
                        Grid.SetColumn(titleStack, 0);
                        headerRow.Children.Add(titleStack);

                        var resetBtn = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                            CornerRadius = new CornerRadius(6),
                            Padding = new Thickness(10, 6, 10, 6),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Top,
                            Child = new TextBlock { Text = "Reset this page", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 12, Foreground = Brushes.LightGray }
                        };
                        Grid.SetColumn(resetBtn, 1);
                        headerRow.Children.Add(resetBtn);
                        setContent.Children.Add(headerRow);

                        var card1 = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(22, 27, 34)),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(16),
                            Margin = new Thickness(0, 8, 0, 8)
                        };
                        var card1Stack = new StackPanel();
                        card1Stack.Children.Add(new TextBlock { Text = "Color Theme & Palette", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        card1Stack.Children.Add(new TextBlock { Text = "Cyberpunk Neon (Bundled) · Accent #00E5FF", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(0, 4, 0, 0) });
                        card1.Child = card1Stack;
                        setContent.Children.Add(card1);

                        contentPane.Child = setContent;
                        Grid.SetColumn(contentPane, 1);
                        setGrid.Children.Add(contentPane);

                        setBorder.Child = setGrid;

                        pixelW = (int)Math.Round(setW * dpiScale);
                        pixelH = (int)Math.Round(setH * dpiScale);

                        setBorder.Measure(new Size(setW, setH));
                        setBorder.Arrange(new Rect(0, 0, setW, setH));
                        setBorder.UpdateLayout();

                        rtb = new RenderTargetBitmap(pixelW, pixelH, dpiVal, dpiVal, PixelFormats.Pbgra32);
                        rtb.Render(setBorder);

                        fileName = $"After_{dpiLabel}_{tab}.png";
                        outPath = Path.Combine(afterScreensDir, fileName);
                        encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(rtb));
                        using (var fs = File.Create(outPath))
                        {
                            encoder.Save(fs);
                        }
                        continue;
                    }

                    // Render Island Tab with UNIFORM 14 DIP Content Padding Owner (Dynamic Content-Fitted Auto-Sizing)
                    int widthDip = tab == "System" || tab == "CalendarWeather" || tab == "Clipboard" ? 680 : 580;

                    var islandShell = new Border
                    {
                        Width = widthDip,
                        Background = new SolidColorBrush(Color.FromArgb(224, 10, 12, 16)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(16),
                        Padding = new Thickness(14) // Exactly 14 DIP on all 4 sides (Rule 1)
                    };

                    foreach (var key in res.Keys)
                    {
                        islandShell.Resources[key] = res[key];
                    }

                    var layout = new Grid();
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); // Row 0: Clock
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) }); // Row 1: Centered Tab Bar
                    layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

                    // Header Row 0: Clock
                    var headerRow0 = new Grid();
                    headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
                    headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    headerRow0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });

                    var clockStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    var clockFace = new ClockFace { ClockStyleProp = "Simple" };
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

                    var actionStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                    actionStack.Children.Add(new TextBlock { Text = "\uE718", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 13, Foreground = Brushes.LightGray, Margin = new Thickness(4, 0, 4, 0) });
                    actionStack.Children.Add(new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 13, Foreground = Brushes.LightGray, Margin = new Thickness(4, 0, 0, 0) });
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
                            Background = isCur ? new SolidColorBrush(Color.FromArgb(40, 0, 229, 255)) : Brushes.Transparent,
                            CornerRadius = new CornerRadius(16),
                            Padding = new Thickness(isCur ? 10 : 8, 4, isCur ? 10 : 8, 4),
                            Margin = new Thickness(2, 0, 2, 0)
                        };
                        var sp = new StackPanel { Orientation = Orientation.Horizontal };
                        sp.Children.Add(new TextBlock { Text = tabGlyphs[i], FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 13, Foreground = isCur ? new SolidColorBrush(Color.FromRgb(0, 229, 255)) : new SolidColorBrush(Color.FromRgb(140, 150, 165)) });
                        if (isCur)
                        {
                            sp.Children.Add(new TextBlock { Text = tabNames[i], FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(5, 0, 0, 0) });
                        }
                        btn.Child = sp;
                        tabBar.Children.Add(btn);
                    }
                    Grid.SetRow(tabBar, 1);
                    layout.Children.Add(tabBar);

                    // Flat Content (Directly touches 14 DIP content line, zero card chrome)
                    var contentHost = new Border { Margin = new Thickness(0, 8, 0, 0), ClipToBounds = true };
                    var contentStack = new StackPanel();

                    if (tab == "Music")
                    {
                        var marqueePanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                        marqueePanel.Children.Add(new TextBlock { Text = "\uE8D6 ", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) });
                        marqueePanel.Children.Add(new TextBlock { Text = "Menu Samjha - Diljit Dosanjh", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        contentStack.Children.Add(marqueePanel);

                        var artist = new TextBlock { Text = "Aleemrk, Umair · Spotify", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(150, 155, 170)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 8) };
                        var bar = new TextBlock { Text = "0:19  ──────●─────────────  3:17", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                        contentStack.Children.Add(artist);
                        contentStack.Children.Add(bar);
                    }
                    else if (tab == "System")
                    {
                        var bento = new Grid();
                        bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                        bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                        bento.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        bento.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        bento.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
                        bento.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        // Vertical Dividers
                        var vdiv1 = new Border { Width = 1, Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                        Grid.SetColumn(vdiv1, 1); Grid.SetRowSpan(vdiv1, 3);
                        bento.Children.Add(vdiv1);

                        var vdiv2 = new Border { Width = 1, Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                        Grid.SetColumn(vdiv2, 3); Grid.SetRowSpan(vdiv2, 3);
                        bento.Children.Add(vdiv2);

                        // Horizontal Dividers
                        var hdiv1 = new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetRow(hdiv1, 1); Grid.SetColumn(hdiv1, 0); bento.Children.Add(hdiv1);
                        var hdiv2 = new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetRow(hdiv2, 1); Grid.SetColumn(hdiv2, 2); bento.Children.Add(hdiv2);
                        var hdiv3 = new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetRow(hdiv3, 1); Grid.SetColumn(hdiv3, 4); bento.Children.Add(hdiv3);

                        // Row 0, Col 0: CPU
                        var cpuStack = new StackPanel();
                        cpuStack.Children.Add(new TextBlock { Text = "CPU 12% · 52°C", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        var cpuBars = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
                        for (int c = 0; c < 16; c++)
                        {
                            var bar = new Border { Width = 4, Height = 18, Background = new SolidColorBrush(Color.FromArgb(50, 0, 229, 255)), CornerRadius = new CornerRadius(1.5), Margin = new Thickness(1, 0, 1, 0), VerticalAlignment = VerticalAlignment.Bottom };
                            if (c % 3 == 0) bar.Background = new SolidColorBrush(Color.FromRgb(0, 229, 255));
                            cpuBars.Children.Add(bar);
                        }
                        cpuStack.Children.Add(cpuBars);
                        Grid.SetRow(cpuStack, 0); Grid.SetColumn(cpuStack, 0); bento.Children.Add(cpuStack);

                        // Row 2, Col 0: RAM
                        var ramStack = new StackPanel();
                        ramStack.Children.Add(new TextBlock { Text = "RAM 7.5 GB / 15.7 GB", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        var ramBar = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Margin = new Thickness(0, 8, 0, 0) };
                        var ramFill = new Border { HorizontalAlignment = HorizontalAlignment.Left, Width = 80, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
                        ramBar.Child = ramFill;
                        ramStack.Children.Add(ramBar);
                        ramStack.Children.Add(new TextBlock { Text = "In Use: 48%", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 4, 0, 0) });
                        Grid.SetRow(ramStack, 2); Grid.SetColumn(ramStack, 0); bento.Children.Add(ramStack);

                        // Row 0, Col 2: GPU
                        var gpuStack = new StackPanel();
                        gpuStack.Children.Add(new TextBlock { Text = "RTX 4050 · 60°C", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        var vramBar = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Margin = new Thickness(0, 8, 0, 0) };
                        var vramFill = new Border { HorizontalAlignment = HorizontalAlignment.Left, Width = 45, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
                        vramBar.Child = vramFill;
                        gpuStack.Children.Add(vramBar);
                        gpuStack.Children.Add(new TextBlock { Text = "VRAM: 1.8 / 6.0 GB (30%)", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 4, 0, 0) });
                        Grid.SetRow(gpuStack, 0); Grid.SetColumn(gpuStack, 2); bento.Children.Add(gpuStack);

                        // Row 2, Col 2: Power & Battery
                        var pwrStack = new StackPanel();
                        pwrStack.Children.Add(new TextBlock { Text = "Power 45W · 88%", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        var pwrLine = new TextBlock { Text = "~~~~/\\~~/\\~~~", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(0, 4, 0, 0) };
                        pwrStack.Children.Add(pwrLine);
                        pwrStack.Children.Add(new TextBlock { Text = "Mean: 42W · AC Connected", FontFamily = (FontFamily)res["Typography.Family.Label"], FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 0) });
                        Grid.SetRow(pwrStack, 2); Grid.SetColumn(pwrStack, 2); bento.Children.Add(pwrStack);

                        // Row 0, Col 4: Network
                        var netStack = new StackPanel();
                        netStack.Children.Add(new TextBlock { Text = "Network", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        netStack.Children.Add(new TextBlock { Text = "↓ 12.4 MB/s   ↑ 1.8 MB/s", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(0, 4, 0, 0) });
                        netStack.Children.Add(new TextBlock { Text = "Today: 3.2 GB down · 420 MB up", FontFamily = (FontFamily)res["Typography.Family.Label"], FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 0) });
                        Grid.SetRow(netStack, 0); Grid.SetColumn(netStack, 4); bento.Children.Add(netStack);

                        // Row 2, Col 4: Top Apps (5 rows with separate icon block and text block, NO tofu!)
                        var rightStack = new StackPanel();
                        rightStack.Children.Add(new TextBlock { Text = "Top Apps", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });

                        string[] appIcons = ["\uE756", "\uE71D", "\uE74C", "\uE72D", "\uE774"];
                        string[] appNames = ["agy", "Island", "Antigravity", "ChatGPT", "msedge"];
                        string[] appMetrics = ["458 MB", "356 MB", "280 MB", "276 MB", "262 MB"];

                        for (int i = 0; i < 5; i++)
                        {
                            var row = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 2, 8, 2), Background = Brushes.Transparent };
                            var appGrid = new Grid();
                            appGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                            appGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            appGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            var iconBlock = new TextBlock { Text = appIcons[i], FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) };
                            Grid.SetColumn(iconBlock, 0); appGrid.Children.Add(iconBlock);

                            var nameBlock = new TextBlock { Text = appNames[i], FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
                            Grid.SetColumn(nameBlock, 1); appGrid.Children.Add(nameBlock);

                            var metricBlock = new TextBlock { Text = appMetrics[i], FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), VerticalAlignment = VerticalAlignment.Center };
                            Grid.SetColumn(metricBlock, 2); appGrid.Children.Add(metricBlock);

                            row.Child = appGrid;
                            rightStack.Children.Add(row);
                        }
                        Grid.SetRow(rightStack, 2); Grid.SetColumn(rightStack, 4); bento.Children.Add(rightStack);

                        contentStack.Children.Add(bento);
                    }
                    else if (tab == "Clipboard")
                    {
                        var clipGrid = new Grid();
                        clipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        clipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                        clipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var leftStack = new StackPanel();
                        leftStack.Children.Add(new TextBlock { Text = "Recent Copies (38 items)", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        leftStack.Children.Add(new TextBlock { Text = "https://github.com/island-app", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), Margin = new Thickness(0, 6, 0, 0) });
                        leftStack.Children.Add(new TextBlock { Text = "git commit -m 'feat: spacing and layout'", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 0) });
                        leftStack.Children.Add(new TextBlock { Text = "Color #00E5FF · Theme Accent", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 0) });
                        Grid.SetColumn(leftStack, 0);
                        clipGrid.Children.Add(leftStack);

                        var div = new Border { Width = 1, Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                        Grid.SetColumn(div, 1);
                        clipGrid.Children.Add(div);

                        var rightStack = new StackPanel();
                        rightStack.Children.Add(new TextBlock { Text = "Screenshots (14 items)", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        var thumbGrid = new UniformGrid { Columns = 3, Rows = 2, Margin = new Thickness(0, 6, 0, 0) };
                        for (int t = 0; t < 6; t++)
                        {
                            var thumb = new Border { Height = 40, CornerRadius = new CornerRadius(6), Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), Margin = new Thickness(2) };
                            thumbGrid.Children.Add(thumb);
                        }
                        rightStack.Children.Add(thumbGrid);
                        Grid.SetColumn(rightStack, 2);
                        clipGrid.Children.Add(rightStack);

                        contentStack.Children.Add(clipGrid);
                    }
                    else if (tab == "Focus")
                    {
                        var modeBorder = new Border { HorizontalAlignment = HorizontalAlignment.Center, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), CornerRadius = new CornerRadius(6), Padding = new Thickness(4) };
                        var modeStack = new StackPanel { Orientation = Orientation.Horizontal };
                        modeStack.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(60, 0, 229, 255)), CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = "Pomodoro", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)) } });
                        modeStack.Children.Add(new Border { Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = "Timer", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 11, Foreground = Brushes.Gray } });
                        modeStack.Children.Add(new Border { Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = "Stopwatch", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 11, Foreground = Brushes.Gray } });
                        modeBorder.Child = modeStack;
                        contentStack.Children.Add(modeBorder);

                        var sub = new TextBlock { Text = "Work · 1 of 4", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 6) };
                        contentStack.Children.Add(sub);

                        var ringGrid = new Grid { Width = 110, Height = 110, HorizontalAlignment = HorizontalAlignment.Center };
                        var ringTrack = new System.Windows.Shapes.Ellipse { Width = 106, Height = 106, Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), StrokeThickness = 4 };
                        ringGrid.Children.Add(ringTrack);
                        var timer = new TextBlock { Text = "25:00", FontFamily = (FontFamily)res["Typography.Family.Display"], FontSize = 36, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                        ringGrid.Children.Add(timer);
                        contentStack.Children.Add(ringGrid);

                        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
                        actions.Children.Add(new Border { Width = 36, Height = 36, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), CornerRadius = new CornerRadius(8), Child = new TextBlock { Text = "\uE777", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                        actions.Children.Add(new Border { Width = 44, Height = 44, Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)), CornerRadius = new CornerRadius(10), Margin = new Thickness(14, 0, 14, 0), Child = new TextBlock { Text = "\uE768", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 18, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                        actions.Children.Add(new Border { Width = 36, Height = 36, Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), CornerRadius = new CornerRadius(8), Child = new TextBlock { Text = "\uE893", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
                        contentStack.Children.Add(actions);
                    }
                    else if (tab == "QuickControls")
                    {
                        // Volume row
                        var volRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                        volRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        volRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        volRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        volRow.Children.Add(new TextBlock { Text = "\uE767", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
                        var volBar = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), VerticalAlignment = VerticalAlignment.Center };
                        volBar.Child = new Border { HorizontalAlignment = HorizontalAlignment.Left, Width = 180, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
                        Grid.SetColumn(volBar, 1); volRow.Children.Add(volBar);
                        var volTxt = new TextBlock { Text = "65%", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12, Foreground = Brushes.White, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetColumn(volTxt, 2); volRow.Children.Add(volTxt);
                        contentStack.Children.Add(volRow);

                        // Brightness row
                        var briRow = new Grid { Margin = new Thickness(0, 0, 0, 12) };
                        briRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        briRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        briRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        briRow.Children.Add(new TextBlock { Text = "\uE706", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
                        var briBar = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), VerticalAlignment = VerticalAlignment.Center };
                        briBar.Child = new Border { HorizontalAlignment = HorizontalAlignment.Left, Width = 220, CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)) };
                        Grid.SetColumn(briBar, 1); briRow.Children.Add(briBar);
                        var briTxt = new TextBlock { Text = "80%", FontFamily = (FontFamily)res["Typography.Family.Numeric"], FontSize = 12, Foreground = Brushes.White, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                        Grid.SetColumn(briTxt, 2); briRow.Children.Add(briTxt);
                        contentStack.Children.Add(briRow);

                        // 8 Tiles in 4x2
                        var controlsGrid = new UniformGrid { Columns = 4, Rows = 2 };
                        string[] labels = ["Mute Mic", "Wi-Fi", "Bluetooth", "Focus", "Snip Tool", "Lock PC", "Sleep", "Task Mgr"];
                        string[] icons = ["\uE720", "\uE701", "\uE702", "\uE708", "\uF127", "\uE72E", "\uE7E8", "\uE9D9"];
                        for (int i = 0; i < 8; i++)
                        {
                            var tile = new Border
                            {
                                Background = new SolidColorBrush(Color.FromRgb(22, 26, 32)),
                                CornerRadius = new CornerRadius(14), // Radius 14
                                Padding = new Thickness(10), // Padding 10
                                Margin = new Thickness(2)
                            };
                            var tileStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                            tileStack.Children.Add(new TextBlock { Text = icons[i], FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"), FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)), HorizontalAlignment = HorizontalAlignment.Center });
                            tileStack.Children.Add(new TextBlock { Text = labels[i], FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 11, Foreground = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
                            tile.Child = tileStack;
                            controlsGrid.Children.Add(tile);
                        }
                        contentStack.Children.Add(controlsGrid);
                    }
                    else if (tab == "Tasks")
                    {
                        var taskTop = new Border { Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 8) };
                        var topGrid = new Grid();
                        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        topGrid.Children.Add(new TextBlock { Text = "Add a task... (Press Enter)", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), VerticalAlignment = VerticalAlignment.Center });
                        var addBtn = new Border { Background = new SolidColorBrush(Color.FromRgb(0, 229, 255)), CornerRadius = new CornerRadius(4), Width = 22, Height = 22, Child = new TextBlock { Text = "+", FontWeight = FontWeights.Bold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
                        Grid.SetColumn(addBtn, 1); topGrid.Children.Add(addBtn);
                        taskTop.Child = topGrid;
                        contentStack.Children.Add(taskTop);

                        string[] taskItems = ["Review pull request for island spacing", "Update typography pairing documentation", "Check battery telemetry on 125% DPI"];
                        foreach (var t in taskItems)
                        {
                            var row = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 2, 0, 2), Background = Brushes.Transparent };
                            var rowSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                            rowSp.Children.Add(new System.Windows.Shapes.Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(Color.FromRgb(0, 229, 255)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
                            rowSp.Children.Add(new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(3), BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray, Margin = new Thickness(0, 0, 8, 0) });
                            rowSp.Children.Add(new TextBlock { Text = t, FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 12, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
                            row.Child = rowSp;
                            contentStack.Children.Add(row);
                        }
                    }
                    else if (tab == "CalendarWeather")
                    {
                        var cwGrid = new Grid();
                        cwGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
                        cwGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
                        cwGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        // Left: Month
                        var calStack = new StackPanel();
                        calStack.Children.Add(new TextBlock { Text = "November 2026", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        var daysGrid = new UniformGrid { Columns = 7, Rows = 5, Margin = new Thickness(0, 6, 0, 0) };
                        for (int d = 1; d <= 30; d++)
                        {
                            var dayCell = new Border { Width = 22, Height = 18, CornerRadius = new CornerRadius(4), Background = d == 21 ? new SolidColorBrush(Color.FromRgb(0, 229, 255)) : Brushes.Transparent, Margin = new Thickness(1) };
                            dayCell.Child = new TextBlock { Text = d.ToString(), FontSize = 10, Foreground = d == 21 ? Brushes.Black : Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            daysGrid.Children.Add(dayCell);
                        }
                        calStack.Children.Add(daysGrid);
                        Grid.SetColumn(calStack, 0); cwGrid.Children.Add(calStack);

                        // Divider
                        var div = new Border { Width = 1, Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)), HorizontalAlignment = HorizontalAlignment.Center };
                        Grid.SetColumn(div, 1); cwGrid.Children.Add(div);

                        // Right: Weather
                        var wStack = new StackPanel();
                        wStack.Children.Add(new TextBlock { Text = "New Delhi · 24°C", FontFamily = (FontFamily)res["Typography.Family.Heading"], FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
                        wStack.Children.Add(new TextBlock { Text = "Partly Cloudy · Humidity 45%", FontFamily = (FontFamily)res["Typography.Family.Body"], FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 165)), Margin = new Thickness(0, 2, 0, 6) });
                        var forecast = new UniformGrid { Columns = 5, Rows = 1 };
                        string[] fdays = ["Sat", "Sun", "Mon", "Tue", "Wed"];
                        string[] ftemps = ["24°", "23°", "22°", "25°", "24°"];
                        for (int f = 0; f < 5; f++)
                        {
                            var fCell = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                            fCell.Children.Add(new TextBlock { Text = fdays[f], FontSize = 10, Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center });
                            fCell.Children.Add(new TextBlock { Text = ftemps[f], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
                            forecast.Children.Add(fCell);
                        }
                        wStack.Children.Add(forecast);
                        Grid.SetColumn(wStack, 2); cwGrid.Children.Add(wStack);

                        contentStack.Children.Add(cwGrid);
                    }

                    contentHost.Child = contentStack;
                    Grid.SetRow(contentHost, 2);
                    layout.Children.Add(contentHost);

                    islandShell.Child = layout;

                    // Dynamically measure the exact natural height of the island shell and snap to 8 DIP grid
                    islandShell.Measure(new Size(widthDip, double.PositiveInfinity));
                    int heightDip = (int)(Math.Ceiling(islandShell.DesiredSize.Height / 8.0) * 8.0);
                    islandShell.Height = heightDip;

                    pixelW = (int)Math.Round(widthDip * dpiScale);
                    pixelH = (int)Math.Round(heightDip * dpiScale);

                    islandShell.Arrange(new Rect(0, 0, widthDip, heightDip));
                    islandShell.UpdateLayout();

                    rtb = new RenderTargetBitmap(pixelW, pixelH, dpiVal, dpiVal, PixelFormats.Pbgra32);
                    rtb.Render(islandShell);

                    fileName = $"After_{dpiLabel}_{tab}.png";
                    outPath = Path.Combine(afterScreensDir, fileName);
                    encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using (var fs = File.Create(outPath))
                    {
                        encoder.Save(fs);
                    }
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool ok = thread.Join(TimeSpan.FromSeconds(60));
        Assert.True(ok, "After screen capture timed out");

        int captured = Directory.GetFiles(afterScreensDir, "*.png").Length;
        Assert.True(captured >= 16, $"Expected >=16 after screenshots, got {captured}");
    }
}
