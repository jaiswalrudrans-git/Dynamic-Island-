using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Island.App.Features.Music.ViewModels;
using Island.App.Features.SystemInfo.ViewModels;
using Island.App.ViewModels;
using Island.App.Views;
using Island.Core.Settings;

namespace Island.App.Shell;

public partial class OverlayWindow
{
    private void ApplyPanelSettings()
    {
        TextOptions.SetTextRenderingMode(this,
            (settings.Current.BackgroundTransparency > 0 || settings.Current.CollapsedTransparency > 0)
                ? TextRenderingMode.Grayscale
                : TextRenderingMode.ClearType);
        CompactClock?.ApplySettings(settings.Current, music?.DominantColor);
        HeaderClock?.ApplySettings(settings.Current, music?.DominantColor);
        ReducedClock?.ApplySettings(settings.Current, music?.DominantColor);
        if (HeaderDate != null)
            HeaderDate.Visibility = settings.Current.ClockStyle == "WithDate" ? Visibility.Collapsed : Visibility.Visible;
        string action = Pinned ? "Unpin" : "Pin";
        PinButton.ToolTip = action;
        AutomationProperties.SetName(PinButton, action);
        if (Pinned) PinButton.SetResourceReference(Control.ForegroundProperty, "Theme.Accent");
        else PinButton.ClearValue(Control.ForegroundProperty);
        system?.Configure(settings.Current);
        alerts?.Configure(settings.Current);
        audioOsd?.Configure(settings.Current);
        brightnessOsd?.Configure(settings.Current);
        powerAlerts?.Configure(settings.Current);
        tempAlerts?.Configure(settings.Current);

        bool typographyChanged = lastAppliedClockStyle != settings.Current.ClockStyle ||
                                 lastAppliedPreset != settings.Current.TypographyPreset;
        lastAppliedClockStyle = settings.Current.ClockStyle;
        lastAppliedPreset = settings.Current.TypographyPreset;
        if (typographyChanged)
        {
            if (motion.Expanded) AnimateTypographyTransition();
            UpdateIslandTargetSize();
        }

        ApplyTabBarLayout();
        SyncClipboardHotkey();

        if (previousPin != Pinned)
        {
            previousPin = Pinned;
            if (Pinned && !Paused) Expand(true);
            else if (!Shell.IsMouseOver && !menuOpen && !Paused) Schedule(false);
        }
    }

    internal void TogglePin()
    {
        if (Paused) SetPaused(false);
        Save(s => s with { IsPinned = !s.IsPinned });
    }

    private void PinClick(object sender, RoutedEventArgs e) => TogglePin();
    private void SettingsClick(object sender, RoutedEventArgs e) => OpenSettings();

    internal void OpenSettings()
    {
        if (preferences != null) { preferences.Activate(); return; }
        preferences = new MotionSettingsWindow(settings.Current, edit => Save(edit));
        SyncEscape();
        preferences.Closed += (_, _) => { preferences = null; SyncEscape(); };
        preferences.Show();
    }

    private bool Save(Func<AppSettings, AppSettings> edit)
    {
        try { settings.Update(edit); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            log($"Settings update failed: {ex.Message}");
            MessageBox.Show("Could not save this change. " + ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }
}
