using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Island.App.ViewModels;
using Island.Core.Motion;
using Island.Core.Settings;

namespace Island.App.Views;

internal sealed class PanelNavigation
{
    private readonly SpringValue pill = new();
    private readonly TranslateTransform indicator;
    private readonly FrameworkElement[] pages = new FrameworkElement[PanelTab.All.Length];
    private readonly Button[] buttons = new Button[PanelTab.All.Length];
    private readonly Func<AppSettings> settings;
    private readonly Action requestFrame;
    private int selected;

    public int SelectedIndex => selected;
    public bool MusicSelected => selected == 0;
    public bool SystemSelected => selected == 1;
    public bool TasksSelected => selected == 2;
    public bool QuickControlsSelected => selected == 3;
    public bool FocusSelected => selected == 4;
    public bool CalendarSelected => selected == 5;
    public event Action? SelectionChanged;

    public PanelNavigation(StackPanel bar, Grid host, TranslateTransform indicator, Style style,
        Func<AppSettings> settings, Action requestFrame,
        MusicPage musicPage, SystemPage systemPage,
        TasksPage tasksPage, QuickControlsPage quickControlsPage, FocusPage focusPage,
        CalendarWeatherPage calendarWeatherPage)
    {
        this.indicator = indicator;
        this.settings = settings;
        this.requestFrame = requestFrame;

        for (int i = 0; i < pages.Length; i++)
        {
            var tab = PanelTab.All[i];
            int index = i;

            var iconBlock = new TextBlock
            {
                Text = tab.Glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var button = new Button
            {
                Content = iconBlock,
                Style = style,
                ToolTip = string.IsNullOrEmpty(tab.Description) ? tab.Name : tab.Description,
                Height = 30,
                Width = 32,
                MinWidth = 32,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                ClipToBounds = true
            };
            AutomationProperties.SetName(button, tab.Name);
            button.Click += (_, _) => Select(index);
            AttachButtonMotion(button, settings);
            bar.Children.Add(button);
            buttons[i] = button;

            FrameworkElement page = i switch
            {
                0 => musicPage,
                1 => systemPage,
                2 => tasksPage,
                3 => quickControlsPage,
                4 => focusPage,
                5 => calendarWeatherPage,
                _ => CreatePage(tab)
            };
            page.RenderTransform = new TranslateTransform();
            page.IsHitTestVisible = i == 0;
            page.Opacity = i == 0 ? 1 : 0;
            pages[i] = page;
            host.Children.Add(page);
        }
        MarkSelected(0, true);
    }

    private static FrameworkElement CreatePage(PanelTab tab)
    {
        var layout = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(34, 0, 34, 0) };
        var icon = new TextBlock
        {
            Text = tab.Glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        icon.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Accent");
        layout.Children.Add(icon);
        var title = new TextBlock
        {
            Text = tab.Name,
            FontSize = 30,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 2)
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Foreground");
        layout.Children.Add(title);
        var description = new TextBlock { Text = tab.Description, FontSize = 14, Opacity = .72 };
        description.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Foreground");
        layout.Children.Add(description);
        return layout;
    }

    private void MarkSelected(int index, bool active)
    {
        if (active)
        {
            buttons[index].SetResourceReference(Control.ForegroundProperty, "Theme.Accent");
        }
        else
        {
            buttons[index].SetResourceReference(Control.ForegroundProperty, "Theme.TextSecondary");
        }
        AutomationProperties.SetItemStatus(buttons[index], active ? "Selected" : "");
    }

    public void Select(int index)
    {
        if (index == selected) return;
        Keyboard.ClearFocus();
        var config = settings();
        MarkSelected(selected, false);
        selected = index;
        MarkSelected(index, true);
        double duration = (config.ReduceMotion ? 100 : 200) / config.AnimationSpeed;
        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            bool incoming = i == selected;
            page.IsHitTestVisible = incoming;
            if (!incoming && page.Opacity == 0 && !page.HasAnimatedProperties) continue;
            Animate(page, UIElement.OpacityProperty, page.Opacity, incoming ? 1 : 0, duration);
            var offset = (TranslateTransform)page.RenderTransform;
            offset.X = 0; // Strictly 0 so text never slides outside borders
            if (!config.ReduceMotion)
            {
                double originY = incoming && page.Opacity == 0 ? 4 : offset.Y;
                Animate(offset, TranslateTransform.YProperty, originY, incoming ? 0 : -4, duration);
            }
            else
            {
                offset.Y = 0;
            }
        }
        requestFrame();
        SelectionChanged?.Invoke();
    }

    public Size GetDesiredSize(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= pages.Length)
            return new Size(680, 400);

        // Standardized stable width across all tabs: zero jumping, zero text overflow
        double islandW = 680;
        double islandH = tabIndex switch
        {
            0 => 380, // Music
            1 => 440, // System
            2 => 400, // Tasks
            3 => 380, // Quick Controls
            4 => 390, // Focus
            5 => 440, // Calendar & Weather
            _ => 400
        };

        // Constraints: max 62% width, max 70% height
        double screenW = SystemParameters.WorkArea.Width > 0 ? SystemParameters.WorkArea.Width : 1920;
        double screenH = SystemParameters.WorkArea.Height > 0 ? SystemParameters.WorkArea.Height : 1080;

        islandW = Math.Clamp(islandW, 520, Math.Max(520, screenW * 0.62));
        islandH = Math.Clamp(islandH, 200, Math.Max(300, screenH * 0.70));

        // Snap to 8 DIP grid
        islandW = Math.Ceiling(islandW / 8.0) * 8.0;
        islandH = Math.Ceiling(islandH / 8.0) * 8.0;

        return new Size(islandW, islandH);
    }

    public bool Advance(double seconds, bool reduced)
    {
        if (reduced) pill.Set(selected);
        bool moving = pill.Advance(selected, seconds);
        indicator.X = pill.Value * 32;
        return moving;
    }

    internal static void AttachButtonMotion(Button button, Func<AppSettings> settings)
    {
        var scale = new ScaleTransform();
        button.RenderTransformOrigin = new Point(.5, .5);
        button.RenderTransform = scale;
        void Update()
        {
            var config = settings();
            double target = config.ReduceMotion ? 1 : button.IsPressed ? .96 : button.IsMouseOver ? 1.04 : 1;
            double duration = 90 / config.AnimationSpeed;
            Animate(scale, ScaleTransform.ScaleXProperty, scale.ScaleX, target, duration);
            Animate(scale, ScaleTransform.ScaleYProperty, scale.ScaleY, target, duration);
        }
        button.MouseEnter += (_, _) => Update();
        button.MouseLeave += (_, _) => Update();
        button.AddHandler(UIElement.MouseDownEvent, new System.Windows.Input.MouseButtonEventHandler((_, _) => Update()), true);
        button.AddHandler(UIElement.MouseUpEvent, new System.Windows.Input.MouseButtonEventHandler((_, _) => Update()), true);
    }

    private static void Animate(IAnimatable target, DependencyProperty property, double from, double to, double ms)
    {
        ((DependencyObject)target).SetValue(property, to);
        target.BeginAnimation(property, new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        });
    }
}
