using Island.Core.Motion;
using Xunit;

namespace Island.Tests;

public sealed class MotionTests
{
    [Fact]
    public void SpringIsIndependentOfFrameRateAndHasSmallOvershoot()
    {
        var at60 = new SpringValue();
        var at120 = new SpringValue();
        double peak = 0;
        for (int i = 0; i < 60; i++)
        {
            at60.Advance(1, 1d / 60);
            at120.Advance(1, 1d / 120);
            at120.Advance(1, 1d / 120);
            peak = Math.Max(peak, at60.Value);
            Assert.InRange(Math.Abs(at60.Value - at120.Value), 0, .0006);
        }
        Assert.InRange(peak, 1.018, 1.03);
        Assert.False(at60.Advance(1, .1));
        Assert.Equal(1, at60.Value);
    }

    [Fact]
    public void ClosingFadesContentFirstAndReenteringKeepsTheCurrentShape()
    {
        var panel = new PanelMotion();
        panel.Target(true);
        panel.Advance(.015, false);
        Assert.Equal(0, panel.HeaderOpacity);
        for (int i = 0; i < 100; i++) panel.Advance(.01, false);
        Assert.Equal(1, panel.BodyOpacity);
        panel.Target(false);
        panel.Advance(.06, false);
        Assert.InRange(panel.BodyOpacity, 0, .6);
        Assert.Equal(1, panel.Progress);
        panel.Advance(.18, false);
        double halfway = panel.Progress;
        Assert.InRange(halfway, .1, .9);
        panel.Target(true);
        Assert.Equal(halfway, panel.Progress);
        panel.Advance(.001, false);
        Assert.InRange(Math.Abs(panel.Progress - halfway), 0, .02);
        for (int i = 0; i < 100; i++) panel.Advance(.01, false);
        Assert.False(panel.Advance(.01, false));
        Assert.Equal(1, panel.Progress);
    }

    [Fact]
    public void ReducedMotionFinishesQuicklyWithoutOvershoot()
    {
        var panel = new PanelMotion();
        panel.Target(true);
        for (int i = 0; i < 15; i++)
        {
            panel.Advance(.01, true);
            Assert.InRange(panel.Progress, 0, 1);
        }
        Assert.False(panel.Advance(.01, true));
        panel.Target(false);
        Assert.False(panel.Advance(.15, true));
        Assert.Equal(0, panel.Progress);
    }
}
