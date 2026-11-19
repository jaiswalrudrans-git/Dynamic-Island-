namespace Island.Core.Motion;

public sealed class PanelMotion
{
    private readonly SpringValue shape = new();
    private double closingFrom;
    private double closingTime;
    private double revealTime;

    public bool Expanded { get; private set; }
    public double Progress => shape.Value;
    public double HeaderOpacity => Math.Clamp(revealTime / .18, 0, 1);
    public double BodyOpacity => Math.Clamp((revealTime - .03) / .18, 0, 1);

    public void Target(bool expanded)
    {
        if (Expanded == expanded) return;
        Expanded = expanded;
        closingFrom = Progress;
        closingTime = 0;
    }

    public bool Advance(double seconds, bool reduced)
    {
        bool moving;
        if (Expanded)
        {
            if (reduced)
            {
                shape.Set(Math.Min(1, Progress + seconds / .1));
                moving = Progress != 1;
            }
            else moving = shape.Advance(1, seconds);

            if (Progress >= .6 || reduced)
                revealTime = Math.Min(.21, revealTime + seconds * (reduced ? 2.1 : 1));
            return moving || revealTime < .21;
        }

        // The content gets the first 120 ms. A partly revealed panel needs less lead time.
        double fadeTime = Math.Min(seconds, revealTime / 1.75);
        revealTime = Math.Max(0, revealTime - seconds * 1.75);
        if (reduced)
            shape.Set(Math.Max(0, Progress - seconds / .1));
        else if (Progress != 0 && seconds > fadeTime)
        {
            closingTime += seconds - fadeTime;
            double t = Math.Min(1, closingTime / .32);
            double ease = t < .5 ? 4 * t * t * t : 1 - 4 * Math.Pow(1 - t, 3);
            double slope = 12 * Math.Pow(Math.Min(t, 1 - t), 2);
            shape.Set(closingFrom * (1 - ease), -closingFrom * slope / .32);
        }
        return Progress != 0 || revealTime > 0;
    }
}
