using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Island.App.Header.Views;
using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow
{
    private void InitializeClock(AppSettings current)
    {
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
    }
}
