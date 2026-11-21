using System.Windows;
using Island.App.Design;

namespace Island.App.Shell;

public static class CollapsedNotchLayout
{
    public const double MinFloorWidth = 96.0;
    public const double TimeOnlySidePadding = 14.0;
    public const double MusicSidePadding = 12.0;
    public const double ItemGap = 8.0;

    public static double CalculateWidth(
        double clockWidth,
        bool isMusicPlaying = false,
        bool showCover = false,
        double coverWidth = 22.0,
        bool showEqualizer = false,
        double equalizerWidth = 25.0,
        bool showText = false,
        double textWidth = 0.0)
    {
        if (!isMusicPlaying && !showCover)
        {
            double width = clockWidth + 2 * TimeOnlySidePadding;
            width = Math.Max(MinFloorWidth, width);
            return Math.Round(width / 2.0) * 2.0;
        }

        double contentWidth = 0.0;
        int count = 0;

        void Add(bool visible, double width)
        {
            if (!visible || width <= 0) return;
            if (count > 0) contentWidth += ItemGap;
            contentWidth += width;
            count++;
        }

        Add(showCover, coverWidth);
        Add(true, clockWidth);
        Add(showEqualizer, equalizerWidth);
        Add(showText, Math.Min(140.0, textWidth));

        double total = contentWidth + 2 * MusicSidePadding;
        total = Math.Max(MinFloorWidth, total);
        return Math.Round(total / 2.0) * 2.0;
    }

    public static double CalculateFromItems(
        double sidePadding,
        double gap,
        double floor,
        IEnumerable<(double Width, bool IsVisible)> items)
    {
        double contentWidth = 0.0;
        int count = 0;
        foreach (var (width, visible) in items)
        {
            if (!visible || width <= 0) continue;
            if (count > 0) contentWidth += gap;
            contentWidth += width;
            count++;
        }
        double total = contentWidth + 2 * sidePadding;
        total = Math.Max(floor, total);
        return Math.Round(total / 2.0) * 2.0;
    }
}

public partial class OverlayWindow
{
    private double GetClockTrailingAdvance()
    {
        double scale = settings.Current.ClockSizeScale;
        return settings.Current.ClockStyle switch
        {
            "DotMatrix" => 2.0 * scale,
            "Pixel" => 2.2 * scale,
            "Segment" => 2.0 * scale,
            "Wide" => 3.0 * scale,
            _ => 0.0
        };
    }

    internal double GetTargetCollapsedWidth()
    {
        if (alerts != null && alerts.HasActiveAlert)
        {
            return 272.0;
        }
        if (system != null && system.HasCollapsedStat)
        {
            return 240.0;
        }
        if (focus != null && focus.IsRunning)
        {
            return 240.0;
        }

        CompactClock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double trailing = GetClockTrailingAdvance();
        double clockW = Math.Max(0, CompactClock.DesiredSize.Width - trailing);

        bool musicActive = music != null && music.Preview;
        if (musicActive)
        {
            bool showEqualizer = settings.Current.MusicEqualizer && music?.Spectrum?.Available == true;
            bool showLyrics = settings.Current.LyricsEnabled && settings.Current.MusicPreviewText == "Lyric" && !string.IsNullOrEmpty(music?.CurrentLyric);
            bool showTitle = settings.Current.MusicPreviewText == "Title" && !string.IsNullOrEmpty(music?.Title);
            bool showText = showLyrics || showTitle;
            double textW = 0;
            if (showText && PreviewLabel != null)
            {
                PreviewLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                textW = PreviewLabel.DesiredSize.Width;
            }

            return CollapsedNotchLayout.CalculateWidth(
                clockWidth: clockW,
                isMusicPlaying: true,
                showCover: true,
                coverWidth: 22.0,
                showEqualizer: showEqualizer,
                equalizerWidth: 25.0,
                showText: showText,
                textWidth: textW);
        }

        return CollapsedNotchLayout.CalculateWidth(clockWidth: clockW, isMusicPlaying: false);
    }

    private void UpdateIslandTargetSize()
    {
        var size = navigation.GetDesiredSize(navigation.SelectedIndex);
        targetIslandWidth = size.Width;
        targetIslandHeight = size.Height;
        bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        double overhead = belowClock ? 116.0 : 80.0;
        double contentW = Math.Max(580 - IslandMetrics.HorizontalInset, Math.Round(size.Width - IslandMetrics.HorizontalInset));
        double contentH = Math.Max(60, Math.Round(size.Height - overhead));
        if (Header.Width != contentW) Header.Width = contentW;
        if (Body.Width != contentW) Body.Width = contentW;
        if (Body.Height != contentH) Body.Height = contentH;
        RequestFrame();
    }

    private void ApplyTabBarLayout()
    {
        if (HeaderRow0 == null || HeaderRow1 == null || Body == null) return;
        bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        if (belowClock)
        {
            HeaderRow0.Height = new GridLength(44);
            HeaderRow1.Height = new GridLength(36);
            Header.Height = 80;
            if (HeaderLeftCol != null) HeaderLeftCol.Width = new GridLength(72);
            if (HeaderRightCol != null) HeaderRightCol.Width = new GridLength(72);
            Body.Margin = new Thickness(0, 102, 0, 0);
            if (LegacyTabContainer != null) LegacyTabContainer.Visibility = Visibility.Collapsed;
            if (TabBarRow != null) TabBarRow.Visibility = Visibility.Visible;
            if (TabBarRow != null && TabBarContainer != null && TabBarRow.Children.Count == 0 && LegacyTabContainer?.Child != null)
            {
                var el = LegacyTabContainer.Child;
                LegacyTabContainer.Child = null;
                TabBarRow.Children.Add(el);
            }
        }
        else
        {
            HeaderRow0.Height = new GridLength(44);
            HeaderRow1.Height = new GridLength(0);
            Header.Height = 44;
            if (HeaderLeftCol != null) HeaderLeftCol.Width = GridLength.Auto;
            if (HeaderRightCol != null) HeaderRightCol.Width = new GridLength(72);
            Body.Margin = new Thickness(0, 66, 0, 0);
            if (LegacyTabContainer != null) LegacyTabContainer.Visibility = Visibility.Visible;
            if (TabBarRow != null) TabBarRow.Visibility = Visibility.Collapsed;
            if (LegacyTabContainer != null && LegacyTabContainer.Child == null && TabBarRow != null && TabBarRow.Children.Count > 0)
            {
                var el = TabBarRow.Children[0];
                TabBarRow.Children.Clear();
                LegacyTabContainer.Child = el;
            }
        }
    }
}
