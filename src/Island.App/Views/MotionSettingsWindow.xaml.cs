using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Island.App.Design;
using Island.App.Header.Views;
using Island.Core;
using Island.Core.Settings;

namespace Island.App.Views;

public partial class MotionSettingsWindow : Window
{
    private readonly Action<Func<AppSettings, AppSettings>> save;
    private string activePage = "Appearance";

    public MotionSettingsWindow(AppSettings current, Action<Func<AppSettings, AppSettings>> save)
    {
        this.save = save;
        InitializeComponent();
        Title = AppIdentity.Name + " settings";
        UpdateSidebarSelection();

        // Appearance
        ThemeSelector.ItemsSource = new[] { "Midnight", "Dark", "Graphite", "Pure Black", "Light", "One Dark Pro", "GitHub Dark", "Dracula", "Night Owl", "Cyberpunk", "Aurora", "Amber Terminal", "Mono White", "Sakura" };
        ThemeSelector.SelectedItem = current.Theme;
        SurfaceSelector.ItemsSource = new[] { "Opaque", "Translucent", "Clear" };
        SurfaceSelector.SelectedItem = current.SurfaceStyle;
        BgTranspSlider.Value = current.BackgroundTransparency;
        BgTranspBox.Text = ((int)Math.Round(current.BackgroundTransparency)).ToString();
        ColTranspSlider.Value = current.CollapsedTransparency;
        ColTranspBox.Text = ((int)Math.Round(current.CollapsedTransparency)).ToString();
        NeonIntensitySlider.Value = Math.Round(current.NeonIntensity * 100);
        NeonIntensityBox.Text = ((int)Math.Round(current.NeonIntensity * 100)).ToString();
        BottomNeonLineCheck.IsChecked = current.BottomNeonLine;
        NeonMusicReactiveCheck.IsChecked = current.NeonMusicReactive;
        NeonLoadReactiveCheck.IsChecked = current.NeonLoadReactive;
        InterfaceSizeSelector.ItemsSource = new[] { "Auto", "Compact", "Comfortable", "Large" };
        InterfaceSizeSelector.SelectedItem = current.InterfaceSize;
        UiScaleSlider.Value = current.UiScale;

        // Clock Studio
        ShowNotchClockCheck.IsChecked = current.ShowNotchClock;
        Clock24HourCheck.IsChecked = current.Use24HourClock;
        ClockSecondsCheck.IsChecked = current.ShowSeconds;
        ClockBlinkCheck.IsChecked = current.ClockBlinkingColon;
        ClockZeroCheck.IsChecked = current.ClockLeadingZero;
        ClockSizeSlider.Value = current.ClockSizeScale;
        CustomColorBox.Text = current.ClockCustomColor;
        CurrentStyleLabel.Text = current.ClockStyle;
        CurrentColorLabel.Text = current.ClockColor;
        CustomColorRow.Visibility = current.ClockColor == "Custom" ? Visibility.Visible : Visibility.Collapsed;

        var styleDefs = new (string Id, string Label)[]
        {
            ("Simple", "Simple"),
            ("WithDate", "With date"),
            ("Stacked", "Stacked"),
            ("DotMatrix", "Dot matrix"),
            ("Segment", "7-Segment"),
            ("Neon", "Neon glow"),
            ("Serif", "Serif italic"),
            ("Wide", "Wide bold"),
            ("Condensed", "Condensed"),
            ("Pixel", "Pixel art"),
            ("Compact", "Compact"),
            ("Terminal", "Terminal"),
            ("Gradient", "Gradient"),
            ("Outline", "Outline")
        };

        var styleBorders = new List<(string Id, Border Box, ClockFace Face)>();
        foreach (var def in styleDefs)
        {
            var face = new ClockFace
            {
                ClockStyleProp = def.Id,
                ClockColorProp = current.ClockColor,
                CustomColorHex = current.ClockCustomColor,
                Use24Hour = current.Use24HourClock,
                ShowSecondsProp = current.ShowSeconds,
                BlinkingColon = current.ClockBlinkingColon,
                LeadingZero = current.ClockLeadingZero,
                SizeScale = 0.85,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var label = new TextBlock
            {
                Text = def.Label,
                FontSize = 10,
                Foreground = (Brush)FindResource("Theme.TextSecondary"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(face);
            stack.Children.Add(label);

            bool isSelected = current.ClockStyle == def.Id;
            var box = new Border
            {
                Background = (Brush)FindResource("Theme.SurfaceRaised"),
                BorderBrush = isSelected ? (Brush)FindResource("Theme.Accent") : (Brush)FindResource("Theme.Border"),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4, 6, 4, 6),
                Margin = new Thickness(4),
                Cursor = Cursors.Hand,
                Child = stack
            };

            string id = def.Id;
            box.MouseLeftButtonDown += (_, _) =>
            {
                save(s => s with { ClockStyle = id });
                CurrentStyleLabel.Text = id;
                if (MatchTypographyCheck.IsChecked == true)
                {
                    CurrentTypographyLabel.Text = $"{id} (Clock-Synced)";
                }
                foreach (var item in styleBorders)
                {
                    bool sel = item.Id == id;
                    item.Box.BorderBrush = sel ? (Brush)FindResource("Theme.Accent") : (Brush)FindResource("Theme.Border");
                    item.Box.BorderThickness = new Thickness(sel ? 2 : 1);
                }
            };

            styleBorders.Add((def.Id, box, face));
            ClockStylesGrid.Children.Add(box);
        }

        var colorDefs = new (string Id, string Label, string Hex)[]
        {
            ("White", "White", "#F5F5F7"),
            ("Amber", "Amber", "#FFB300"),
            ("Cyan", "Cyan", "#00E5FF"),
            ("Pink", "Pink", "#FF2E97"),
            ("Green", "Green", "#00E676"),
            ("Violet", "Violet", "#B388FF"),
            ("Gradient", "Gradient", "gradient"),
            ("Cover", "Album Art", "cover"),
            ("Custom", "Custom", "custom")
        };

        var swatchBorders = new List<(string Id, Border Circle)>();
        foreach (var def in colorDefs)
        {
            Brush fillBrush;
            if (def.Hex == "gradient")
            {
                fillBrush = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new(Color.FromRgb(0x00, 0xE5, 0xFF), 0.0),
                        new(Color.FromRgb(0xFF, 0x2E, 0x97), 0.5),
                        new(Color.FromRgb(0xFF, 0xB3, 0x00), 1.0)
                    },
                    new Point(0, 0), new Point(1, 1));
            }
            else if (def.Hex == "cover")
            {
                fillBrush = new SolidColorBrush(Color.FromRgb(0x82, 0xAA, 0xFF));
            }
            else if (def.Hex == "custom")
            {
                try { fillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(current.ClockCustomColor)); }
                catch { fillBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF)); }
            }
            else
            {
                fillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(def.Hex));
            }

            bool isSelected = current.ClockColor == def.Id;
            var circle = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = fillBrush,
                BorderBrush = isSelected ? (Brush)FindResource("Theme.Accent") : Brushes.Transparent,
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
                ToolTip = def.Label
            };

            string id = def.Id;
            circle.MouseLeftButtonDown += (_, _) =>
            {
                save(s => s with { ClockColor = id });
                CurrentColorLabel.Text = id;
                CustomColorRow.Visibility = id == "Custom" ? Visibility.Visible : Visibility.Collapsed;
                foreach (var item in swatchBorders)
                {
                    bool sel = item.Id == id;
                    item.Circle.BorderBrush = sel ? (Brush)FindResource("Theme.Accent") : Brushes.Transparent;
                    item.Circle.BorderThickness = new Thickness(sel ? 2 : 1);
                }
                foreach (var item in styleBorders)
                {
                    item.Face.ClockColorProp = id;
                }
            };

            swatchBorders.Add((def.Id, circle));
            ColorSwatchesPanel.Children.Add(circle);
        }

        var previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        previewTimer.Tick += (_, _) =>
        {
            var now = DateTime.Now;
            foreach (var item in styleBorders)
            {
                item.Face.Time = now;
            }
        };
        previewTimer.Start();
        Closed += (_, _) => previewTimer.Stop();

        ShowNotchClockCheck.Click += (_, _) => save(s => s with { ShowNotchClock = ShowNotchClockCheck.IsChecked == true });
        Clock24HourCheck.Click += (_, _) =>
        {
            bool val = Clock24HourCheck.IsChecked == true;
            save(s => s with { Use24HourClock = val });
            foreach (var item in styleBorders) item.Face.Use24Hour = val;
        };
        ClockSecondsCheck.Click += (_, _) =>
        {
            bool val = ClockSecondsCheck.IsChecked == true;
            save(s => s with { ShowSeconds = val });
            foreach (var item in styleBorders) item.Face.ShowSecondsProp = val;
        };
        ClockBlinkCheck.Click += (_, _) =>
        {
            bool val = ClockBlinkCheck.IsChecked == true;
            save(s => s with { ClockBlinkingColon = val });
            foreach (var item in styleBorders) item.Face.BlinkingColon = val;
        };
        ClockZeroCheck.Click += (_, _) =>
        {
            bool val = ClockZeroCheck.IsChecked == true;
            save(s => s with { ClockLeadingZero = val });
            foreach (var item in styleBorders) item.Face.LeadingZero = val;
        };
        ClockSizeSlider.ValueChanged += (_, _) =>
        {
            save(s => s with { ClockSizeScale = ClockSizeSlider.Value });
        };
        CustomColorBox.TextChanged += (_, _) =>
        {
            string hex = CustomColorBox.Text.Trim();
            if (hex.Length is 4 or 7 && hex.StartsWith('#'))
            {
                save(s => s with { ClockCustomColor = hex });
                foreach (var item in styleBorders) item.Face.CustomColorHex = hex;
            }
        };

        // Typography Studio
        MatchTypographyCheck.IsChecked = current.MatchTypographyToClock;
        MatchAccentCheck.IsChecked = current.MatchAccentToClock;
        LegibilityGuardCheck.IsChecked = current.LegibilityGuard;
        TypographyScaleSlider.Value = current.TypographySizeScale;
        TypographyUppercaseCheck.IsChecked = current.TypographyUppercaseLabels;
        TypographyTabularCheck.IsChecked = current.TypographyTabularNumerals;
        TypographyGlowCheck.IsChecked = current.TypographyTextGlow;
        TypographyGlowSlider.Value = current.TypographyGlowStrength;
        TypographyGlowRow.Visibility = current.TypographyTextGlow ? Visibility.Visible : Visibility.Collapsed;

        TypographyWeightCombo.ItemsSource = new[] { "Regular", "Medium", "Bold" };
        TypographyWeightCombo.SelectedItem = current.TypographyWeight;

        CurrentTypographyLabel.Text = current.MatchTypographyToClock
            ? $"{current.ClockStyle} (Clock-Synced)"
            : current.TypographyPreset;

        var typoBorders = new List<(string Id, Border Box)>();

        foreach (var preset in TypographyResources.Presets)
        {
            var pDisplay = new TextBlock
            {
                Text = "14:32",
                FontFamily = preset.Display,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("Theme.Foreground"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var pStats = new TextBlock
            {
                Text = "CPU 29%",
                FontFamily = preset.Numeric,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Theme.Accent"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 1)
            };
            var pBody = new TextBlock
            {
                Text = "Now Playing",
                FontFamily = preset.Body,
                FontSize = 10,
                Foreground = (Brush)FindResource("Theme.TextSecondary"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var pName = new TextBlock
            {
                Text = preset.DisplayName,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("Theme.TextTertiary"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(pDisplay);
            stack.Children.Add(pStats);
            stack.Children.Add(pBody);
            stack.Children.Add(pName);

            bool isSel = string.Equals(current.TypographyPreset, preset.Id, StringComparison.OrdinalIgnoreCase);
            var box = new Border
            {
                Background = (Brush)FindResource("Theme.SurfaceRaised"),
                BorderBrush = isSel ? (Brush)FindResource("Theme.Accent") : (Brush)FindResource("Theme.Border"),
                BorderThickness = new Thickness(isSel ? 2 : 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4, 6, 4, 6),
                Margin = new Thickness(4),
                Cursor = Cursors.Hand,
                Child = stack
            };

            string pid = preset.Id;
            box.MouseLeftButtonDown += (_, _) =>
            {
                save(s => s with { TypographyPreset = pid, MatchTypographyToClock = false });
                MatchTypographyCheck.IsChecked = false;
                CurrentTypographyLabel.Text = pid;
                foreach (var b in typoBorders)
                {
                    bool s = b.Id == pid;
                    b.Box.BorderBrush = s ? (Brush)FindResource("Theme.Accent") : (Brush)FindResource("Theme.Border");
                    b.Box.BorderThickness = new Thickness(s ? 2 : 1);
                }
            };

            typoBorders.Add((preset.Id, box));
            TypographyPresetsGrid.Children.Add(box);
        }

        MatchTypographyCheck.Click += (_, _) =>
        {
            bool val = MatchTypographyCheck.IsChecked == true;
            save(s => s with { MatchTypographyToClock = val });
            CurrentTypographyLabel.Text = val ? $"{current.ClockStyle} (Clock-Synced)" : current.TypographyPreset;
        };

        MatchAccentCheck.Click += (_, _) =>
        {
            save(s => s with { MatchAccentToClock = MatchAccentCheck.IsChecked == true });
        };

        LegibilityGuardCheck.Click += (_, _) =>
        {
            save(s => s with { LegibilityGuard = LegibilityGuardCheck.IsChecked == true });
        };

        TypographyScaleSlider.ValueChanged += (_, _) =>
        {
            save(s => s with { TypographySizeScale = TypographyScaleSlider.Value });
        };

        TypographyWeightCombo.SelectionChanged += (_, _) =>
        {
            if (TypographyWeightCombo.SelectedItem is string w)
                save(s => s with { TypographyWeight = w });
        };

        TypographyUppercaseCheck.Click += (_, _) =>
        {
            save(s => s with { TypographyUppercaseLabels = TypographyUppercaseCheck.IsChecked == true });
        };

        TypographyTabularCheck.Click += (_, _) =>
        {
            save(s => s with { TypographyTabularNumerals = TypographyTabularCheck.IsChecked == true });
        };

        TypographyGlowCheck.Click += (_, _) =>
        {
            bool val = TypographyGlowCheck.IsChecked == true;
            TypographyGlowRow.Visibility = val ? Visibility.Visible : Visibility.Collapsed;
            save(s => s with { TypographyTextGlow = val });
        };

        TypographyGlowSlider.ValueChanged += (_, _) =>
        {
            save(s => s with { TypographyGlowStrength = TypographyGlowSlider.Value });
        };

        var bundledFontNames = new[]
        {
            "Default (From Theme)",
            "Segoe UI Variable Display, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Inter, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Doto, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Silkscreen, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Space Grotesk, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Share Tech Mono, Consolas",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Rajdhani, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Chakra Petch, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#JetBrains Mono, Consolas",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Barlow Condensed, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Bebas Neue, Arial",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Fredoka, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Instrument Serif, Georgia",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Orbitron, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Unbounded, Segoe UI",
            "pack://application:,,,/Island.App;component/Assets/Fonts/#Caveat, Segoe UI"
        };

        void SetupCombo(ComboBox cb, string? val, Action<string?> apply)
        {
            cb.ItemsSource = bundledFontNames;
            cb.SelectedItem = string.IsNullOrEmpty(val) ? "Default (From Theme)" : val;
            cb.SelectionChanged += (_, _) =>
            {
                string? chosen = cb.SelectedItem as string;
                apply(chosen == "Default (From Theme)" ? null : chosen);
            };
        }

        SetupCombo(RoleDisplayCombo, current.FontFamilyDisplay, f => save(s => s with { FontFamilyDisplay = f, MatchTypographyToClock = false }));
        SetupCombo(RoleHeadingCombo, current.FontFamilyHeading, f => save(s => s with { FontFamilyHeading = f, MatchTypographyToClock = false }));
        SetupCombo(RoleBodyCombo, current.FontFamilyBody, f => save(s => s with { FontFamilyBody = f, MatchTypographyToClock = false }));
        SetupCombo(RoleLabelCombo, current.FontFamilyLabel, f => save(s => s with { FontFamilyLabel = f, MatchTypographyToClock = false }));
        SetupCombo(RoleNumericCombo, current.FontFamilyNumeric, f => save(s => s with { FontFamilyNumeric = f, MatchTypographyToClock = false }));

        // Motion
        Reduced.IsChecked = current.ReduceMotion;
        Speed.Value = current.AnimationSpeed;
        Hover.Value = current.HoverDelayMs;
        Leave.Value = current.CollapseDelayMs;

        // Music
        MusicPreview.IsChecked = current.ShowMusicPreview;
        Equalizer.IsChecked = current.MusicEqualizer;
        Lyrics.IsChecked = current.LyricsEnabled;
        CoverGlow.IsChecked = current.CoverGlow;
        PreviewText.ItemsSource = new[] { "Clock", "Title", "Lyric" };
        PreviewText.SelectedItem = current.MusicPreviewText;

        // System
        CollapsedStat.ItemsSource = new[] { "None", "Cpu", "Ram", "Network", "Battery", "CpuRam" };
        CollapsedStat.SelectedItem = current.CollapsedStat;
        ProcessSort.ItemsSource = new[] { "Cpu", "Memory" };
        ProcessSort.SelectedItem = current.SystemProcessSort;

        // Alerts
        AlertsEnabledCheck.IsChecked = current.AlertsEnabled;
        AlertVolumeCheck.IsChecked = current.AlertVolumeOsdEnabled;
        AlertBrightnessCheck.IsChecked = current.AlertBrightnessOsdEnabled;
        AlertMicCheck.IsChecked = current.AlertMicOsdEnabled;
        AlertBatteryCheck.IsChecked = current.AlertBatteryEnabled;
        AlertTempCheck.IsChecked = current.AlertTemperatureEnabled;
        AlertSoundCheck.IsChecked = current.AlertSoundEnabled;
        AlertDurationSlider.Value = current.AlertDurationSeconds;
        LayoutDebugCheck.IsChecked = current.LayoutDebug;

        // Event hooks
        ThemeSelector.SelectionChanged += (_, _) =>
        {
            if (ThemeSelector.SelectedItem is string theme) save(s => s with { Theme = theme });
        };
        SurfaceSelector.SelectionChanged += (_, _) =>
        {
            if (SurfaceSelector.SelectedItem is string style)
            {
                double targetBg = style switch
                {
                    "Clear" => 55,
                    "Translucent" => 20,
                    _ => 0
                };
                BgTranspSlider.Value = targetBg;
                save(s => s with { SurfaceStyle = style, BackgroundTransparency = targetBg });
            }
        };

        BgTranspSlider.ValueChanged += (_, _) =>
        {
            int val = (int)Math.Round(BgTranspSlider.Value);
            BgTranspBox.Text = val.ToString();
            save(s => s with { BackgroundTransparency = val });
        };
        BgTranspMinus.Click += (_, _) =>
        {
            BgTranspSlider.Value = Math.Clamp(Math.Round(BgTranspSlider.Value) - 1, 0, 70);
        };
        BgTranspPlus.Click += (_, _) =>
        {
            BgTranspSlider.Value = Math.Clamp(Math.Round(BgTranspSlider.Value) + 1, 0, 70);
        };
        void CommitBgTransp()
        {
            if (double.TryParse(BgTranspBox.Text, out var v))
                BgTranspSlider.Value = Math.Clamp(Math.Round(v), 0, 70);
            else
                BgTranspBox.Text = ((int)Math.Round(BgTranspSlider.Value)).ToString();
        }
        BgTranspBox.LostKeyboardFocus += (_, _) => CommitBgTransp();
        BgTranspBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { CommitBgTransp(); e.Handled = true; }
        };

        ColTranspSlider.ValueChanged += (_, _) =>
        {
            int val = (int)Math.Round(ColTranspSlider.Value);
            ColTranspBox.Text = val.ToString();
            save(s => s with { CollapsedTransparency = val });
        };
        ColTranspMinus.Click += (_, _) =>
        {
            ColTranspSlider.Value = Math.Clamp(Math.Round(ColTranspSlider.Value) - 1, 0, 50);
        };
        ColTranspPlus.Click += (_, _) =>
        {
            ColTranspSlider.Value = Math.Clamp(Math.Round(ColTranspSlider.Value) + 1, 0, 50);
        };
        void CommitColTransp()
        {
            if (double.TryParse(ColTranspBox.Text, out var v))
                ColTranspSlider.Value = Math.Clamp(Math.Round(v), 0, 50);
            else
                ColTranspBox.Text = ((int)Math.Round(ColTranspSlider.Value)).ToString();
        }
        ColTranspBox.LostKeyboardFocus += (_, _) => CommitColTransp();
        ColTranspBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { CommitColTransp(); e.Handled = true; }
        };

        NeonIntensitySlider.ValueChanged += (_, _) =>
        {
            int val = (int)Math.Round(NeonIntensitySlider.Value);
            NeonIntensityBox.Text = val.ToString();
            save(s => s with { NeonIntensity = val / 100.0 });
        };
        NeonIntensityMinus.Click += (_, _) =>
        {
            NeonIntensitySlider.Value = Math.Clamp(Math.Round(NeonIntensitySlider.Value) - 5, 0, 100);
        };
        NeonIntensityPlus.Click += (_, _) =>
        {
            NeonIntensitySlider.Value = Math.Clamp(Math.Round(NeonIntensitySlider.Value) + 5, 0, 100);
        };
        void CommitNeonIntensity()
        {
            if (double.TryParse(NeonIntensityBox.Text, out var v))
                NeonIntensitySlider.Value = Math.Clamp(Math.Round(v), 0, 100);
            else
                NeonIntensityBox.Text = ((int)Math.Round(NeonIntensitySlider.Value)).ToString();
        }
        NeonIntensityBox.LostKeyboardFocus += (_, _) => CommitNeonIntensity();
        NeonIntensityBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { CommitNeonIntensity(); e.Handled = true; }
        };
        BottomNeonLineCheck.Click += (_, _) => save(s => s with { BottomNeonLine = BottomNeonLineCheck.IsChecked == true });
        NeonMusicReactiveCheck.Click += (_, _) => save(s => s with { NeonMusicReactive = NeonMusicReactiveCheck.IsChecked == true });
        NeonLoadReactiveCheck.Click += (_, _) => save(s => s with { NeonLoadReactive = NeonLoadReactiveCheck.IsChecked == true });

        InterfaceSizeSelector.SelectionChanged += (_, _) =>
        {
            if (InterfaceSizeSelector.SelectedItem is string size) save(s => s with { InterfaceSize = size });
        };

        MusicPreview.Click += (_, _) => save(s => s with { ShowMusicPreview = MusicPreview.IsChecked == true });
        Equalizer.Click += (_, _) => save(s => s with { MusicEqualizer = Equalizer.IsChecked == true });
        Lyrics.Click += (_, _) => save(s => s with { LyricsEnabled = Lyrics.IsChecked == true });
        CoverGlow.Click += (_, _) => save(s => s with { CoverGlow = CoverGlow.IsChecked == true });
        PreviewText.SelectionChanged += (_, _) => save(s => s with { MusicPreviewText = (string)PreviewText.SelectedItem });
        CollapsedStat.SelectionChanged += (_, _) => save(s => s with { CollapsedStat = (string)CollapsedStat.SelectedItem });
        ProcessSort.SelectionChanged += (_, _) => save(s => s with { SystemProcessSort = (string)ProcessSort.SelectedItem });
        AlertsEnabledCheck.Click += (_, _) => save(s => s with { AlertsEnabled = AlertsEnabledCheck.IsChecked == true });
        AlertVolumeCheck.Click += (_, _) => save(s => s with { AlertVolumeOsdEnabled = AlertVolumeCheck.IsChecked == true });
        AlertBrightnessCheck.Click += (_, _) => save(s => s with { AlertBrightnessOsdEnabled = AlertBrightnessCheck.IsChecked == true });
        AlertMicCheck.Click += (_, _) => save(s => s with { AlertMicOsdEnabled = AlertMicCheck.IsChecked == true });
        AlertBatteryCheck.Click += (_, _) => save(s => s with { AlertBatteryEnabled = AlertBatteryCheck.IsChecked == true });
        AlertTempCheck.Click += (_, _) => save(s => s with { AlertTemperatureEnabled = AlertTempCheck.IsChecked == true });
        AlertSoundCheck.Click += (_, _) => save(s => s with { AlertSoundEnabled = AlertSoundCheck.IsChecked == true });
        LayoutDebugCheck.Click += (_, _) => save(s => s with { LayoutDebug = LayoutDebugCheck.IsChecked == true });
        Reduced.Click += (_, _) => save(s => s with { ReduceMotion = Reduced.IsChecked == true });

        // Commit slider edits live
        void Bind(Slider slider, Func<AppSettings, AppSettings> edit)
        {
            slider.ValueChanged += (_, _) => save(edit);
        }
        Bind(UiScaleSlider, s => s with { UiScale = UiScaleSlider.Value });
        Bind(Speed, s => s with { AnimationSpeed = Speed.Value });
        Bind(Hover, s => s with { HoverDelayMs = (int)Hover.Value });
        Bind(Leave, s => s with { CollapseDelayMs = (int)Leave.Value });
        Bind(AlertDurationSlider, s => s with { AlertDurationSeconds = (int)AlertDurationSlider.Value });

        // Tabs & Navigation
        TabBarPositionCombo.ItemsSource = new[] { "Below clock", "Left of clock" };
        TabBarPositionCombo.SelectedItem = string.Equals(current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase) ? "Left of clock" : "Below clock";
        TabBarPositionCombo.SelectionChanged += (_, _) =>
        {
            if (TabBarPositionCombo.SelectedItem is string pos)
                save(s => s with { TabBarPosition = pos == "Left of clock" ? "LeftOfClock" : "BelowClock" });
        };

        TabLabelModeCombo.ItemsSource = new[] { "Selected tab only", "All tabs", "Icons only" };
        TabLabelModeCombo.SelectedItem = current.TabLabelMode switch
        {
            "All" => "All tabs",
            "None" => "Icons only",
            _ => "Selected tab only"
        };
        TabLabelModeCombo.SelectionChanged += (_, _) =>
        {
            if (TabLabelModeCombo.SelectedItem is string mode)
            {
                string token = mode switch
                {
                    "All tabs" => "All",
                    "Icons only" => "None",
                    _ => "SelectedOnly"
                };
                save(s => s with { TabLabelMode = token });
            }
        };

        HeaderMouseWheelCheck.IsChecked = current.HeaderMouseWheelTabSwitch;
        HeaderMouseWheelCheck.Click += (_, _) => save(s => s with { HeaderMouseWheelTabSwitch = HeaderMouseWheelCheck.IsChecked == true });
        TabSwitchOnHoverCheck.IsChecked = current.TabSwitchOnHover;
        TabSwitchOnHoverCheck.Click += (_, _) => save(s => s with { TabSwitchOnHover = TabSwitchOnHoverCheck.IsChecked == true });

        // Clipboard
        ClipboardEnabledCheck.IsChecked = current.ClipboardEnabled;
        ClipboardEnabledCheck.Click += (_, _) => save(s => s with { ClipboardEnabled = ClipboardEnabledCheck.IsChecked == true });
        ClipboardHistorySlider.Value = current.ClipboardHistorySize;
        Bind(ClipboardHistorySlider, s => s with { ClipboardHistorySize = (int)ClipboardHistorySlider.Value });
        ClipboardRememberCheck.IsChecked = current.ClipboardRememberAcrossRestarts;
        ClipboardRememberCheck.Click += (_, _) => save(s => s with { ClipboardRememberAcrossRestarts = ClipboardRememberCheck.IsChecked == true });
        ClipboardFilterSecretsCheck.IsChecked = current.ClipboardFilterSecrets;
        ClipboardFilterSecretsCheck.Click += (_, _) => save(s => s with { ClipboardFilterSecrets = ClipboardFilterSecretsCheck.IsChecked == true });
        ClipboardCollapseCheck.IsChecked = current.ClipboardCollapseAfterPaste;
        ClipboardCollapseCheck.Click += (_, _) => save(s => s with { ClipboardCollapseAfterPaste = ClipboardCollapseCheck.IsChecked == true });

        ClipboardAutoExpireCombo.ItemsSource = new[] { "Never", "24 hours", "7 days", "30 days" };
        ClipboardAutoExpireCombo.SelectedItem = current.ClipboardAutoExpireHours switch
        {
            24 => "24 hours",
            168 => "7 days",
            720 => "30 days",
            _ => "Never"
        };
        ClipboardAutoExpireCombo.SelectionChanged += (_, _) =>
        {
            if (ClipboardAutoExpireCombo.SelectedItem is string expire)
            {
                int hours = expire switch
                {
                    "24 hours" => 24,
                    "7 days" => 168,
                    "30 days" => 720,
                    _ => 0
                };
                save(s => s with { ClipboardAutoExpireHours = hours });
            }
        };

        ClipboardClickActionCombo.ItemsSource = new[] { "Paste into target app", "Copy to clipboard" };
        ClipboardClickActionCombo.SelectedItem = current.ClipboardClickAction == "Copy" ? "Copy to clipboard" : "Paste into target app";
        ClipboardClickActionCombo.SelectionChanged += (_, _) =>
        {
            if (ClipboardClickActionCombo.SelectedItem is string act)
                save(s => s with { ClipboardClickAction = act == "Copy to clipboard" ? "Copy" : "Paste" });
        };

        // Screenshots
        ScreenshotsEnabledCheck.IsChecked = current.ScreenshotsEnabled;
        ScreenshotsEnabledCheck.Click += (_, _) => save(s => s with { ScreenshotsEnabled = ScreenshotsEnabledCheck.IsChecked == true });
        ScreenshotsClipboardCheck.IsChecked = current.ScreenshotsIncludeClipboard;
        ScreenshotsClipboardCheck.Click += (_, _) => save(s => s with { ScreenshotsIncludeClipboard = ScreenshotsClipboardCheck.IsChecked == true });
        ScreenshotsSaveSnipsCheck.IsChecked = current.ScreenshotsSaveClipboardToFolder;
        ScreenshotsSaveSnipsCheck.Click += (_, _) => save(s => s with { ScreenshotsSaveClipboardToFolder = ScreenshotsSaveSnipsCheck.IsChecked == true });

        ScreenshotsThumbSizeCombo.ItemsSource = new[] { "Compact (180px)", "Medium (240px)", "Large (320px)" };
        ScreenshotsThumbSizeCombo.SelectedItem = current.ScreenshotsThumbnailSize switch
        {
            <= 120 => "Compact (180px)",
            >= 240 => "Large (320px)",
            _ => "Medium (240px)"
        };
        ScreenshotsThumbSizeCombo.SelectionChanged += (_, _) =>
        {
            if (ScreenshotsThumbSizeCombo.SelectedItem is string sz)
            {
                int px = sz.StartsWith("Compact") ? 90 : sz.StartsWith("Large") ? 240 : 160;
                save(s => s with { ScreenshotsThumbnailSize = px });
            }
        };

        ScreenshotsClickActionCombo.ItemsSource = new[] { "Copy to clipboard", "Open in default viewer", "Paste into target app" };
        ScreenshotsClickActionCombo.SelectedItem = current.ScreenshotsClickAction switch
        {
            "Open" => "Open in default viewer",
            "Paste" => "Paste into target app",
            _ => "Copy to clipboard"
        };
        ScreenshotsClickActionCombo.SelectionChanged += (_, _) =>
        {
            if (ScreenshotsClickActionCombo.SelectedItem is string act)
            {
                string token = act.StartsWith("Open") ? "Open" : act.StartsWith("Paste") ? "Paste" : "Copy";
                save(s => s with { ScreenshotsClickAction = token });
            }
        };
    }

    private void NavClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            SwitchPage(tag);
        }
    }

    private void SwitchPage(string page)
    {
        activePage = page;
        PageAppearance.Visibility = page == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        PageClock.Visibility = page == "Clock" ? Visibility.Visible : Visibility.Collapsed;
        PageTypography.Visibility = page == "Typography" ? Visibility.Visible : Visibility.Collapsed;
        PageTabsClipboard.Visibility = page == "Tabs" ? Visibility.Visible : Visibility.Collapsed;
        PageMotion.Visibility = page == "Motion" ? Visibility.Visible : Visibility.Collapsed;
        PageSystemAlerts.Visibility = page == "System" ? Visibility.Visible : Visibility.Collapsed;
        PageAbout.Visibility = page == "About" ? Visibility.Visible : Visibility.Collapsed;

        PageHeaderTitle.Text = page switch
        {
            "Appearance" => "Appearance",
            "Clock" => "Clock Studio",
            "Typography" => "Typography Themes",
            "Tabs" => "Tabs & Clipboard",
            "Motion" => "Motion & Timing",
            "System" => "System & Alerts",
            "About" => "About Island",
            _ => "Settings"
        };

        UpdateSidebarSelection();
    }

    private void UpdateSidebarSelection()
    {
        void Highlight(Button btn, bool active)
        {
            if (btn == null) return;
            if (active)
            {
                btn.SetResourceReference(Control.BackgroundProperty, "Theme.AccentSoft");
                btn.SetResourceReference(Control.ForegroundProperty, "Theme.Accent");
            }
            else
            {
                btn.Background = Brushes.Transparent;
                btn.SetResourceReference(Control.ForegroundProperty, "Theme.TextSecondary");
            }
        }

        Highlight(NavAppearanceBtn, activePage == "Appearance");
        Highlight(NavClockBtn, activePage == "Clock");
        Highlight(NavTypographyBtn, activePage == "Typography");
        Highlight(NavTabsBtn, activePage == "Tabs");
        Highlight(NavMotionBtn, activePage == "Motion");
        Highlight(NavSystemBtn, activePage == "System");
        Highlight(NavAboutBtn, activePage == "About");
    }

    private void ResetCurrentPageClick(object sender, RoutedEventArgs e)
    {
        switch (activePage)
        {
            case "Appearance":
                ThemeSelector.SelectedItem = "Midnight";
                SurfaceSelector.SelectedItem = "Translucent";
                BgTranspSlider.Value = 20;
                ColTranspSlider.Value = 15;
                NeonIntensitySlider.Value = 100;
                BottomNeonLineCheck.IsChecked = false;
                NeonMusicReactiveCheck.IsChecked = true;
                NeonLoadReactiveCheck.IsChecked = true;
                InterfaceSizeSelector.SelectedItem = "Auto";
                UiScaleSlider.Value = 1.0;
                save(s => s with
                {
                    Theme = "Midnight",
                    SurfaceStyle = "Translucent",
                    BackgroundTransparency = 20,
                    CollapsedTransparency = 15,
                    NeonIntensity = 1.0,
                    BottomNeonLine = false,
                    NeonMusicReactive = true,
                    NeonLoadReactive = true,
                    InterfaceSize = "Auto",
                    UiScale = 1.0
                });
                break;

            case "Clock":
                ShowNotchClockCheck.IsChecked = true;
                Clock24HourCheck.IsChecked = true;
                ClockSecondsCheck.IsChecked = false;
                ClockBlinkCheck.IsChecked = false;
                ClockZeroCheck.IsChecked = true;
                ClockSizeSlider.Value = 1.0;
                CurrentStyleLabel.Text = "Simple";
                CurrentColorLabel.Text = "White";
                CustomColorBox.Text = "#00E5FF";
                CustomColorRow.Visibility = Visibility.Collapsed;
                save(s => s with
                {
                    ShowNotchClock = true,
                    ClockStyle = "Simple",
                    ClockColor = "White",
                    ClockCustomColor = "#00E5FF",
                    Use24HourClock = true,
                    ShowSeconds = false,
                    ClockBlinkingColon = false,
                    ClockLeadingZero = true,
                    ClockSizeScale = 1.0
                });
                break;

            case "Typography":
                MatchTypographyCheck.IsChecked = true;
                MatchAccentCheck.IsChecked = false;
                LegibilityGuardCheck.IsChecked = true;
                TypographyScaleSlider.Value = 1.0;
                TypographyWeightCombo.SelectedItem = "Regular";
                TypographyUppercaseCheck.IsChecked = false;
                TypographyTabularCheck.IsChecked = true;
                TypographyGlowCheck.IsChecked = false;
                TypographyGlowSlider.Value = 0.5;
                RoleDisplayCombo.SelectedItem = "Default (From Theme)";
                RoleHeadingCombo.SelectedItem = "Default (From Theme)";
                RoleBodyCombo.SelectedItem = "Default (From Theme)";
                RoleLabelCombo.SelectedItem = "Default (From Theme)";
                RoleNumericCombo.SelectedItem = "Default (From Theme)";
                save(s => s with
                {
                    MatchTypographyToClock = true,
                    MatchAccentToClock = false,
                    LegibilityGuard = true,
                    TypographyPreset = "Classic",
                    TypographySizeScale = 1.0,
                    TypographyWeight = "Regular",
                    TypographyUppercaseLabels = false,
                    TypographyTabularNumerals = true,
                    TypographyTextGlow = false,
                    TypographyGlowStrength = 0.5,
                    FontFamilyDisplay = null,
                    FontFamilyHeading = null,
                    FontFamilyBody = null,
                    FontFamilyLabel = null,
                    FontFamilyNumeric = null
                });
                break;

            case "Tabs":
                TabBarPositionCombo.SelectedItem = "Below clock";
                TabLabelModeCombo.SelectedItem = "Selected tab only";
                HeaderMouseWheelCheck.IsChecked = true;
                TabSwitchOnHoverCheck.IsChecked = false;
                ClipboardEnabledCheck.IsChecked = true;
                ClipboardHistorySlider.Value = 50;
                ClipboardRememberCheck.IsChecked = false;
                ClipboardFilterSecretsCheck.IsChecked = true;
                ClipboardCollapseCheck.IsChecked = true;
                ClipboardAutoExpireCombo.SelectedItem = "24 hours";
                ClipboardClickActionCombo.SelectedItem = "Paste into target app";
                ScreenshotsEnabledCheck.IsChecked = true;
                ScreenshotsClipboardCheck.IsChecked = true;
                ScreenshotsSaveSnipsCheck.IsChecked = false;
                ScreenshotsThumbSizeCombo.SelectedItem = "Medium (240px)";
                ScreenshotsClickActionCombo.SelectedItem = "Copy to clipboard";
                save(s => s with
                {
                    TabBarPosition = "BelowClock",
                    TabLabelMode = "SelectedOnly",
                    HeaderMouseWheelTabSwitch = true,
                    TabSwitchOnHover = false,
                    ClipboardEnabled = true,
                    ClipboardHistorySize = 50,
                    ClipboardRememberAcrossRestarts = false,
                    ClipboardFilterSecrets = true,
                    ClipboardCollapseAfterPaste = true,
                    ClipboardAutoExpireHours = 24,
                    ClipboardClickAction = "Paste",
                    ScreenshotsEnabled = true,
                    ScreenshotsIncludeClipboard = true,
                    ScreenshotsSaveClipboardToFolder = false,
                    ScreenshotsThumbnailSize = 160,
                    ScreenshotsClickAction = "Copy"
                });
                break;

            case "Motion":
                Reduced.IsChecked = false;
                Speed.Value = 1.0;
                Hover.Value = 0;
                Leave.Value = 300;
                save(s => s with
                {
                    ReduceMotion = false,
                    AnimationSpeed = 1.0,
                    HoverDelayMs = 0,
                    CollapseDelayMs = 300
                });
                break;

            case "System":
                CollapsedStat.SelectedItem = "None";
                ProcessSort.SelectedItem = "Memory";
                AlertsEnabledCheck.IsChecked = true;
                AlertVolumeCheck.IsChecked = true;
                AlertBrightnessCheck.IsChecked = true;
                AlertMicCheck.IsChecked = true;
                AlertBatteryCheck.IsChecked = true;
                AlertTempCheck.IsChecked = true;
                AlertSoundCheck.IsChecked = true;
                AlertDurationSlider.Value = 3;
                LayoutDebugCheck.IsChecked = false;
                save(s => s with
                {
                    CollapsedStat = "None",
                    SystemProcessSort = "Memory",
                    AlertsEnabled = true,
                    AlertVolumeOsdEnabled = true,
                    AlertBrightnessOsdEnabled = true,
                    AlertMicOsdEnabled = true,
                    AlertBatteryEnabled = true,
                    AlertTemperatureEnabled = true,
                    AlertSoundEnabled = true,
                    AlertDurationSeconds = 3,
                    LayoutDebug = false
                });
                break;
        }
    }
}
