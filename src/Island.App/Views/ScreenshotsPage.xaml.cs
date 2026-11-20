using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Island.App.ViewModels;

namespace Island.App.Views;

public partial class ScreenshotsPage : UserControl
{
    private readonly ScreenshotsViewModel model;
    private Point dragStartPoint;

    public ScreenshotsPage(ScreenshotsViewModel model)
    {
        InitializeComponent();
        this.model = model;
        DataContext = model;
    }

    private void SnipClick(object sender, RoutedEventArgs e) => model.Snip();

    private void ThumbImageLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Image img && img.DataContext is ScreenshotItemViewModel vm)
        {
            if (img.Source != null) return;

            try
            {
                if (!string.IsNullOrEmpty(vm.FilePath) && File.Exists(vm.FilePath))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(vm.FilePath);
                    bmp.DecodePixelWidth = 240; // Downsample for memory efficiency
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    img.Source = bmp;
                }
                else if (vm.RawBytes != null && vm.RawBytes.Length > 0)
                {
                    using var ms = new MemoryStream(vm.RawBytes);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = ms;
                    bmp.DecodePixelWidth = 240;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    img.Source = bmp;
                }
            }
            catch { }
        }
    }

    private void ThumbnailClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && FindParent<Button>(dep) != null)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: ScreenshotItemViewModel vm })
        {
            if (e.ClickCount >= 2 || (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                model.Copy(vm.Id);
            }
            else
            {
                model.Paste(vm.Id);
            }
        }
    }

    private void ThumbnailMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement fe && fe.DataContext is ScreenshotItemViewModel vm)
        {
            var diff = dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var data = new DataObject();
                bool hasData = false;

                if (!string.IsNullOrEmpty(vm.FilePath) && File.Exists(vm.FilePath))
                {
                    var fileDrop = new System.Collections.Specialized.StringCollection { vm.FilePath };
                    data.SetFileDropList(fileDrop);
                    hasData = true;
                }
                else if (vm.RawBytes != null && vm.RawBytes.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(vm.RawBytes);
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = ms;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        data.SetImage(bmp);
                        hasData = true;
                    }
                    catch { }
                }

                if (hasData)
                {
                    try
                    {
                        DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy);
                    }
                    catch { }
                }
            }
        }
        else
        {
            dragStartPoint = e.GetPosition(null);
        }
    }

    private void PinClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.TogglePin(id);
    }

    private void CopyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.Copy(id);
    }

    private void OpenClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.Open(id);
    }

    private void DeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.Delete(id);
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typed) return typed;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
