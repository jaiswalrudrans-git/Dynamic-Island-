using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Island.App.ViewModels;
using Island.Core.Motion;
using Island.Core.Settings;

namespace Island.App.Views;

internal sealed class PanelNavigation
{
    private readonly SpringValue pillX = new();
    private readonly SpringValue pillW = new();
    private readonly Border? pillBorder;
    private readonly TranslateTransform indicator;
    private readonly FrameworkElement[] pages = new FrameworkElement[PanelTab.All.Length];
    private readonly Button[] buttons = new Button[PanelTab.All.Length];
    private readonly TextBlock[] iconBlocks = new TextBlock[PanelTab.All.Length];
    private readonly StackPanel bar;
    private readonly Func<AppSettings> settings;
    private readonly Action requestFrame;
    private readonly DispatcherTimer hoverTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private int selected;
    private int hoverCandidate = -1;
    private double targetPillX;
    private double targetPillW = 32;

    public int SelectedIndex => selected;
    public bool MusicSelected => selected == 0;
    public bool SystemSelected => selected == 1;
    public bool TasksSelected => selected == 2;
    public bool QuickControlsSelected => selected == 3;
    public bool FocusSelected => selected == 4;
    public bool CalendarSelected => selected == 5;
    public bool ClipboardSelected => selected == 6;
    public event Action? SelectionChanged;

