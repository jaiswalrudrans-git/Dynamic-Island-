using System.Windows;
using System.Windows.Media;

namespace Island.App.Views;

// Mutate a retained clip in window coordinates. Layout and HWND bounds stay fixed.
internal sealed class NotchClip
{
    private readonly PathFigure outline = new() { IsClosed = true };
    private readonly LineSegment top = new(), right = new(), bottom = new(), left = new();
    private readonly QuadraticBezierSegment shoulderR = new(), cornerR = new(), cornerL = new(), shoulderL = new();
    public PathGeometry Geometry { get; } = new();

    public NotchClip()
    {
        foreach (var segment in new PathSegment[] { top, shoulderR, right, cornerR, bottom, cornerL, left, shoulderL })
            outline.Segments.Add(segment);
        Geometry.Figures.Add(outline);
        Update(0);
    }

    public void Update(double progress)
    {
        double halfWidth = 65 + 255 * progress;
        double h = 32 + 288 * progress;
        double shoulder = 8 + 5 * progress;
        double radius = 16 + 16 * progress;
        double x1 = 380 - halfWidth, x2 = 380 + halfWidth;
        double l = x1 + shoulder, r = x2 - shoulder;
        outline.StartPoint = new Point(x1, 0);
        top.Point = new Point(x2, 0);
        shoulderR.Point1 = new Point(r, 0);
        shoulderR.Point2 = new Point(r, shoulder);
        right.Point = new Point(r, h - radius);
        cornerR.Point1 = new Point(r, h);
        cornerR.Point2 = new Point(r - radius, h);
        bottom.Point = new Point(l + radius, h);
        cornerL.Point1 = new Point(l, h);
        cornerL.Point2 = new Point(l, h - radius);
        left.Point = new Point(l, shoulder);
        shoulderL.Point1 = new Point(l, 0);
        shoulderL.Point2 = outline.StartPoint;
    }
}
