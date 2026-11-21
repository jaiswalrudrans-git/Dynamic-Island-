using System.Windows;
using System.Windows.Controls;
using Island.App.Features.Controls.ViewModels;

namespace Island.App.Features.Controls.Views;

public partial class QuickControlsPage : UserControl
{
    private readonly QuickControlsViewModel model;

    public QuickControlsPage(QuickControlsViewModel model)
    {
        InitializeComponent();
        this.model = model;
        DataContext = model;
    }

    private void VolumeSlider_DragStarted(object? sender, EventArgs e) => model.NotifyVolumeDragStarted();
    private void VolumeSlider_DragCompleted(object? sender, EventArgs e) => model.CommitVolume();
    private void BrightnessSlider_DragStarted(object? sender, EventArgs e) => model.NotifyBrightnessDragStarted();
    private void BrightnessSlider_DragCompleted(object? sender, EventArgs e) => model.CommitBrightness();

    private void VolumeMuteClick(object sender, RoutedEventArgs e) => model.ToggleMute();
    private void MicClick(object sender, RoutedEventArgs e) => model.ToggleMicMute();
    private async void WifiClick(object sender, RoutedEventArgs e) => await model.ToggleWifiAsync();
    private async void BluetoothClick(object sender, RoutedEventArgs e) => await model.ToggleBluetoothAsync();
    private void DndClick(object sender, RoutedEventArgs e) => model.OpenDndSettings();
    private void ScreenshotClick(object sender, RoutedEventArgs e) => model.TakeScreenshot();
    private void LockClick(object sender, RoutedEventArgs e) => model.LockPc();
    private void SleepClick(object sender, RoutedEventArgs e) => model.SleepPc();
    private void TaskManagerClick(object sender, RoutedEventArgs e) => model.OpenTaskManager();
}
