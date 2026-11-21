using System.Windows;
using System.Windows.Media;
using Island.App.Features.Music.Services;

namespace Island.App.Features.Music.Views;

public sealed class SpectrumMeter : FrameworkElement
{
    private LoopbackSpectrum? source;
    internal event Action? AvailabilityChanged;

    internal void Attach(LoopbackSpectrum? value)
    {
        if (source != null) source.Frame -= Refresh;
        source = value;
        if (source != null) source.Frame += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        bool available = source?.Available == true;
        var nextVis = available ? Visibility.Visible : Visibility.Collapsed;
        if (Visibility != nextVis)
        {
            Visibility = nextVis;
            AvailabilityChanged?.Invoke();
        }
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Visibility == Visibility.Collapsed || source?.Available != true)
            return new Size(0, 0);
        return new Size(25, 16);
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
