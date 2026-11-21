using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Island.App.ViewModels;

namespace Island.App.Shell;

public partial class OverlayWindow
{
    private void DrawPanel()
    {
        double p = Math.Clamp(motion.Progress, 0, 1.06);
        bool reduced = settings.Current.ReduceMotion;
        ReducedBase.Visibility = reduced && p > 0 && p < 1 ? Visibility.Visible : Visibility.Hidden;
        double shape = reduced && p > 0 ? 1 : p;
        double curW = currentWidthSpring.Value;
        double curH = currentHeightSpring.Value;
        if (shape != lastShape || previewWidth.Value != lastPreview || curW != lastWidth || curH != lastHeight)
        {
            double centerX = ActualWidth > 0 ? ActualWidth / 2.0 : 520.0;
            notch.Update(shape, previewWidth.Value, curW, curH, centerX);
            lastPreview = previewWidth.Value;
            lastWidth = curW;
            lastHeight = curH;
            double bottom = 35 + (curH - 35) * shape;
            double shadowTop = bottom - 5 * shape;
            EdgeOffset.Y = shadowTop - BottomEdge.Height;
            lastShape = shape;

            bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
            double overhead = belowClock ? 116.0 : 80.0;
            double liveContentW = Math.Max(300, Math.Round(curW - 32));
            double liveContentH = Math.Max(60, Math.Round(curH - overhead));
            if (Header.Width != liveContentW) Header.Width = liveContentW;
            if (Body.Width != liveContentW) Body.Width = liveContentW;
            if (Body.Height != liveContentH) Body.Height = liveContentH;
            Pages.Clip = new RectangleGeometry(new Rect(0, 0, liveContentW, liveContentH), 27, 27);
        }
        IslandBackground.Opacity = shape;
        CollapsedBackground.Opacity = Math.Max(0, 1.0 - shape);
        OuterShadow.Opacity = 0.25 + 0.15 * shape;
        Shell.Opacity = reduced && p > 0 ? Math.Clamp(p, .01, 1) : 1;
        double compactOpacity = reduced && p > 0 ? 0 : Math.Max(0, 1 - p * 2);
        bool showAlert = alerts.HasActiveAlert;
        bool showMusic = !showAlert && music.Preview;
        bool showStat = !showAlert && !showMusic && system.HasCollapsedStat;
        bool showFocus = !showAlert && !showMusic && !showStat && (focus != null && focus.IsRunning);
        bool showClockOnly = !showAlert && !showMusic && !showStat && !showFocus && settings.Current.ShowNotchClock;

        CompactAlert.Visibility = showAlert && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactAlert.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1) * Math.Clamp(alertSpring.Value, 0.01, 1);

        CompactMusic.Visibility = showMusic && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactMusic.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        CompactStat.Visibility = showStat && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactStat.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        CompactFocus.Visibility = showFocus && compactOpacity > 0 ? Visibility.Visible : Visibility.Hidden;
        CompactFocus.Opacity = compactOpacity * Math.Clamp(previewWidth.Value, 0, 1);

        double clockOpacity = (showClockOnly || (showMusic && music.PreviewClock && settings.Current.ShowNotchClock)) ? compactOpacity : 0;
        CompactClock.Opacity = clockOpacity;
        CompactClock.Visibility = clockOpacity > 0.01 ? Visibility.Visible : Visibility.Hidden;
        Header.Opacity = motion.HeaderOpacity;
        Body.Opacity = motion.BodyOpacity;
        HeaderY.Y = reduced ? 0 : 6 * (1 - Header.Opacity);
        BodyY.Y = reduced ? 0 : 6 * (1 - Body.Opacity);
        Header.IsHitTestVisible = motion.Expanded && Header.Opacity > .15;
        Body.IsHitTestVisible = motion.Expanded && Body.Opacity > .15;
        UpdateBottomNeonLine();

        if (LayoutDebugOverlay != null)
        {
            if (settings.Current.LayoutDebug && shape > 0.1)
            {
                LayoutDebugOverlay.Visibility = Visibility.Visible;
                double centerX = ActualWidth > 0 ? ActualWidth / 2.0 : 520.0;
                double iLeft = centerX - curW / 2.0;
                Canvas.SetLeft(DebugIslandBounds, iLeft);
                Canvas.SetTop(DebugIslandBounds, 0);
                DebugIslandBounds.Width = curW;
                DebugIslandBounds.Height = curH;

                double cLeft = centerX - Body.Width / 2.0;
                Canvas.SetLeft(DebugHeaderBounds, cLeft);
                Canvas.SetTop(DebugHeaderBounds, 12);
                DebugHeaderBounds.Width = Header.Width;
                DebugHeaderBounds.Height = 44;

                Canvas.SetLeft(DebugContentBounds, cLeft);
                Canvas.SetTop(DebugContentBounds, 64);
                DebugContentBounds.Width = Body.Width;
                DebugContentBounds.Height = Body.Height;

                Canvas.SetLeft(DebugHud, iLeft + 8);
                Canvas.SetTop(DebugHud, curH + 6);
                double overheadDbg = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase) ? 116.0 : 80.0;
                double slackX = Math.Max(0, (curW - 32) - Body.Width);
                double slackY = Math.Max(0, (curH - overheadDbg) - Body.Height);
                string tabName = navigation != null ? PanelTab.All[navigation.SelectedIndex].Name : "Tab";
                DebugHudText.Text = $"Tab: {tabName} | Island: {curW:0}x{curH:0} | Content: {Body.Width:0}x{Body.Height:0} | Safe Inset: 16/14 DIP | Slack: X={slackX:0} Y={slackY:0} DIP";
            }
            else
            {
                LayoutDebugOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void UpdateBottomNeonLine()
    {
        double intensity = settings.Current.NeonIntensity;
        var activeNeonColor = (Color)ColorConverter.ConvertFromString(settings.Current.Theme == "Cyberpunk" ? "#FF2E97" : "#00E5FF");

        if (!settings.Current.BottomNeonLine)
        {
            BottomEdge.Visibility = Visibility.Collapsed;
            return;
        }
        BottomEdge.Visibility = Visibility.Visible;

        if (settings.Current.NeonLoadReactive && system != null && (system.CpuPercent > 85 || alerts.HasActiveAlert))
        {
            BottomEdge.Background = (Brush)FindResource("Theme.Warning");
            BottomNeonGlow.Color = (Color)ColorConverter.ConvertFromString(settings.Current.Theme == "Cyberpunk" ? "#FCEE0A" : "#FFA726");
            BottomNeonGlow.BlurRadius = 12 * intensity;
            BottomNeonGlow.Opacity = 0.9 * intensity;
            return;
        }

        if (settings.Current.NeonMusicReactive && music != null && music.Playing)
        {
            BottomEdge.Background = (Brush)FindResource("Theme.NeonLine");
            BottomNeonGlow.Color = activeNeonColor;
            double energy = music.Spectrum?.Levels is { Length: > 0 } lvls ? lvls.Average() : 0.2;
            double pulse = Math.Clamp(energy * 2.5, 0.1, 1.0);
            BottomNeonGlow.BlurRadius = (8 + 10 * pulse) * intensity;
            BottomNeonGlow.Opacity = Math.Clamp(0.5 + 0.5 * pulse, 0.4, 1.0) * intensity;
            return;
        }

        BottomEdge.Background = (Brush)FindResource("Theme.NeonLine");
        BottomNeonGlow.Color = activeNeonColor;
        BottomNeonGlow.BlurRadius = 8 * intensity;
        BottomNeonGlow.Opacity = 0.75 * intensity;
    }
}
