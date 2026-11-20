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

    private void ItemPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.TogglePin(id);
    }

    private void ItemCopyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.Copy(id);
    }

    private void ItemDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.Delete(id);
    }

    private void ItemCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && FindParent<Button>(dep) != null)
        {
            return; // Clicked an action button
        }

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

    private void ItemCardMouseMove(object sender, MouseEventArgs e)
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
                    data.SetData(DataFormats.UnicodeText, vm.Text);
                    hasData = true;
                }

                if (vm.FilePaths != null && vm.FilePaths.Count > 0)
                {
                    var fileDrop = new System.Collections.Specialized.StringCollection();
                    fileDrop.AddRange(vm.FilePaths.ToArray());
                    data.SetFileDropList(fileDrop);
                    hasData = true;
                }

                if (vm.ImageBytes != null && vm.ImageBytes.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(vm.ImageBytes);
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

    private void SearchGotFocus(object sender, RoutedEventArgs e) => setTypingFocus(true);
    private void SearchLostFocus(object sender, RoutedEventArgs e) => setTypingFocus(false);

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
