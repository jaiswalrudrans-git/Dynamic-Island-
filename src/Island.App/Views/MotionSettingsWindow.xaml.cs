using System.Windows;
using System.Windows.Controls;
using Island.Core;
using Island.Core.Settings;

namespace Island.App.Views;

public partial class MotionSettingsWindow : Window
{
    public MotionSettingsWindow(AppSettings current, Action<Func<AppSettings, AppSettings>> save)
    {
        InitializeComponent();
        Title = AppIdentity.Name + " settings";
        Reduced.IsChecked = current.ReduceMotion;
        Speed.Value = current.AnimationSpeed;
        Hover.Value = current.HoverDelayMs;
        Leave.Value = current.CollapseDelayMs;
        MusicPreview.IsChecked = current.ShowMusicPreview;
        Equalizer.IsChecked = current.MusicEqualizer;
        Lyrics.IsChecked = current.LyricsEnabled;
        CoverGlow.IsChecked = current.CoverGlow;
        PreviewText.ItemsSource = new[] { "Clock", "Title", "Lyric" };
        PreviewText.SelectedItem = current.MusicPreviewText;
        CollapsedStat.ItemsSource = new[] { "None", "Cpu", "Ram", "Network", "Battery", "CpuRam" };
        CollapsedStat.SelectedItem = current.CollapsedStat;
        ProcessSort.ItemsSource = new[] { "Cpu", "Memory" };
        ProcessSort.SelectedItem = current.SystemProcessSort;
        AlertsEnabledCheck.IsChecked = current.AlertsEnabled;
        AlertVolumeCheck.IsChecked = current.AlertVolumeOsdEnabled;
        AlertBrightnessCheck.IsChecked = current.AlertBrightnessOsdEnabled;
        AlertMicCheck.IsChecked = current.AlertMicOsdEnabled;
        AlertBatteryCheck.IsChecked = current.AlertBatteryEnabled;
        AlertTempCheck.IsChecked = current.AlertTemperatureEnabled;
        AlertSoundCheck.IsChecked = current.AlertSoundEnabled;
        AlertDurationSlider.Value = current.AlertDurationSeconds;
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
        // Commit settled slider edits, including keyboard input, without writing on every drag pixel.
        void Bind(Slider slider, Func<AppSettings, AppSettings> edit)
        {
            slider.ValueChanged += (_, _) => { if (!slider.IsMouseCaptureWithin) save(edit); };
            slider.LostMouseCapture += (_, _) => save(edit);
        }
        Bind(Speed, s => s with { AnimationSpeed = Speed.Value });
        Bind(Hover, s => s with { HoverDelayMs = (int)Hover.Value });
        Bind(Leave, s => s with { CollapseDelayMs = (int)Leave.Value });
        Bind(AlertDurationSlider, s => s with { AlertDurationSeconds = (int)AlertDurationSlider.Value });
    }
}
