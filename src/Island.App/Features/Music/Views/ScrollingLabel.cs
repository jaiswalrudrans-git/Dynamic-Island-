using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Island.App.Features.Music.Views;

public sealed class ScrollingLabel : Canvas
{
    private readonly TranslateTransform offset = new();
    private readonly TextBlock caption;
    private bool active, reduced;

    public ScrollingLabel()
    {
        ClipToBounds = true;
        caption = new TextBlock { Foreground = Brushes.WhiteSmoke, FontSize = 12, FontWeight = FontWeights.SemiBold,
            RenderTransform = offset };
        Children.Add(caption);
        SizeChanged += (_, _) => ArrangeCaption();
    }

    internal void Update(string text, bool show, bool reduceMotion)
    {
        if (active == show && reduced == reduceMotion && caption.Text == text) return;
        active = show;
        reduced = reduceMotion;
        caption.Text = text;
        Visibility = show ? Visibility.Visible : Visibility.Hidden;
        ArrangeCaption();
    }

    private void ArrangeCaption()
    {
        offset.BeginAnimation(TranslateTransform.XProperty, null);
        if (!active || ActualWidth <= 0) return;
        caption.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double overflow = caption.DesiredSize.Width - ActualWidth;
        SetTop(caption, (ActualHeight - caption.DesiredSize.Height) / 2);
        offset.X = overflow < 0 ? -overflow / 2 : 0;
        if (overflow <= 1 || reduced) return;
        var travel = TimeSpan.FromSeconds(overflow / 32);
        var pause = TimeSpan.FromSeconds(1);
        var scroll = new DoubleAnimationUsingKeyFrames { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
        scroll.KeyFrames.Add(new LinearDoubleKeyFrame(0, pause));
        scroll.KeyFrames.Add(new LinearDoubleKeyFrame(-overflow, pause + travel));
        scroll.KeyFrames.Add(new LinearDoubleKeyFrame(-overflow, pause + travel + pause));
        offset.BeginAnimation(TranslateTransform.XProperty, scroll);
    }
}
