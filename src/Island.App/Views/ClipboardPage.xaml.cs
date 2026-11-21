using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Island.App.ViewModels;
using Island.Core.Clipboard;

namespace Island.App.Views;

public partial class ClipboardPage : UserControl
{
    private readonly ClipboardViewModel model;
    private readonly Action<bool> setTypingFocus;
    private Point dragStartPoint;

    public ClipboardPage(ClipboardViewModel model, Action<bool> setTypingFocus)
    {
        InitializeComponent();
        this.model = model;
        this.setTypingFocus = setTypingFocus;
        DataContext = model;
    }

    private void TogglePauseClick(object sender, RoutedEventArgs e) => model.TogglePause();
    private void ClearAllClick(object sender, RoutedEventArgs e) => model.ClearAll();
    private void SnipClick(object sender, RoutedEventArgs e) => model.TriggerSnip();

    private void SearchGotFocus(object sender, RoutedEventArgs e) => setTypingFocus(true);
    private void SearchLostFocus(object sender, RoutedEventArgs e) => setTypingFocus(false);

    // --- Copies Actions ---
    private void CopyItemPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.TogglePin(id);
    }

    private void CopyItemCopyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.Copy(id);
    }

    private void CopyItemDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.Delete(id);
    }

    private void CopyItemClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && FindParent<Button>(dep) != null) return;

        if (sender is FrameworkElement { DataContext: ClipboardItemViewModel vm })
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

    private void CopyItemMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement fe && fe.DataContext is ClipboardItemViewModel vm)
        {
            var diff = dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var data = new DataObject();
                bool hasData = false;
                if (!string.IsNullOrEmpty(vm.Text))
                {
                    data.SetText(vm.Text);
                    hasData = true;
                }
                if (vm.HasFiles && vm.FilePaths != null)
                {
                    var sc = new System.Collections.Specialized.StringCollection();
                    sc.AddRange(vm.FilePaths.ToArray());
                    data.SetFileDropList(sc);
                    hasData = true;
                }
                if (hasData)
                {
                    try { DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy); }
                    catch { }
                }
            }
        }
        else
        {
            dragStartPoint = e.GetPosition(null);
        }
    }

    // --- Screenshot Actions ---
    private void ScreenshotPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.TogglePinScreenshot(id);
    }

    private void ScreenshotCopyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.CopyScreenshot(id);
    }

    private void ScreenshotOpenClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.OpenScreenshot(id);
    }

    private void ScreenshotDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.DeleteScreenshot(id);
    }

    private void ScreenshotClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && FindParent<Button>(dep) != null) return;

        if (sender is FrameworkElement { DataContext: ScreenshotItemViewModel vm })
        {
            if (e.ClickCount >= 2 || (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                model.CopyScreenshot(vm.Id);
            }
            else
            {
                model.PasteScreenshot(vm.Id);
            }
        }
    }

    private void ScreenshotMouseMove(object sender, MouseEventArgs e)
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
                    try { DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy); }
                    catch { }
                }
            }
        }
        else
        {
            dragStartPoint = e.GetPosition(null);
        }
    }

    private void ScreenshotImageLoaded(object sender, RoutedEventArgs e)
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
