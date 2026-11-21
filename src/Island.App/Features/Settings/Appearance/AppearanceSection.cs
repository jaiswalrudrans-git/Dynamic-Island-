using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow
{
    private void InitializeAppearance(AppSettings current)
    {
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

        Bind(UiScaleSlider, s => s with { UiScale = UiScaleSlider.Value });
    }
}
