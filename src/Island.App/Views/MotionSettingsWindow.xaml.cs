using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        ThemeSelector.ItemsSource = new[] { "Midnight", "Dark", "Graphite", "Pure Black", "Light", "One Dark Pro", "GitHub Dark", "Dracula", "Night Owl", "Cyberpunk" };
        ThemeSelector.SelectedItem = current.Theme;
        SurfaceSelector.ItemsSource = new[] { "Opaque", "Translucent", "Clear" };
        SurfaceSelector.SelectedItem = current.SurfaceStyle;
        BgTranspSlider.Value = current.BackgroundTransparency;
        BgTranspBox.Text = ((int)Math.Round(current.BackgroundTransparency)).ToString();
        ColTranspSlider.Value = current.CollapsedTransparency;
        ColTranspBox.Text = ((int)Math.Round(current.CollapsedTransparency)).ToString();
        InterfaceSizeSelector.ItemsSource = new[] { "Auto", "Compact", "Comfortable", "Large" };
        InterfaceSizeSelector.SelectedItem = current.InterfaceSize;
        UiScaleSlider.Value = current.UiScale;

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

        // Commit settled slider edits
        void Bind(Slider slider, Func<AppSettings, AppSettings> edit)
        {
            slider.ValueChanged += (_, _) => { if (!slider.IsMouseCaptureWithin) save(edit); };
            slider.LostMouseCapture += (_, _) => save(edit);
        }
        Bind(UiScaleSlider, s => s with { UiScale = UiScaleSlider.Value });
        Bind(Speed, s => s with { AnimationSpeed = Speed.Value });
        Bind(Hover, s => s with { HoverDelayMs = (int)Hover.Value });
        Bind(Leave, s => s with { CollapseDelayMs = (int)Leave.Value });
        Bind(AlertDurationSlider, s => s with { AlertDurationSeconds = (int)AlertDurationSlider.Value });
    }
}
