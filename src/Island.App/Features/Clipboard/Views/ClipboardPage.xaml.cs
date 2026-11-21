using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Island.App.Features.Clipboard.ViewModels;

namespace Island.App.Features.Clipboard.Views;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

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

    // --- Header & Navigation Controls ---
    private void AllTabClick(object sender, RoutedEventArgs e) => model.ShowFavoritesOnly = false;
    private void FavoritesTabClick(object sender, RoutedEventArgs e) => model.ShowFavoritesOnly = true;
    private void ToggleSearchClick(object sender, RoutedEventArgs e) => model.IsSearchOpen = !model.IsSearchOpen;
    private void TogglePauseClick(object sender, RoutedEventArgs e) => model.TogglePause();
    private void ClearAllClick(object sender, RoutedEventArgs e) => model.ClearAll();
    private void ClearFilesClick(object sender, RoutedEventArgs e) => model.ClearDroppedFiles();

    private void SearchGotFocus(object sender, RoutedEventArgs e) => setTypingFocus(true);
    private void SearchLostFocus(object sender, RoutedEventArgs e) => setTypingFocus(false);

    // --- Clipboard Row Actions ---
    private void Row_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && FindParent<Button>(dep) != null) return;

        if (sender is FrameworkElement { DataContext: ClipboardRowViewModel row })
        {
            model.OnRowClicked(row);
        }
    }

    private void RowFavoriteClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ClipboardRowViewModel row })
        {
            model.ToggleFavorite(row);
        }
    }

    private void RowCopyClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ClipboardRowViewModel row })
        {
            model.CopyRow(row);
        }
    }

    private void RowPastePlainClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ClipboardRowViewModel row })
        {
            model.PasteRow(row, plainText: true);
        }
    }

    private void RowDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ClipboardRowViewModel row })
        {
            model.DeleteRow(row);
        }
    }

    private void Row_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ClipboardRowViewModel { IsImage: true } row)
        {
            var preview = row.LargePreview;
            if (preview != null)
            {
                PreviewPopupImage.Source = preview;
                ImagePreviewPopup.PlacementTarget = fe;
                ImagePreviewPopup.IsOpen = true;
            }
        }
    }

    private void Row_MouseLeave(object sender, MouseEventArgs e)
    {
        ImagePreviewPopup.IsOpen = false;
        PreviewPopupImage.Source = null;
    }

    private void Row_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement fe && fe.DataContext is ClipboardRowViewModel row)
        {
            var diff = dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var data = new DataObject();
                bool hasData = false;

                if (row.IsImage)
                {
                    if (!string.IsNullOrEmpty(row.FilePath) && File.Exists(row.FilePath))
                    {
                        var sc = new StringCollection { row.FilePath };
                        data.SetFileDropList(sc);
                        hasData = true;
                    }
                    else if (row.LargePreview is BitmapSource bmp)
                    {
                        data.SetImage(bmp);
                        hasData = true;
                    }
                    else if (row.ClipboardItem?.ImageBytes != null)
                    {
                        try
                        {
                            using var ms = new MemoryStream(row.ClipboardItem.ImageBytes);
                            var img = new BitmapImage();
                            img.BeginInit();
                            img.StreamSource = ms;
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.EndInit();
                            data.SetImage(img);
                            hasData = true;
                        }
                        catch { }
                    }
                }
                else
                {
                    if (row.FilePaths != null && row.FilePaths.Count > 0)
                    {
                        var sc = new StringCollection();
                        sc.AddRange(row.FilePaths.ToArray());
                        data.SetFileDropList(sc);
                        hasData = true;
                    }
                    if (!string.IsNullOrEmpty(row.ClipboardItem?.Text))
                    {
                        data.SetText(row.ClipboardItem.Text);
                        hasData = true;
                    }
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

    // --- File Transfer Shelf ---
    private void DropShelf_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                model.AddDroppedFiles(files);
            }
        }
    }

    private void DropShelf_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void DropShelf_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void FileChip_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement fe && fe.DataContext is DroppedFileItem item)
        {
            var diff = dragStartPoint - e.GetPosition(null);
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var data = new DataObject();
                var sc = new StringCollection { item.FullPath };
                data.SetFileDropList(sc);
                try { DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy); }
                catch { }
            }
        }
        else
        {
            dragStartPoint = e.GetPosition(null);
        }
    }

    private void RemoveFileClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DroppedFileItem item })
        {
            model.RemoveDroppedFile(item);
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;
        while (current != null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