    public PanelNavigation(StackPanel bar, Grid host, Border? pillBorder, TranslateTransform indicator, Style style,
        Func<AppSettings> settings, Action requestFrame,
        MusicPage musicPage, SystemPage systemPage,
        TasksPage tasksPage, QuickControlsPage quickControlsPage, FocusPage focusPage,
        CalendarWeatherPage calendarWeatherPage,
        ClipboardPage clipboardPage)
    {
        this.bar = bar;
        this.pillBorder = pillBorder;
        this.indicator = indicator;
        this.settings = settings;
        this.requestFrame = requestFrame;

        hoverTimer.Tick += (_, _) =>
        {
            hoverTimer.Stop();
            if (hoverCandidate >= 0 && hoverCandidate < pages.Length && hoverCandidate != selected)
            {
                Select(hoverCandidate);
            }
        };

        for (int i = 0; i < pages.Length; i++)
        {
            var tab = PanelTab.All[i];
            int index = i;

            var iconBlock = new TextBlock
            {
                Text = tab.Glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            iconBlock.SetResourceReference(TextBlock.ForegroundProperty, "Theme.TextSecondary");
            iconBlocks[i] = iconBlock;

            var button = new Button
            {
                Content = iconBlock,
                Style = style,
                Width = 32,
                Height = 28,
                Padding = new Thickness(0),
                Margin = new Thickness(2, 0, 2, 0),
                ClipToBounds = true
            };
            ToolTipService.SetInitialShowDelay(button, 400);
            ToolTipService.SetBetweenShowDelay(button, 100);
            ToolTipService.SetToolTip(button, string.IsNullOrEmpty(tab.Description) ? tab.Name : tab.Description);
            AutomationProperties.SetName(button, tab.Name);
            button.Click += (_, _) => Select(index);

            button.MouseEnter += (_, _) =>
            {
                if (settings().TabSwitchOnHover && index != selected)
                {
                    hoverCandidate = index;
                    hoverTimer.Stop();
                    hoverTimer.Start();
                }
            };
            button.MouseLeave += (_, _) =>
            {
                if (hoverCandidate == index)
                {
                    hoverTimer.Stop();
                    hoverCandidate = -1;
                }
            };

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
                6 => clipboardPage,
                _ => CreatePage(tab)
            };
            page.RenderTransform = new TranslateTransform();
            page.IsHitTestVisible = i == 0;
            page.Opacity = i == 0 ? 1 : 0;
            pages[i] = page;
            host.Children.Add(page);
        }

        UpdateLabels();
        MarkSelected(0, true);
        UpdatePillTarget(instant: true);
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

    private void UpdateLabels()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Width = 32;
            buttons[i].Height = 28;
            buttons[i].Padding = new Thickness(0);
        }
    }

    private void MarkSelected(int index, bool active)
    {
        if (active)
        {
            buttons[index].SetResourceReference(Control.ForegroundProperty, "Theme.Accent");
            iconBlocks[index].SetResourceReference(TextBlock.ForegroundProperty, "Theme.Accent");
        }
        else
        {
            buttons[index].SetResourceReference(Control.ForegroundProperty, "Theme.TextSecondary");
            iconBlocks[index].SetResourceReference(TextBlock.ForegroundProperty, "Theme.TextSecondary");
        }
        AutomationProperties.SetItemStatus(buttons[index], active ? "Selected" : "");
    }

    private void UpdatePillTarget(bool instant = false)
    {
        if (selected < 0 || selected >= buttons.Length) return;

        bar.UpdateLayout();
        var btn = buttons[selected];
        btn.UpdateLayout();

        double x = 0;
        try
        {
            var transform = btn.TransformToAncestor(bar);
            var pt = transform.Transform(new Point(0, 0));
            x = pt.X;
        }
        catch
        {
            for (int i = 0; i < selected; i++)
            {
                x += buttons[i].ActualWidth + 4;
            }
        }

        targetPillX = x;
        targetPillW = 32;

        if (instant)
        {
            pillX.Set(targetPillX);
            pillW.Set(targetPillW);
            indicator.X = targetPillX;
            if (pillBorder != null) pillBorder.Width = targetPillW;
        }
    }

    public void SelectNext()
    {
        int next = (selected + 1) % pages.Length;
        Select(next);
    }

    public void SelectPrevious()
    {
        int prev = (selected - 1 + pages.Length) % pages.Length;
        Select(prev);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= pages.Length || index == selected) return;
        Keyboard.ClearFocus();
        var config = settings();
        int oldIndex = selected;
        bool movingRight = index > oldIndex;

        MarkSelected(selected, false);
        selected = index;
        UpdateLabels();
        MarkSelected(index, true);
        UpdatePillTarget(instant: config.ReduceMotion);

        double duration = (config.ReduceMotion ? 100 : 200) / config.AnimationSpeed;
        double slideOffset = 24.0;

        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            bool incoming = i == selected;
            bool outgoing = i == oldIndex;
            page.IsHitTestVisible = incoming;

            var offset = (TranslateTransform)page.RenderTransform;

            if (incoming)
            {
                Animate(page, UIElement.OpacityProperty, page.Opacity, 1.0, duration);
                if (!config.ReduceMotion)
                {
                    double startX = movingRight ? slideOffset : -slideOffset;
                    offset.X = startX;
                    Animate(offset, TranslateTransform.XProperty, startX, 0, duration);
                }
                else
                {
                    offset.X = 0;
                }
            }
            else if (outgoing)
            {
                Animate(page, UIElement.OpacityProperty, page.Opacity, 0.0, duration);
                if (!config.ReduceMotion)
                {
                    double targetX = movingRight ? -slideOffset : slideOffset;
                    Animate(offset, TranslateTransform.XProperty, offset.X, targetX, duration);
                }
                else
                {
                    offset.X = 0;
                }
            }
            else
            {
                if (page.Opacity > 0)
                    Animate(page, UIElement.OpacityProperty, page.Opacity, 0.0, duration);
                offset.X = 0;
            }
        }

        requestFrame();
        SelectionChanged?.Invoke();
    }

    public Size GetDesiredSize(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= pages.Length)
            return new Size(580, 240);

        var page = pages[tabIndex];

        double screenW = SystemParameters.WorkArea.Width > 0 ? SystemParameters.WorkArea.Width : 1920;
        double screenH = SystemParameters.WorkArea.Height > 0 ? SystemParameters.WorkArea.Height : 1080;
        double minIslandW = 580.0;
        double maxIslandW = Math.Max(minIslandW, Math.Floor(screenW * 0.62 / 8.0) * 8.0);
        double maxIslandH = Math.Max(180, Math.Floor(screenH * 0.70 / 8.0) * 8.0);

        // Pass 1: Measure unconstrained to determine content's natural desired width
        page.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double desiredContentW = page.DesiredSize.Width;

        // Comfortable baseline widths per tab
        desiredContentW = tabIndex switch
        {
            0 => Math.Max(desiredContentW, 580), // Music
            1 => Math.Max(desiredContentW, 680), // System
            2 => Math.Max(desiredContentW, 580), // Tasks
            3 => Math.Max(desiredContentW, 620), // Quick Controls
            4 => Math.Max(desiredContentW, 580), // Focus
            5 => Math.Max(desiredContentW, 680), // Calendar & Weather
            6 => Math.Max(desiredContentW, 680), // Clipboard & Screenshots dual-half
            _ => Math.Max(desiredContentW, 580)
        };

        // Horizontal padding: 16 left + 16 right = 32 DIP (Single Root Padding Owner)
        double targetContentW = Math.Clamp(desiredContentW, minIslandW - 32.0, maxIslandW - 32.0);
        double targetIslandW = Math.Ceiling((targetContentW + 32.0) / 8.0) * 8.0;
        targetIslandW = Math.Clamp(targetIslandW, minIslandW, maxIslandW);
        targetContentW = targetIslandW - 32.0;

        // Pass 2: Measure with the target content width so text wrapping & reflow compute exact height
        page.Measure(new Size(targetContentW, double.PositiveInfinity));
        double desiredContentH = page.DesiredSize.Height;

        // Ensure comfortable, un-cramped height floors per tab matching flat layout
        desiredContentH = tabIndex switch
        {
            0 => Math.Max(desiredContentH, 90),  // Music (empty 90, active 170)
            1 => Math.Max(desiredContentH, 264), // System (clean 3x2 Bento Grid)
            2 => Math.Max(desiredContentH, 75),  // Tasks (empty 75, populated 140-200)
            3 => Math.Max(desiredContentH, 230), // Quick Controls (sliders + 8 tiles)
            4 => Math.Max(desiredContentH, 160), // Focus (stopwatch 160, pomodoro ~262, timer ~298)
            5 => Math.Max(desiredContentH, 250), // Calendar & Weather (month + weather)
            6 => Math.Max(desiredContentH, 140), // Clipboard & Screenshots (empty 140, dual-half 250)
            _ => Math.Max(desiredContentH, 120)
        };

        // Vertical overhead:
        // When TabBarPosition is "BelowClock": 14 top + 80 header + 8 gap + 14 bottom = 116 DIP.
        // When TabBarPosition is "LeftOfClock": 14 top + 44 header + 8 gap + 14 bottom = 80 DIP.
        bool belowClock = !string.Equals(settings().TabBarPosition, "LeftOfClock", StringComparison.OrdinalIgnoreCase);
        double overhead = belowClock ? 116.0 : 80.0;

        double targetIslandH = Math.Ceiling((desiredContentH + overhead) / 8.0) * 8.0;
        targetIslandH = Math.Clamp(targetIslandH, 120.0, maxIslandH);

        return new Size(targetIslandW, targetIslandH);
    }

    public bool Advance(double seconds, bool reduced)
    {
        if (reduced)
        {
            pillX.Set(targetPillX);
            pillW.Set(targetPillW);
        }
        bool movingX = pillX.Advance(targetPillX, seconds);
        bool movingW = pillW.Advance(targetPillW, seconds);
        indicator.X = pillX.Value;
        if (pillBorder != null && pillW.Value > 0)
        {
            pillBorder.Width = pillW.Value;
        }
        return movingX || movingW;
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
        button.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler((_, _) => Update()), true);
        button.AddHandler(UIElement.MouseUpEvent, new MouseButtonEventHandler((_, _) => Update()), true);
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
