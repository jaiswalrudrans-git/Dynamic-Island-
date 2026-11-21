using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow
{
    private void InitializeSystemAlerts(AppSettings current)
    {
        // System
        CollapsedStat.ItemsSource = new[] { "None", "Cpu", "Ram", "Network", "Battery", "CpuRam" };
        CollapsedStat.SelectedItem = current.CollapsedStat;
        ProcessSort.ItemsSource = new[] { "Cpu", "Memory" };
        ProcessSort.SelectedItem = current.SystemProcessSort;

        // Alerts
        AlertsEnabledCheck.IsChecked = current.AlertsEnabled;
        AlertVolumeCheck.IsChecked = current.AlertVolumeOsdEnabled;
        AlertBrightnessCheck.IsChecked = current.AlertBrightnessOsdEnabled;
        AlertMicCheck.IsChecked = current.AlertMicOsdEnabled;
        AlertBatteryCheck.IsChecked = current.AlertBatteryEnabled;
        AlertTempCheck.IsChecked = current.AlertTemperatureEnabled;
        AlertSoundCheck.IsChecked = current.AlertSoundEnabled;
        AlertDurationSlider.Value = current.AlertDurationSeconds;
        LayoutDebugCheck.IsChecked = current.LayoutDebug;

        CollapsedStat.SelectionChanged += (_, _) => save(s => s with { CollapsedStat = (string)CollapsedStat.SelectedItem });
        ProcessSort.SelectionChanged += (_, _) => save(s => s with { SystemProcessSort = (string)ProcessSort.SelectedItem });
        AlertsEnabledCheck.Click += (_, _) => save(s => s with { AlertsEnabled = AlertsEnabledCheck.IsChecked == true });
        AlertVolumeCheck.Click += (_, _) => save(s => s with { AlertVolumeOsdEnabled = AlertVolumeCheck.IsChecked == true });
        AlertBrightnessCheck.Click += (_, _) => save(s => s with { AlertBrightnessOsdEnabled = AlertBrightnessCheck.IsChecked == true });
        AlertMicCheck.Click += (_, _) => save(s => s with { AlertMicOsdEnabled = AlertMicCheck.IsChecked == true });
        AlertBatteryCheck.Click += (_, _) => save(s => s with { AlertBatteryEnabled = AlertBatteryCheck.IsChecked == true });
        AlertTempCheck.Click += (_, _) => save(s => s with { AlertTemperatureEnabled = AlertTempCheck.IsChecked == true });
        AlertSoundCheck.Click += (_, _) => save(s => s with { AlertSoundEnabled = AlertSoundCheck.IsChecked == true });
        LayoutDebugCheck.Click += (_, _) => save(s => s with { LayoutDebug = LayoutDebugCheck.IsChecked == true });

        Bind(AlertDurationSlider, s => s with { AlertDurationSeconds = (int)AlertDurationSlider.Value });
    }
}
