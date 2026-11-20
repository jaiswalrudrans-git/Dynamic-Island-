using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Island.Core;
using Island.Core.Settings;

namespace Island.App.Views;

public partial class MotionSettingsWindow : Window
{
    public MotionSettingsWindow(AppSettings current, Action<Func<AppSettings, AppSettings>> save)
    {
        InitializeComponent();
        Title = AppIdentity.Name + " settings";

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
    }
}
