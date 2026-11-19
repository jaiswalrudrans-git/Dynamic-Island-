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
        MusicPreview.Click += (_, _) => save(s => s with { ShowMusicPreview = MusicPreview.IsChecked == true });
        Equalizer.Click += (_, _) => save(s => s with { MusicEqualizer = Equalizer.IsChecked == true });
        Lyrics.Click += (_, _) => save(s => s with { LyricsEnabled = Lyrics.IsChecked == true });
        CoverGlow.Click += (_, _) => save(s => s with { CoverGlow = CoverGlow.IsChecked == true });
        PreviewText.SelectionChanged += (_, _) => save(s => s with { MusicPreviewText = (string)PreviewText.SelectedItem });
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
    }
}
