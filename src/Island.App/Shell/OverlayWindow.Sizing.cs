using System.Windows;
using Island.App.Design;

namespace Island.App.Shell;

public partial class OverlayWindow
{
    private void UpdateIslandTargetSize()
    {
        var size = navigation.GetDesiredSize(navigation.SelectedIndex);
        targetIslandWidth = size.Width;
        targetIslandHeight = size.Height;
        bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        double overhead = belowClock ? 116.0 : 80.0;
        double contentW = Math.Max(580 - IslandMetrics.HorizontalInset, Math.Round(size.Width - IslandMetrics.HorizontalInset));
        double contentH = Math.Max(60, Math.Round(size.Height - overhead));
        if (Header.Width != contentW) Header.Width = contentW;
        if (Body.Width != contentW) Body.Width = contentW;
        if (Body.Height != contentH) Body.Height = contentH;
        RequestFrame();
    }

    private void ApplyTabBarLayout()
    {
        if (HeaderRow0 == null || HeaderRow1 == null || Body == null) return;
        bool belowClock = !string.Equals(settings.Current.TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        if (belowClock)
        {
            HeaderRow0.Height = new GridLength(44);
            HeaderRow1.Height = new GridLength(36);
            Header.Height = 80;
            if (HeaderLeftCol != null) HeaderLeftCol.Width = new GridLength(72);
            if (HeaderRightCol != null) HeaderRightCol.Width = new GridLength(72);
            Body.Margin = new Thickness(0, 102, 0, 0);
            if (LegacyTabContainer != null) LegacyTabContainer.Visibility = Visibility.Collapsed;
            if (TabBarRow != null) TabBarRow.Visibility = Visibility.Visible;
            if (TabBarRow != null && TabBarContainer != null && TabBarRow.Children.Count == 0 && LegacyTabContainer?.Child != null)
            {
                var el = LegacyTabContainer.Child;
                LegacyTabContainer.Child = null;
                TabBarRow.Children.Add(el);
            }
        }
        else
        {
            HeaderRow0.Height = new GridLength(44);
            HeaderRow1.Height = new GridLength(0);
            Header.Height = 44;
            if (HeaderLeftCol != null) HeaderLeftCol.Width = GridLength.Auto;
            if (HeaderRightCol != null) HeaderRightCol.Width = new GridLength(72);
            Body.Margin = new Thickness(0, 66, 0, 0);
            if (LegacyTabContainer != null) LegacyTabContainer.Visibility = Visibility.Visible;
            if (TabBarRow != null) TabBarRow.Visibility = Visibility.Collapsed;
            if (LegacyTabContainer != null && LegacyTabContainer.Child == null && TabBarRow != null && TabBarRow.Children.Count > 0)
            {
                var el = TabBarRow.Children[0];
                TabBarRow.Children.Clear();
                LegacyTabContainer.Child = el;
            }
        }
    }
}
