using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
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

    public PanelNavigation(StackPanel bar, Grid host, TranslateTransform indicator, Style style,
        Func<AppSettings> settings, Action requestFrame)
    {
        this.indicator = indicator;
        this.settings = settings;
        this.requestFrame = requestFrame;
        for (int i = 0; i < pages.Length; i++)
        {
            var tab = PanelTab.All[i];
            int index = i;
            var button = new Button { Content = tab.Glyph, Style = style, ToolTip = tab.Name };
            AutomationProperties.SetName(button, tab.Name);
            button.Click += (_, _) => Select(index);
            AttachButtonMotion(button, settings);
            bar.Children.Add(button);
            buttons[i] = button;

            var page = CreatePage(tab);
            page.Opacity = i == 0 ? 1 : 0;
            pages[i] = page;
            host.Children.Add(page);
        }
        MarkSelected(0, true);
    }

    private static FrameworkElement CreatePage(PanelTab tab)
    {
        var layout = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(34,0,34,0),
            RenderTransform = new TranslateTransform() };
        var icon = new TextBlock { Text = tab.Glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 24, HorizontalAlignment = HorizontalAlignment.Left };
        icon.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Accent");
        layout.Children.Add(icon);
        var title = new TextBlock { Text = tab.Name, FontSize = 30, FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0,12,0,2) };
        title.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Foreground");
        layout.Children.Add(title);
        var description = new TextBlock { Text = tab.Description, FontSize = 14, Opacity = .72 };
        description.SetResourceReference(TextBlock.ForegroundProperty, "Theme.Foreground");
        layout.Children.Add(description);
        return layout;
    }

    private void MarkSelected(int index, bool active)
    {
        if (active) buttons[index].SetResourceReference(Control.ForegroundProperty, "Theme.Accent");
        else buttons[index].ClearValue(Control.ForegroundProperty);
        AutomationProperties.SetItemStatus(buttons[index], active ? "Selected" : "");
    }

    private void Select(int index)
    {
        if (index == selected) return;
        var config = settings();
        int direction = Math.Sign(index - selected);
        MarkSelected(selected, false);
        selected = index;
        MarkSelected(index, true);
        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            bool incoming = i == selected;
            if (!incoming && page.Opacity == 0 && !page.HasAnimatedProperties) continue;
            var offset = (TranslateTransform)page.RenderTransform;
            double origin = incoming && page.Opacity == 0 ? direction * 24 : offset.X;
            double duration = (config.ReduceMotion ? 100 : 220) / config.AnimationSpeed;
            Animate(page, UIElement.OpacityProperty, page.Opacity, incoming ? 1 : 0, duration);
            Animate(offset, TranslateTransform.XProperty, config.ReduceMotion ? 0 : origin,
                config.ReduceMotion || incoming ? 0 : -direction * 24, duration);
        }
        requestFrame();
    }

    public bool Advance(double seconds, bool reduced)
    {
        if (reduced) pill.Set(selected);
        bool moving = pill.Advance(selected, seconds);
        indicator.X = pill.Value * 31;
        return moving;
    }

    internal static void AttachButtonMotion(Button button, Func<AppSettings> settings)
    {
        var scale = new ScaleTransform();
        button.RenderTransformOrigin = new Point(.5,.5);
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
        // Store the terminal value as the base; Stop releases the animation clock after the transition.
        ((DependencyObject)target).SetValue(property, to);
        target.BeginAnimation(property, new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        });
    }
}
