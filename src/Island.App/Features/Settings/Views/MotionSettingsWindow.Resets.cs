using System.Windows;
using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow
{
    private void ResetCurrentPageClick(object sender, RoutedEventArgs e)
    {
        switch (activePage)
        {
            case "Appearance":
                ThemeSelector.SelectedItem = "Midnight";
                SurfaceSelector.SelectedItem = "Translucent";
                BgTranspSlider.Value = 20;
                ColTranspSlider.Value = 15;
                NeonIntensitySlider.Value = 100;
                BottomNeonLineCheck.IsChecked = false;
                NeonMusicReactiveCheck.IsChecked = true;
                NeonLoadReactiveCheck.IsChecked = true;
                InterfaceSizeSelector.SelectedItem = "Auto";
                UiScaleSlider.Value = 1.0;
                save(s => s with
                {
                    Theme = "Midnight",
                    SurfaceStyle = "Translucent",
                    BackgroundTransparency = 20,
                    CollapsedTransparency = 15,
                    NeonIntensity = 1.0,
                    BottomNeonLine = false,
                    NeonMusicReactive = true,
                    NeonLoadReactive = true,
                    InterfaceSize = "Auto",
                    UiScale = 1.0
                });
                break;

            case "Clock":
                ShowNotchClockCheck.IsChecked = true;
                Clock24HourCheck.IsChecked = true;
                ClockSecondsCheck.IsChecked = false;
                ClockBlinkCheck.IsChecked = false;
                ClockZeroCheck.IsChecked = true;
                ClockSizeSlider.Value = 1.0;
                CurrentStyleLabel.Text = "Simple";
                CurrentColorLabel.Text = "White";
                CustomColorBox.Text = "#00E5FF";
                CustomColorRow.Visibility = Visibility.Collapsed;
                save(s => s with
                {
                    ShowNotchClock = true,
                    ClockStyle = "Simple",
                    ClockColor = "White",
                    ClockCustomColor = "#00E5FF",
                    Use24HourClock = true,
                    ShowSeconds = false,
                    ClockBlinkingColon = false,
                    ClockLeadingZero = true,
                    ClockSizeScale = 1.0
                });
                break;

            case "Typography":
                MatchTypographyCheck.IsChecked = true;
                MatchAccentCheck.IsChecked = false;
                LegibilityGuardCheck.IsChecked = true;
                TypographyScaleSlider.Value = 1.0;
                TypographyWeightCombo.SelectedItem = "Regular";
                TypographyUppercaseCheck.IsChecked = false;
                TypographyTabularCheck.IsChecked = true;
                TypographyGlowCheck.IsChecked = false;
                TypographyGlowSlider.Value = 0.5;
                RoleDisplayCombo.SelectedItem = "Default (From Theme)";
                RoleHeadingCombo.SelectedItem = "Default (From Theme)";
                RoleBodyCombo.SelectedItem = "Default (From Theme)";
                RoleLabelCombo.SelectedItem = "Default (From Theme)";
                RoleNumericCombo.SelectedItem = "Default (From Theme)";
                save(s => s with
                {
                    MatchTypographyToClock = true,
                    MatchAccentToClock = false,
                    LegibilityGuard = true,
                    TypographyPreset = "Classic",
                    TypographySizeScale = 1.0,
                    TypographyWeight = "Regular",
                    TypographyUppercaseLabels = false,
                    TypographyTabularNumerals = true,
                    TypographyTextGlow = false,
                    TypographyGlowStrength = 0.5,
                    FontFamilyDisplay = null,
                    FontFamilyHeading = null,
                    FontFamilyBody = null,
                    FontFamilyLabel = null,
                    FontFamilyNumeric = null
                });
                break;

            case "Tabs":
                TabBarPositionCombo.SelectedItem = "Below clock";
                TabLabelModeCombo.SelectedItem = "Selected tab only";
                HeaderMouseWheelCheck.IsChecked = true;
                TabSwitchOnHoverCheck.IsChecked = false;
                ClipboardEnabledCheck.IsChecked = true;
                ClipboardHistorySlider.Value = 50;
                ClipboardRememberCheck.IsChecked = false;
                ClipboardFilterSecretsCheck.IsChecked = true;
                ClipboardCollapseCheck.IsChecked = true;
                ClipboardAutoExpireCombo.SelectedItem = "24 hours";
                ClipboardClickActionCombo.SelectedItem = "Paste into target app";
                ScreenshotsEnabledCheck.IsChecked = true;
                ScreenshotsClipboardCheck.IsChecked = true;
                ScreenshotsSaveSnipsCheck.IsChecked = false;
                ScreenshotsThumbSizeCombo.SelectedItem = "Medium (240px)";
                ScreenshotsClickActionCombo.SelectedItem = "Copy to clipboard";
                save(s => s with
                {
                    TabBarPosition = "BelowClock",
                    TabLabelMode = "SelectedOnly",
                    HeaderMouseWheelTabSwitch = true,
                    TabSwitchOnHover = false,
                    ClipboardEnabled = true,
                    ClipboardHistorySize = 50,
                    ClipboardRememberAcrossRestarts = false,
                    ClipboardFilterSecrets = true,
                    ClipboardCollapseAfterPaste = true,
                    ClipboardAutoExpireHours = 24,
                    ClipboardClickAction = "Paste",
                    ScreenshotsEnabled = true,
                    ScreenshotsIncludeClipboard = true,
                    ScreenshotsSaveClipboardToFolder = false,
                    ScreenshotsThumbnailSize = 160,
                    ScreenshotsClickAction = "Copy"
                });
                break;

            case "Motion":
                Reduced.IsChecked = false;
                Speed.Value = 1.0;
                Hover.Value = 0;
                Leave.Value = 300;
                save(s => s with
                {
                    ReduceMotion = false,
                    AnimationSpeed = 1.0,
                    HoverDelayMs = 0,
                    CollapseDelayMs = 300
                });
                break;

            case "System":
                CollapsedStat.SelectedItem = "None";
                ProcessSort.SelectedItem = "Memory";
                AlertsEnabledCheck.IsChecked = true;
                AlertVolumeCheck.IsChecked = true;
                AlertBrightnessCheck.IsChecked = true;
                AlertMicCheck.IsChecked = true;
                AlertBatteryCheck.IsChecked = true;
                AlertTempCheck.IsChecked = true;
                AlertSoundCheck.IsChecked = true;
                AlertDurationSlider.Value = 3;
                LayoutDebugCheck.IsChecked = false;
                save(s => s with
                {
                    CollapsedStat = "None",
                    SystemProcessSort = "Memory",
                    AlertsEnabled = true,
                    AlertVolumeOsdEnabled = true,
                    AlertBrightnessOsdEnabled = true,
                    AlertMicOsdEnabled = true,
                    AlertBatteryEnabled = true,
                    AlertTemperatureEnabled = true,
                    AlertSoundEnabled = true,
                    AlertDurationSeconds = 3,
                    LayoutDebug = false
                });
                break;
        }
    }
}
