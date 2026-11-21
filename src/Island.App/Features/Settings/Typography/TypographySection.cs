using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Island.App.Design;
using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow
{
    private void InitializeTypography(AppSettings current)
    {
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
    }
}
