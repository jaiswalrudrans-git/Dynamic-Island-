using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Island.App.Features.Music.Services;
using Island.App.Features.Music.ViewModels;
using Island.App.Header.Views;
using Island.Core.Settings;

namespace Island.App.Features.Music.Views;

public partial class MusicPage : UserControl
{
    private readonly MusicViewModel model;
    private readonly Action<string?> pin;
    private readonly Action<bool> menuState;
    private bool isDragging;
    private string lastLyric = "";

    internal MusicPage(MusicViewModel model, Action<string?> pin, Action<bool> menuState, Func<AppSettings> settings)
    {
        InitializeComponent();
        this.model = model;
        this.pin = pin;
        this.menuState = menuState;
        DataContext = model;
        Meter.Attach(model.Spectrum);
        foreach (Button button in TransportButtons.Children) PanelNavigation.AttachButtonMotion(button, settings);

        Seek.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SeekPointerDown), true);
        Seek.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(SeekDragStarted), true);
        Seek.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(SeekDragCompleted), true);
        Seek.AddHandler(PreviewMouseMoveEvent, new MouseEventHandler(SeekPointerMove), true);
        Seek.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(SeekPointerUp), true);
        Seek.LostMouseCapture += SeekLostCapture;
        Seek.ValueChanged += SeekValueChanged;

        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MusicViewModel.Title))
            {
                Dispatcher.InvokeAsync(UpdateTitleMarquee, DispatcherPriority.Loaded);
            }
            if (e.PropertyName != nameof(MusicViewModel.CurrentLyric)) return;
            if (lastLyric.Length > 0 && lastLyric == model.PreviousLyric && !model.ReducedMotion && IsHitTestVisible)
                LyricY.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(18, 0,
                    TimeSpan.FromMilliseconds(180 / model.AnimationSpeed)) { FillBehavior = FillBehavior.Stop });
            lastLyric = model.CurrentLyric;
        };
    }

    private void TitleGrid_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateTitleMarquee();

    private void UpdateTitleMarquee()
    {
        if (TrackTitleBlock == null || TitleTransform == null) return;
        TitleTransform.BeginAnimation(TranslateTransform.XProperty, null);
        if (model.ReducedMotion)
        {
            TitleTransform.X = 0;
            return;
        }

        TrackTitleBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        if (TrackTitleBlock.Parent is FrameworkElement parent && parent.ActualWidth > 0)
        {
            double overflow = TrackTitleBlock.DesiredSize.Width - parent.ActualWidth;
            if (overflow > 6)
            {
                var travel = TimeSpan.FromSeconds(Math.Max(3.0, overflow / 22.0));
                var pause = TimeSpan.FromSeconds(1.5);
                var anim = new DoubleAnimationUsingKeyFrames
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };
                anim.KeyFrames.Add(new LinearDoubleKeyFrame(0, pause));
                anim.KeyFrames.Add(new LinearDoubleKeyFrame(-overflow - 4, pause + travel));
                anim.KeyFrames.Add(new LinearDoubleKeyFrame(-overflow - 4, pause + travel + pause));
                TitleTransform.BeginAnimation(TranslateTransform.XProperty, anim);
                return;
            }
        }
        TitleTransform.X = 0;
    }

    private async void Transport(object sender, RoutedEventArgs e) => await model.CommandAsync((string)((Button)sender).Tag);

    private void ChoosePlayer(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = (Button)sender, Placement = PlacementMode.Bottom };
        foreach (var player in model.Players)
        {
            var item = new MenuItem { Header = player.Label, ToolTip = player.Id, IsChecked = player == model.Selected };
            item.Click += (_, _) =>
            {
                if (model.Pinned) pin(player.Id);
                model.Select(player);
            };
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var toggle = new MenuItem { Header = model.Pinned ? "Unpin player" : "Pin this player" };
        toggle.Click += (_, _) => pin(model.Pinned ? null : model.Selected?.Id);
        menu.Items.Add(toggle);
        menu.Closed += (_, _) => menuState(false);
        menuState(true);
        menu.IsOpen = true;
    }

    private double PointerFraction(MouseEventArgs e) =>
        Math.Clamp((e.GetPosition(Seek).X - 6) / Math.Max(1, Seek.ActualWidth - 12), 0, 1);

    private void SeekPointerDown(object sender, MouseButtonEventArgs e)
    {
        if (!model.CanSeek) return;
        isDragging = true;
        model.BeginSeek();
        var sliderTrack = Seek.Template?.FindName("PART_Track", Seek) as Track;
        if (sliderTrack?.Thumb.IsMouseOver != true)
        {
            Seek.CaptureMouse();
            double frac = PointerFraction(e);
            Seek.Value = frac;
            model.PreviewSeek(frac);
        }
    }

    private void SeekDragStarted(object sender, DragStartedEventArgs e)
    {
        if (!model.CanSeek) return;
        isDragging = true;
        model.BeginSeek();
    }

    private void SeekPointerMove(object sender, MouseEventArgs e)
    {
        if (isDragging && Seek.IsMouseCaptured && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            double frac = PointerFraction(e);
            Seek.Value = frac;
            model.PreviewSeek(frac);
        }
    }

    private void SeekValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isDragging)
        {
            model.PreviewSeek(Seek.Value);
        }
    }

    private void SeekDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (!isDragging) return;
        if (e.Canceled)
        {
            isDragging = false;
            model.EndSeekCancel();
            return;
        }
        _ = CommitSeekAsync();
    }

    private void SeekPointerUp(object sender, MouseButtonEventArgs e)
    {
        if (isDragging) _ = CommitSeekAsync();
    }

    private void SeekLostCapture(object sender, MouseEventArgs e)
    {
        if (isDragging && !Seek.IsMouseCaptureWithin)
        {
            _ = CommitSeekAsync();
        }
    }

    private async Task CommitSeekAsync()
    {
        if (!isDragging) return;
        isDragging = false;
        if (Seek.IsMouseCaptured) Seek.ReleaseMouseCapture();
        if (!model.CanSeek) return;
        await model.CommitSeekAsync(Seek.Value);
    }
}
