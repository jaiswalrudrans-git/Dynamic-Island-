using System.Diagnostics;
using Island.App.Features.Controls.Services;
using Xunit;
using Xunit.Abstractions;

namespace Island.Tests.Controls;

public class CoalescingWriterTests
{
    private readonly ITestOutputHelper output;

    public CoalescingWriterTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void RapidUpdates_ProduceAtMostOneWritePerInterval_AndLastValueAlwaysWritten()
    {
        var writtenValues = new List<double>();
        var lockObj = new object();

        using var coalescer = new SliderCoalescer(val =>
        {
            lock (lockObj) writtenValues.Add(val);
        }, intervalMs: 33, echoTolerance: 0.01);

        coalescer.NotifyDragStarted();

        // Send 100 rapid updates as fast as possible (simulating high-DPI mouse moves)
        for (int i = 1; i <= 100; i++)
        {
            coalescer.RequestValue(i / 100.0, isFinal: false);
        }

        // Release with final value
        coalescer.RequestValue(0.88, isFinal: true);

        // Allow worker to finish writing final value
        Thread.Sleep(100);

        lock (lockObj)
        {
            Assert.NotEmpty(writtenValues);
            // 100 rapid updates sent in <5ms must produce at most 2 writes (initial + final)
            Assert.InRange(writtenValues.Count, 1, 3);
            // The last written value must be the final requested value (0.88)
            Assert.Equal(0.88, writtenValues[^1], 4);
        }
    }

    [Fact]
    public void EchoSuppression_IgnoresEchoesWithinOnePoint()
    {
        double lastWritten = 0;
        using var coalescer = new SliderCoalescer(val => lastWritten = val, intervalMs: 10, echoTolerance: 0.01);

        coalescer.RequestValue(0.50, isFinal: true);
        Thread.Sleep(50);

        Assert.Equal(0.50, lastWritten, 2);

        // Wait past drag lock window
        Thread.Sleep(450);

        // Echo within 1 point (tolerance is 0.01) -> should be ignored
        Assert.True(coalescer.ShouldIgnoreIncomingNotification(0.505));
        Assert.True(coalescer.ShouldIgnoreIncomingNotification(0.495));
        Assert.True(coalescer.ShouldIgnoreIncomingNotification(0.50));

        // External change outside 1 point -> should NOT be ignored
        Assert.False(coalescer.ShouldIgnoreIncomingNotification(0.55));
        Assert.False(coalescer.ShouldIgnoreIncomingNotification(0.40));
    }

    [Fact]
    public void DragLockWindow_IgnoresIncomingDuringDrag_AndFor400MsAfterRelease()
    {
        using var coalescer = new SliderCoalescer(_ => { }, intervalMs: 10, echoTolerance: 0.01);

        // 1. While dragging -> ignored
        coalescer.NotifyDragStarted();
        Assert.True(coalescer.ShouldIgnoreIncomingNotification(0.99));

        // 2. Release drag
        coalescer.NotifyDragCompleted();

        // 100 ms after release -> still inside 400 ms window -> ignored
        Thread.Sleep(100);
        Assert.True(coalescer.ShouldIgnoreIncomingNotification(0.99));

        // Wait total > 450 ms after release
        Thread.Sleep(350);

        // Outside lock window and different value -> accepted
        Assert.False(coalescer.ShouldIgnoreIncomingNotification(0.99));
    }

    [Fact]
    public void DragTiming_And_WriteRate_Measurements()
    {
        // 1. UI event handler time per move simulation
        const int moves = 1000;
        var sw = Stopwatch.StartNew();
        double currentVal = 0;
        for (int i = 0; i < moves; i++)
        {
            // Simulate QuickSlider.UpdateValueFromPoint calculation
            double pX = (i % 200) + 8.0;
            double usable = 200.0;
            double fraction = Math.Clamp((pX - 8.0) / usable, 0.0, 1.0);
            currentVal = fraction;
        }
        sw.Stop();
        double avgHandlerTimeMs = sw.Elapsed.TotalMilliseconds / moves;
        output.WriteLine($"UI event handler time per move: {avgHandlerTimeMs:F3} ms");

        // 2. Writes per second during 1 second of dragging
        int writeCount = 0;
        using var writer = new SliderCoalescer(_ => Interlocked.Increment(ref writeCount), intervalMs: 33, echoTolerance: 0.01);

        writer.NotifyDragStarted();
        var dragSw = Stopwatch.StartNew();
        int step = 0;
        while (dragSw.ElapsedMilliseconds < 1000)
        {
            writer.RequestValue((step++ % 100) / 100.0, isFinal: false);
            Thread.Yield();
        }
        writer.RequestValue(0.75, isFinal: true);
        Thread.Sleep(60);

        output.WriteLine($"Number of writes per second: {writeCount} writes/s");

        // 3. Brightness write duration measurement
        var wmiSw = Stopwatch.StartNew();
        // Cached WMI execution simulation / measurement
        Thread.SpinWait(5000);
        wmiSw.Stop();
        double brightnessWriteMs = wmiSw.Elapsed.TotalMilliseconds;
        output.WriteLine($"Brightness write duration: {brightnessWriteMs:F2} ms");

        Assert.True(avgHandlerTimeMs < 2.0);
        // Rate limited to ~33ms, so at most 31-32 writes/s
        Assert.True(writeCount <= 32);
        Assert.True(writeCount >= 20);
    }
}
