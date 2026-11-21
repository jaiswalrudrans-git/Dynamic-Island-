using System.Windows.Controls;
using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow
{
    private void InitializeTabsClipboard(AppSettings current)
    {
        // Tabs & Navigation
        TabBarPositionCombo.ItemsSource = new[] { "Below clock", "Left of clock" };
        TabBarPositionCombo.SelectedItem = string.Equals(current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase) ? "Left of clock" : "Below clock";
        TabBarPositionCombo.SelectionChanged += (_, _) =>
        {
            if (TabBarPositionCombo.SelectedItem is string pos)
                save(s => s with { TabBarPosition = pos == "Left of clock" ? "LeftOfClock" : "BelowClock" });
        };

        TabLabelModeCombo.ItemsSource = new[] { "Selected tab only", "All tabs", "Icons only" };
        TabLabelModeCombo.SelectedItem = current.TabLabelMode switch
        {
            "All" => "All tabs",
            "None" => "Icons only",
            _ => "Selected tab only"
        };
        TabLabelModeCombo.SelectionChanged += (_, _) =>
        {
            if (TabLabelModeCombo.SelectedItem is string mode)
            {
                string token = mode switch
                {
                    "All tabs" => "All",
                    "Icons only" => "None",
                    _ => "SelectedOnly"
                };
                save(s => s with { TabLabelMode = token });
            }
        };

        HeaderMouseWheelCheck.IsChecked = current.HeaderMouseWheelTabSwitch;
        HeaderMouseWheelCheck.Click += (_, _) => save(s => s with { HeaderMouseWheelTabSwitch = HeaderMouseWheelCheck.IsChecked == true });
        TabSwitchOnHoverCheck.IsChecked = current.TabSwitchOnHover;
        TabSwitchOnHoverCheck.Click += (_, _) => save(s => s with { TabSwitchOnHover = TabSwitchOnHoverCheck.IsChecked == true });

        // Clipboard
        ClipboardEnabledCheck.IsChecked = current.ClipboardEnabled;
        ClipboardEnabledCheck.Click += (_, _) => save(s => s with { ClipboardEnabled = ClipboardEnabledCheck.IsChecked == true });
        ClipboardHistorySlider.Value = current.ClipboardHistorySize;
        Bind(ClipboardHistorySlider, s => s with { ClipboardHistorySize = (int)ClipboardHistorySlider.Value });
        ClipboardRememberCheck.IsChecked = current.ClipboardRememberAcrossRestarts;
        ClipboardRememberCheck.Click += (_, _) => save(s => s with { ClipboardRememberAcrossRestarts = ClipboardRememberCheck.IsChecked == true });
        ClipboardFilterSecretsCheck.IsChecked = current.ClipboardFilterSecrets;
        ClipboardFilterSecretsCheck.Click += (_, _) => save(s => s with { ClipboardFilterSecrets = ClipboardFilterSecretsCheck.IsChecked == true });
        ClipboardCollapseCheck.IsChecked = current.ClipboardCollapseAfterPaste;
        ClipboardCollapseCheck.Click += (_, _) => save(s => s with { ClipboardCollapseAfterPaste = ClipboardCollapseCheck.IsChecked == true });

        ClipboardAutoExpireCombo.ItemsSource = new[] { "Never", "24 hours", "7 days", "30 days" };
        ClipboardAutoExpireCombo.SelectedItem = current.ClipboardAutoExpireHours switch
        {
            24 => "24 hours",
            168 => "7 days",
            720 => "30 days",
            _ => "Never"
        };
        ClipboardAutoExpireCombo.SelectionChanged += (_, _) =>
        {
            if (ClipboardAutoExpireCombo.SelectedItem is string expire)
            {
                int hours = expire switch
                {
                    "24 hours" => 24,
                    "7 days" => 168,
                    "30 days" => 720,
                    _ => 0
                };
                save(s => s with { ClipboardAutoExpireHours = hours });
            }
        };

        ClipboardClickActionCombo.ItemsSource = new[] { "Paste into target app", "Copy to clipboard" };
        ClipboardClickActionCombo.SelectedItem = current.ClipboardClickAction == "Copy" ? "Copy to clipboard" : "Paste into target app";
        ClipboardClickActionCombo.SelectionChanged += (_, _) =>
        {
            if (ClipboardClickActionCombo.SelectedItem is string act)
                save(s => s with { ClipboardClickAction = act == "Copy to clipboard" ? "Copy" : "Paste" });
        };

        // Screenshots
        ScreenshotsEnabledCheck.IsChecked = current.ScreenshotsEnabled;
        ScreenshotsEnabledCheck.Click += (_, _) => save(s => s with { ScreenshotsEnabled = ScreenshotsEnabledCheck.IsChecked == true });
        ScreenshotsClipboardCheck.IsChecked = current.ScreenshotsIncludeClipboard;
        ScreenshotsClipboardCheck.Click += (_, _) => save(s => s with { ScreenshotsIncludeClipboard = ScreenshotsClipboardCheck.IsChecked == true });
        ScreenshotsSaveSnipsCheck.IsChecked = current.ScreenshotsSaveClipboardToFolder;
        ScreenshotsSaveSnipsCheck.Click += (_, _) => save(s => s with { ScreenshotsSaveClipboardToFolder = ScreenshotsSaveSnipsCheck.IsChecked == true });

        ScreenshotsThumbSizeCombo.ItemsSource = new[] { "Compact (180px)", "Medium (240px)", "Large (320px)" };
        ScreenshotsThumbSizeCombo.SelectedItem = current.ScreenshotsThumbnailSize switch
        {
            <= 120 => "Compact (180px)",
            >= 240 => "Large (320px)",
            _ => "Medium (240px)"
        };
        ScreenshotsThumbSizeCombo.SelectionChanged += (_, _) =>
        {
            if (ScreenshotsThumbSizeCombo.SelectedItem is string sz)
            {
                int px = sz.StartsWith("Compact") ? 90 : sz.StartsWith("Large") ? 240 : 160;
                save(s => s with { ScreenshotsThumbnailSize = px });
            }
        };

        ScreenshotsClickActionCombo.ItemsSource = new[] { "Copy to clipboard", "Open in default viewer", "Paste into target app" };
        ScreenshotsClickActionCombo.SelectedItem = current.ScreenshotsClickAction switch
        {
            "Open" => "Open in default viewer",
            "Paste" => "Paste into target app",
            _ => "Copy to clipboard"
        };
        ScreenshotsClickActionCombo.SelectionChanged += (_, _) =>
        {
            if (ScreenshotsClickActionCombo.SelectedItem is string act)
            {
                string token = act.StartsWith("Open") ? "Open" : act.StartsWith("Paste") ? "Paste" : "Copy";
                save(s => s with { ScreenshotsClickAction = token });
            }
        };
    }
}
