using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Island.App.Features.Controls.Views;

public class QuickSlider : Control
{
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(QuickSlider),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnRangeChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(QuickSlider),
            new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender, OnRangeChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(QuickSlider),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    private static readonly DependencyProperty VisualFractionProperty =
        DependencyProperty.Register(nameof(VisualFraction), typeof(double), typeof(QuickSlider),
            new PropertyMetadata(0.0, OnVisualFractionChanged));

    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    private double VisualFraction { get => (double)GetValue(VisualFractionProperty); set => SetValue(VisualFractionProperty, value); }

    public event EventHandler? DragStarted;
    public event EventHandler? DragCompleted;

    private readonly Grid rootGrid = new();
    private readonly Border focusRing;
    private readonly Border track;
    private readonly Border fill;
    private readonly ScaleTransform fillScale = new(0, 1);
    private readonly Border thumbHitArea;
    private readonly Grid visualThumb;
    private readonly ScaleTransform thumbScale = new(1, 1);
    private readonly TranslateTransform thumbTranslate = new(0, 0);

    private bool isDragging;
    private bool isUpdatingInternally;

    private long lastMoveDurationTicks;
    public double LastMoveDurationMs => (double)lastMoveDurationTicks / Stopwatch.Frequency * 1000.0;

    public QuickSlider()
    {
        Focusable = true;
        Height = 28;
        MinHeight = 28;
        Background = Brushes.Transparent;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;

        track = new Border
        {
            Height = 6, CornerRadius = new CornerRadius(3),
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch,
            IsHitTestVisible = false
        };
        track.SetResourceReference(Border.BackgroundProperty, "Theme.BorderStrong");

        fill = new Border
        {
            Height = 6, CornerRadius = new CornerRadius(3),
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch,
            RenderTransformOrigin = new Point(0, 0.5), RenderTransform = fillScale,
            IsHitTestVisible = false
        };
        fill.SetResourceReference(Border.BackgroundProperty, "Theme.Data");

        visualThumb = new Grid
        {
            Width = 16, Height = 16,
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
            RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = thumbScale,
            IsHitTestVisible = false
        };
        var outerEllipse = new Ellipse { Width = 16, Height = 16 };
        outerEllipse.SetResourceReference(Shape.FillProperty, "Theme.Data");
        visualThumb.Children.Add(outerEllipse);
        visualThumb.Children.Add(new Ellipse { Width = 10, Height = 10, Fill = Brushes.White });

        thumbHitArea = new Border
        {
            Width = 28, Height = 28, Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(-14, 0, 0, 0), RenderTransform = thumbTranslate, Cursor = Cursors.Hand,
            Child = visualThumb
        };

        focusRing = new Border
        {
            Height = 26, CornerRadius = new CornerRadius(5), BorderThickness = new Thickness(1.5),
            IsHitTestVisible = false, Opacity = 0.0,
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(-2, 0, -2, 0)
        };
        focusRing.SetResourceReference(Border.BorderBrushProperty, "Theme.Accent");

        rootGrid.Children.Add(focusRing);
        rootGrid.Children.Add(track);
        rootGrid.Children.Add(fill);
        rootGrid.Children.Add(thumbHitArea);

        AddVisualChild(rootGrid);
        AddLogicalChild(rootGrid);
        Loaded += (_, _) => UpdateVisualPositions(CalculateFraction(Value));
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => rootGrid;

    protected override Size MeasureOverride(Size constraint)
    {
        rootGrid.Measure(constraint);
        return new Size(double.IsPositiveInfinity(constraint.Width) ? 100 : constraint.Width, 28);
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        rootGrid.Arrange(new Rect(arrangeBounds));
        UpdateVisualPositions(VisualFraction);
        return arrangeBounds;
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QuickSlider s) s.UpdateVisualPositions(s.CalculateFraction(s.Value));
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QuickSlider s) s.HandleValueChanged((double)e.NewValue);
    }

    private static void OnVisualFractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QuickSlider s) s.UpdateVisualPositions((double)e.NewValue);
    }

    private double CalculateFraction(double val)
    {
        double range = Maximum - Minimum;
        return range <= 0 ? 0 : Math.Clamp((val - Minimum) / range, 0.0, 1.0);
    }

    private void HandleValueChanged(double newValue)
    {
        if (isUpdatingInternally) return;
        double fraction = CalculateFraction(newValue);
        if (isDragging)
        {
            VisualFraction = fraction;
        }
        else
        {
            var anim = new DoubleAnimation(fraction, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(VisualFractionProperty, anim);
        }
    }

    private void UpdateVisualPositions(double fraction)
    {
        fraction = Math.Clamp(fraction, 0.0, 1.0);
        fillScale.ScaleX = fraction;
        double usable = Math.Max(0.0, ActualWidth - 16.0);
        thumbTranslate.X = 8.0 + fraction * usable;
    }

    private void UpdateValueFromPoint(Point p)
    {
        long start = Stopwatch.GetTimestamp();
        double usable = Math.Max(1.0, ActualWidth - 16.0);
        double fraction = Math.Clamp((p.X - 8.0) / usable, 0.0, 1.0);
        double newValue = Minimum + fraction * (Maximum - Minimum);

        isUpdatingInternally = true;
        try { Value = newValue; } finally { isUpdatingInternally = false; }
        VisualFraction = fraction;
        lastMoveDurationTicks = Stopwatch.GetTimestamp() - start;
    }

    private void UpdateThumbScale()
    {
        double target = isDragging ? 1.25 : (IsMouseOver ? 1.1 : 1.0);
        var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(80))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        thumbScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        thumbScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    protected override void OnMouseEnter(MouseEventArgs e) { base.OnMouseEnter(e); UpdateThumbScale(); }
    protected override void OnMouseLeave(MouseEventArgs e) { base.OnMouseLeave(e); UpdateThumbScale(); }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton == MouseButton.Left)
        {
            Focus();
            CaptureMouse();
            isDragging = true;
            BeginAnimation(VisualFractionProperty, null);
            UpdateValueFromPoint(e.GetPosition(this));
            UpdateThumbScale();
            DragStarted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateValueFromPoint(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (isDragging && e.ChangedButton == MouseButton.Left)
        {
            isDragging = false;
            ReleaseMouseCapture();
            UpdateThumbScale();
            DragCompleted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (isDragging)
        {
            isDragging = false;
            UpdateThumbScale();
            DragCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnTouchDown(TouchEventArgs e)
    {
        base.OnTouchDown(e);
        Focus();
        CaptureTouch(e.TouchDevice);
        isDragging = true;
        BeginAnimation(VisualFractionProperty, null);
        UpdateValueFromPoint(e.GetTouchPoint(this).Position);
        UpdateThumbScale();
        DragStarted?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnTouchMove(TouchEventArgs e)
    {
        base.OnTouchMove(e);
        if (isDragging) { UpdateValueFromPoint(e.GetTouchPoint(this).Position); e.Handled = true; }
    }

    protected override void OnTouchUp(TouchEventArgs e)
    {
        base.OnTouchUp(e);
        if (isDragging)
        {
            isDragging = false;
            ReleaseTouchCapture(e.TouchDevice);
            UpdateThumbScale();
            DragCompleted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnStylusDown(StylusDownEventArgs e)
    {
        base.OnStylusDown(e);
        Focus();
        CaptureStylus();
        isDragging = true;
        BeginAnimation(VisualFractionProperty, null);
        UpdateValueFromPoint(e.GetPosition(this));
        UpdateThumbScale();
        DragStarted?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnStylusMove(StylusEventArgs e)
    {
        base.OnStylusMove(e);
        if (isDragging) { UpdateValueFromPoint(e.GetPosition(this)); e.Handled = true; }
    }

    protected override void OnStylusUp(StylusEventArgs e)
    {
        base.OnStylusUp(e);
        if (isDragging)
        {
            isDragging = false;
            ReleaseStylusCapture();
            UpdateThumbScale();
            DragCompleted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        double range = Maximum - Minimum;
        double step = range * (isShift ? 0.10 : 0.02);
        double target = Math.Clamp(e.Delta > 0 ? Value + step : Value - step, Minimum, Maximum);

        isUpdatingInternally = true;
        try { Value = target; } finally { isUpdatingInternally = false; }
        VisualFraction = CalculateFraction(target);
        DragCompleted?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        double range = Maximum - Minimum;
        double? next = e.Key switch
        {
            Key.Left or Key.Down => Value - range * 0.02,
            Key.Right or Key.Up => Value + range * 0.02,
            Key.PageDown => Value - range * 0.10,
            Key.PageUp => Value + range * 0.10,
            Key.Home => Minimum,
            Key.End => Maximum,
            _ => null
        };

        if (next.HasValue)
        {
            double target = Math.Clamp(next.Value, Minimum, Maximum);
            isUpdatingInternally = true;
            try { Value = target; } finally { isUpdatingInternally = false; }
            VisualFraction = CalculateFraction(target);
            DragCompleted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) { base.OnGotKeyboardFocus(e); focusRing.Opacity = 1.0; }
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) { base.OnLostKeyboardFocus(e); focusRing.Opacity = 0.0; }
}
