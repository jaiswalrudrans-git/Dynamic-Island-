using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Island.App.Services;
using Island.App.ViewModels;
using Island.Core.Settings;

namespace Island.App.Views;

public partial class MusicPage : UserControl
{
    private readonly MusicViewModel model;
    private readonly Action<string?> pin;
    private readonly Action<bool> menuState;
    private PlayerChoice? seekingPlayer;
    private string? seekingTitle;
    private BindingBase? seekBinding;
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
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(MusicViewModel.CurrentLyric)) return;
            if (lastLyric.Length > 0 && lastLyric == model.PreviousLyric && !model.ReducedMotion && IsHitTestVisible)
                LyricY.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(18, 0,
                    TimeSpan.FromMilliseconds(180 / model.AnimationSpeed)) { FillBehavior = FillBehavior.Stop });
            lastLyric = model.CurrentLyric;
        };
    }

    private async void Transport(object sender, RoutedEventArgs e) => await model.CommandAsync((string)((Button)sender).Tag);

    private void ChoosePlayer(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = (Button)sender, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
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
        Math.Clamp((e.GetPosition(Seek).X - 5) / Math.Max(1, Seek.ActualWidth - 10), 0, 1);

    private void SeekStart(object sender, MouseButtonEventArgs e)
    {
        if (!model.CanSeek || !Seek.CaptureMouse()) return;
        seekingPlayer = model.Selected;
        seekingTitle = model.Title;
        seekBinding = BindingOperations.GetBindingBase(Seek, Slider.ValueProperty);
        BindingOperations.ClearBinding(Seek, Slider.ValueProperty);
        Seek.SetCurrentValue(Slider.ValueProperty, PointerFraction(e));
        e.Handled = true;
    }

    private void SeekMove(object sender, MouseEventArgs e)
    {
        if (seekingPlayer != null) Seek.SetCurrentValue(Slider.ValueProperty, PointerFraction(e));
    }

    private async void SeekEnd(object sender, MouseButtonEventArgs e)
    {
        if (seekingPlayer == null) return;
        bool valid = model.Selected == seekingPlayer && model.Title == seekingTitle && model.CanSeek;
        double value = PointerFraction(e);
        Seek.ReleaseMouseCapture();
        e.Handled = true;
        if (valid) await model.CommandAsync("Seek", value);
    }

    private void SeekLost(object sender, MouseEventArgs e)
    {
        seekingPlayer = null;
        if (seekBinding != null) Seek.SetBinding(Slider.ValueProperty, seekBinding);
        seekBinding = null;
    }
}
