using System.Windows;
using System.Windows.Controls;
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
    }

    private void PomodoroModeClick(object sender, RoutedEventArgs e) => model.SetMode(FocusMode.Pomodoro);
    private void TimerModeClick(object sender, RoutedEventArgs e) => model.SetMode(FocusMode.Timer);
    private void StopwatchModeClick(object sender, RoutedEventArgs e) => model.SetMode(FocusMode.Stopwatch);

    private void Timer5Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(5);
    private void Timer10Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(10);
    private void Timer15Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(15);
    private void Timer25Click(object sender, RoutedEventArgs e) => model.SetTimerMinutes(25);

    private void PlayPauseClick(object sender, RoutedEventArgs e) => model.ToggleStartPause();
    private void ResetClick(object sender, RoutedEventArgs e) => model.Reset();
    private void SkipClick(object sender, RoutedEventArgs e) => model.Skip();
    private void LapClick(object sender, RoutedEventArgs e) => model.AddLap();
}
