using System.Windows;
using System.Windows.Media;
using Island.App.Features.Music.Services;

namespace Island.App.Features.Music.Views;

public sealed class SpectrumMeter : FrameworkElement
{
    private LoopbackSpectrum? source;
    internal void Attach(LoopbackSpectrum? value)
    {
        if (source != null) source.Frame -= Refresh;
        source = value;
        if (source != null) source.Frame += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        Visibility = source?.Available == true ? Visibility.Visible : Visibility.Hidden;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (source?.Available != true) return;
        double step = ActualWidth / source.Levels.Length;
        for (int i = 0; i < source.Levels.Length; i++)
        {
            double height = Math.Max(1, ActualHeight * source.Levels[i]);
            dc.DrawRoundedRectangle(Brushes.WhiteSmoke, null,
                new Rect(step * i, (ActualHeight - height) / 2, step * .62, height), 1.4, 1.4);
        }
    }
}
