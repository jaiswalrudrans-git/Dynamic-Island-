using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Island.App.Header.Views;
using Island.App.Shell;
using Island.Core.Settings;
using Xunit;

namespace Island.Tests.Shell;

public sealed class CollapsedNotchSizingTests
{
    [Fact]
    public void CalculateFromItems_SumsVisibleItemsAndGaps_WithSidePaddingAndNoHiddenSpace()
    {
        var itemsWithoutEqualizer = new (double Width, bool IsVisible)[]
        {
            (22.0, true),   // Cover
            (60.0, true),   // Clock
            (25.0, false),  // Equalizer (hidden)
            (80.0, false)   // Lyrics (hidden)
        };

        double widthWithoutEq = CollapsedNotchLayout.CalculateFromItems(
            sidePadding: 12.0,
            gap: 8.0,
            floor: 96.0,
            items: itemsWithoutEqualizer);

        // 12 + 22 + 8 + 60 + 12 = 114
        Assert.Equal(114.0, widthWithoutEq);

        var itemsWithEqualizer = new (double Width, bool IsVisible)[]
        {
            (22.0, true),   // Cover
            (60.0, true),   // Clock
            (25.0, true),   // Equalizer (visible)
            (80.0, false)   // Lyrics (hidden)
        };

        double widthWithEq = CollapsedNotchLayout.CalculateFromItems(
            sidePadding: 12.0,
            gap: 8.0,
            floor: 96.0,
            items: itemsWithEqualizer);

        // 12 + 22 + 8 + 60 + 8 + 25 + 12 = 147 -> snapped to 2 DIP = 148
        Assert.Equal(148.0, widthWithEq);
    }

    [Fact]
    public void CalculateWidth_TimeOnly_ClockCenteredWith14DipPaddingAndFloor()
    {
        double widthSmall = CollapsedNotchLayout.CalculateWidth(clockWidth: 50.0, isMusicPlaying: false);
        Assert.Equal(96.0, widthSmall);

        double widthLarger = CollapsedNotchLayout.CalculateWidth(clockWidth: 72.0, isMusicPlaying: false);
        Assert.Equal(100.0, widthLarger);
    }

    [Fact]
    public void CalculateWidth_MusicPlaying_EqualsPaddingPlusCoverPlusGapPlusClock()
    {
        double width = CollapsedNotchLayout.CalculateWidth(
            clockWidth: 60.0,
            isMusicPlaying: true,
            showCover: true,
            coverWidth: 22.0,
            showEqualizer: false,
            showText: false);

        Assert.Equal(114.0, width);

        double widthWithHidden = CollapsedNotchLayout.CalculateWidth(
            clockWidth: 60.0,
            isMusicPlaying: true,
            showCover: true,
            coverWidth: 22.0,
            showEqualizer: false,
            equalizerWidth: 25.0,
            showText: false,
            textWidth: 100.0);

        Assert.Equal(114.0, widthWithHidden);
    }

    [Fact]
    public void CaptureCollapsedNotchStates_100Dpi()
    {
        var thread = new Thread(() =>
        {
            string outDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "artifacts", "screens");
            Directory.CreateDirectory(outDir);

            RenderAndSaveState("collapsed_time_only.png", outDir, isMusicPlaying: false, hasEqualizer: false);
            RenderAndSaveState("collapsed_music_playing.png", outDir, isMusicPlaying: true, hasEqualizer: false);
            RenderAndSaveState("collapsed_music_equalizer.png", outDir, isMusicPlaying: true, hasEqualizer: true);
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    private static void RenderAndSaveState(string fileName, string outDir, bool isMusicPlaying, bool hasEqualizer)
    {
        double clockWidth = 60.0;
        double coverWidth = 22.0;
        double eqWidth = 25.0;

        double notchWidth = CollapsedNotchLayout.CalculateWidth(
            clockWidth: clockWidth,
            isMusicPlaying: isMusicPlaying,
            showCover: isMusicPlaying,
            coverWidth: coverWidth,
            showEqualizer: hasEqualizer,
            equalizerWidth: eqWidth);

        double notchHeight = 35.0;

        var container = new Grid
        {
            Width = notchWidth + 40,
            Height = notchHeight + 20,
            Background = Brushes.Transparent
        };

        var notchClip = new NotchClip();
        double centerX = (notchWidth + 40) / 2.0;
        notchClip.Update(0, notchWidth, notchWidth, notchHeight, centerX);

        var notchBorder = new System.Windows.Shapes.Path
        {
            Fill = new SolidColorBrush(Color.FromRgb(15, 17, 23)),
            Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
            StrokeThickness = 1.0,
            Data = notchClip.Geometry
        };
        container.Children.Add(notchBorder);

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (isMusicPlaying)
        {
            var cover = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(30, 35, 45)),
                Margin = new Thickness(0, 0, 8, 0),
                Child = new TextBlock
                {
                    Text = "\uE8D6",
                    FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            stack.Children.Add(cover);
        }

        var clockFace = new ClockFace { ClockStyleProp = "Simple" };
        var settings = new AppSettings { ClockStyle = "Simple", ClockColor = "Gradient" };
        clockFace.ApplySettings(settings);
        clockFace.Width = clockWidth;
        clockFace.Height = 24;
        stack.Children.Add(clockFace);

        if (hasEqualizer)
        {
            var eq = new Border
            {
                Width = 25,
                Height = 16,
                Margin = new Thickness(8, 0, 0, 0),
                Background = Brushes.Transparent
            };
            var eqStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            double[] heights = [8, 14, 11, 15, 9];
            foreach (var h in heights)
            {
                eqStack.Children.Add(new Border
                {
                    Width = 3,
                    Height = h,
                    Margin = new Thickness(1, 0, 1, 0),
                    CornerRadius = new CornerRadius(1),
                    Background = new SolidColorBrush(Color.FromRgb(0, 229, 255))
                });
            }
            eq.Child = eqStack;
            stack.Children.Add(eq);
        }

        container.Children.Add(stack);

        container.Measure(new Size(container.Width, container.Height));
        container.Arrange(new Rect(0, 0, container.Width, container.Height));
        container.UpdateLayout();

        var rtb = new RenderTargetBitmap((int)container.Width, (int)container.Height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(container);

        string filePath = System.IO.Path.Combine(outDir, fileName);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(filePath);
        encoder.Save(fs);
    }
}
