using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Island.Core;
using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow : Window
{
    private readonly Action<Func<AppSettings, AppSettings>> save;
    private string activePage = "Appearance";

    public MotionSettingsWindow(AppSettings current, Action<Func<AppSettings, AppSettings>> save)
    {
        this.save = save;
        InitializeComponent();
        Title = AppIdentity.Name + " settings";
        UpdateSidebarSelection();

        InitializeAppearance(current);
        InitializeClock(current);
        InitializeTypography(current);
        InitializeTabsClipboard(current);
        InitializeMotion(current);
        InitializeSystemAlerts(current);
    }

    private void NavClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
        {
            SwitchPage(tag);
        }
    }

    private void SwitchPage(string page)
    {
        activePage = page;
        PageAppearance.Visibility = page == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        PageClock.Visibility = page == "Clock" ? Visibility.Visible : Visibility.Collapsed;
        PageTypography.Visibility = page == "Typography" ? Visibility.Visible : Visibility.Collapsed;
        PageTabsClipboard.Visibility = page == "Tabs" ? Visibility.Visible : Visibility.Collapsed;
        PageMotion.Visibility = page == "Motion" ? Visibility.Visible : Visibility.Collapsed;
        PageSystemAlerts.Visibility = page == "System" ? Visibility.Visible : Visibility.Collapsed;
        PageAbout.Visibility = page == "About" ? Visibility.Visible : Visibility.Collapsed;

        PageHeaderTitle.Text = page switch
        {
            "Appearance" => "Appearance",
            "Clock" => "Clock Studio",
            "Typography" => "Typography Themes",
            "Tabs" => "Tabs & Clipboard",
            "Motion" => "Motion & Timing",
            "System" => "System & Alerts",
            "About" => "About Island",
            _ => "Settings"
        };

        UpdateSidebarSelection();
    }

    private void UpdateSidebarSelection()
    {
        void Highlight(Button btn, bool active)
        {
            if (btn == null) return;
            if (active)
            {
                btn.SetResourceReference(Control.BackgroundProperty, "Theme.AccentSoft");
                btn.SetResourceReference(Control.ForegroundProperty, "Theme.Accent");
            }
            else
            {
                btn.Background = Brushes.Transparent;
                btn.SetResourceReference(Control.ForegroundProperty, "Theme.TextSecondary");
            }
        }

        Highlight(NavAppearanceBtn, activePage == "Appearance");
        Highlight(NavClockBtn, activePage == "Clock");
        Highlight(NavTypographyBtn, activePage == "Typography");
        Highlight(NavTabsBtn, activePage == "Tabs");
        Highlight(NavMotionBtn, activePage == "Motion");
        Highlight(NavSystemBtn, activePage == "System");
        Highlight(NavAboutBtn, activePage == "About");
    }

    private void Bind(Slider slider, Func<AppSettings, AppSettings> edit)
    {
        slider.ValueChanged += (_, _) => save(edit);
    }
}
