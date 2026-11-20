using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Island.App.ViewModels;
using Island.Core.Focus;

namespace Island.App.Views;

public partial class FocusPage : UserControl
{
    private readonly FocusViewModel model;

    public FocusPage(FocusViewModel model)
    {
        InitializeComponent();
        this.model = model;
        DataContext = model;

        model.PropertyChanged += OnModelPropertyChanged;
        UpdateProgressRing(model.Progress);
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(FocusViewModel.Progress))
        {
            UpdateProgressRing(model.Progress);
        }
    }

    private void UpdateProgressRing(double progress)
    {
        if (RingPath == null) return;

        if (progress <= 0 || !model.ShowProgress)
        {
            RingPath.Data = null;
            return;
        }

        double p = Math.Clamp(progress, 0, 0.9999);
        double angle = p * 2 * Math.PI;

        double cx = 75;
        double cy = 75;
        double r = 68;

        double startX = cx;
        double startY = cy - r;

        double endX = cx + r * Math.Sin(angle);
        double endY = cy - r * Math.Cos(angle);

        bool isLargeArc = p > 0.5;

        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = new Point(startX, startY),
            IsClosed = false,
            IsFilled = false
        };

        var arc = new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(r, r),
            IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Clockwise
        };

        figure.Segments.Add(arc);
        geometry.Figures.Add(figure);
        RingPath.Data = geometry;
    }

    private void PomodoroModeClick(object sender, RoutedEventArgs e) => model.SetMode(FocusMode.Pomodoro);
    private void TimerModeClick(object sender, RoutedEventArgs e) => model.SetMode(FocusMode.Timer);
    private void StopwatchModeClick(object sender, RoutedEventArgs e) => model.SetMode(FocusMode.Stopwatch);

    private void Timer15Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(15);
    private void Timer25Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(25);
    private void Timer45Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(45);
    private void Timer60Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(60);

    private void PlayPauseClick(object sender, RoutedEventArgs e) => model.ToggleStartPause();
    private void ResetClick(object sender, RoutedEventArgs e) => model.Reset();
    private void SkipClick(object sender, RoutedEventArgs e) => model.Skip();
    private void LapClick(object sender, RoutedEventArgs e) => model.AddLap();
}
