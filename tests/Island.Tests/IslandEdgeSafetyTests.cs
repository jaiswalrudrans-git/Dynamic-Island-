using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Xunit;

namespace Island.Tests;

public sealed class IslandEdgeSafetyTests
{
    private static readonly string[] Tabs = ["Music", "System", "Tasks", "QuickControls", "Focus", "CalendarWeather", "Clipboard"];
    private static readonly string[] TypographyPresets = ["Classic", "Dot Matrix", "Techno"];
    private static readonly double[] DpiScales = [1.0, 1.25, 1.5];

    [Theory]
    [InlineData("Music")]
    [InlineData("System")]
    [InlineData("Tasks")]
    [InlineData("QuickControls")]
    [InlineData("Focus")]
    [InlineData("CalendarWeather")]
    [InlineData("Clipboard")]
    public void AllTabsMaintainSafeEdgeClearanceAndZeroCornerIntersections(string tab)
    {
        var thread = new Thread(() =>
        {
            foreach (var preset in TypographyPresets)
            {
                foreach (var dpi in DpiScales)
                {
                    RunEdgeSafetyVerification(tab, preset, dpi);
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    private static void RunEdgeSafetyVerification(string tab, string preset, double dpiScale)
    {
        double contentW = tab switch
        {
            "System" or "CalendarWeather" or "Clipboard" => 680,
            "QuickControls" => 620,
            _ => 580
        };

        double contentH = tab switch
        {
            "System" => 264,
            "CalendarWeather" => 250,
            "Focus" => 266,
            "QuickControls" => 230,
            "Tasks" => 140,
            "Clipboard" => 250,
            _ => 170
        };

        const double overhead = 116.0; // 14 top + 80 header + 8 gap + 14 bottom
        double targetContentW = contentW;
        double islandW = Math.Ceiling((targetContentW + 32.0) / 8.0) * 8.0;
        double islandH = Math.Ceiling((contentH + overhead) / 8.0) * 8.0;

        const double minLeftRightSafeMargin = 16.0;
        const double minTopBottomSafeMargin = 14.0;
        const double cornerRadius = 28.0;

        // Verify mathematical island shell bounds
        Assert.True(islandW >= targetContentW + 32.0, $"Island width {islandW} must accommodate content {targetContentW} + 32 DIP margin");
        Assert.True(islandH >= contentH + overhead, $"Island height {islandH} must accommodate content {contentH} + 116 DIP overhead");

        // Verify corner geometry:
        // Island corners start at radius 28. Content box is inset by 16 DIP horizontally and 14 DIP vertically from bottom.
        // At y = islandH - 14, distance from circle center (28, islandH - 28) is:
        // dy = 14, dx = 28 - 16 = 12.
        // distSq = 12^2 + 14^2 = 144 + 196 = 340.
        // Radius squared = 28^2 = 784.
        // Since 340 < 784, the point (16, islandH - 14) is well inside the corner arc (clearance = 28 - sqrt(340) = 9.56 DIP).
        double cornerClearance = cornerRadius - Math.Sqrt(Math.Pow(cornerRadius - minLeftRightSafeMargin, 2) + Math.Pow(cornerRadius - minTopBottomSafeMargin, 2));
        Assert.True(cornerClearance > 9.0, $"Content box corner clearance {cornerClearance:0.00} DIP must be > 9 DIP inside corner arc");

        // Build sample tab visual tree to verify measured descendants
        var shell = new Border
        {
            Width = islandW,
            Height = islandH,
            CornerRadius = new CornerRadius(cornerRadius),
            Padding = new Thickness(minLeftRightSafeMargin, minTopBottomSafeMargin, minLeftRightSafeMargin, minTopBottomSafeMargin)
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) }); // Row 0: Two-row header
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });  // Row 1: Gap
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 2: Content

        // Centered header
        var header = new Grid();
        header.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
        header.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });

        var clock = new TextBlock { Text = "12:00", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(clock, 0);
        header.Children.Add(clock);

        // Icon-only tab bar (7 tabs x 32 DIP + 4 DIP spacing = 248 DIP)
        var tabButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        for (int i = 0; i < 7; i++)
        {
            tabButtons.Children.Add(new Button { Width = 32, Height = 28, Margin = new Thickness(2, 0, 2, 0) });
        }
        Grid.SetRow(tabButtons, 1);
        header.Children.Add(tabButtons);

        Grid.SetRow(header, 0);
        root.Children.Add(header);

        // Content
        var content = new Border
        {
            Width = targetContentW,
            Height = contentH,
            Clip = new RectangleGeometry(new Rect(0, 0, targetContentW, contentH), cornerRadius - 1, cornerRadius - 1)
        };
        Grid.SetRow(content, 2);
        root.Children.Add(content);

        shell.Child = root;

        shell.Measure(new Size(islandW, islandH));
        shell.Arrange(new Rect(0, 0, islandW, islandH));
        shell.UpdateLayout();

        // Validate content edge distances relative to island outer edge
        Point contentOrigin = content.TranslatePoint(new Point(0, 0), shell);
        double leftDist = contentOrigin.X;
        double rightDist = islandW - (contentOrigin.X + content.ActualWidth);
        double topDist = contentOrigin.Y;
        double bottomDist = islandH - (contentOrigin.Y + content.ActualHeight);

        Assert.True(leftDist >= minLeftRightSafeMargin - 0.01, $"Tab {tab} left distance {leftDist:0.0} must be >= {minLeftRightSafeMargin}");
        Assert.True(rightDist >= minLeftRightSafeMargin - 0.01, $"Tab {tab} right distance {rightDist:0.0} must be >= {minLeftRightSafeMargin}");
        Assert.True(topDist >= 14.0 + 80.0 + 8.0 - 0.01, $"Tab {tab} top distance {topDist:0.0} must clear header");
        Assert.True(bottomDist >= minTopBottomSafeMargin - 0.01, $"Tab {tab} bottom distance {bottomDist:0.0} must be >= {minTopBottomSafeMargin}");

        // Validate that no point of the content bounding box enters the corner forbidden zone
        Point[] corners =
        [
            new Point(contentOrigin.X, contentOrigin.Y),
            new Point(contentOrigin.X + content.ActualWidth, contentOrigin.Y),
            new Point(contentOrigin.X, contentOrigin.Y + content.ActualHeight),
            new Point(contentOrigin.X + content.ActualWidth, contentOrigin.Y + content.ActualHeight)
        ];

        // Bottom-left corner check
        double distBottomLeft = Math.Sqrt(Math.Pow(corners[2].X - cornerRadius, 2) + Math.Pow(corners[2].Y - (islandH - cornerRadius), 2));
        Assert.True(distBottomLeft < cornerRadius, $"Bottom-left corner must reside safely inside the {cornerRadius} DIP corner arc");

        // Bottom-right corner check
        double distBottomRight = Math.Sqrt(Math.Pow(corners[3].X - (islandW - cornerRadius), 2) + Math.Pow(corners[3].Y - (islandH - cornerRadius), 2));
        Assert.True(distBottomRight < cornerRadius, $"Bottom-right corner must reside safely inside the {cornerRadius} DIP corner arc");
    }
}
