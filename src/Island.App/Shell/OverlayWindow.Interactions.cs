using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Island.App.Features.Clipboard.Services;
using Island.App.Header.ViewModels;
using Island.App.ViewModels;
using Island.Core.Clipboard;
using Island.Core.Screenshots;

namespace Island.App.Shell;

public partial class OverlayWindow
{
    private void SyncClipboardHotkey()
    {
        if (source is null) return;
        bool enabled = settings.Current.ClipboardEnabled;
        if (enabled && !clipboardHotkeyRegistered)
        {
            // Ctrl + Alt + V (0x0001 | 0x0002 | 0x4000 = 0x4003, Key 0x56)
            clipboardHotkeyRegistered = RegisterHotKey(source.Handle, ClipboardHotkeyId, 0x4003, 0x56);
            if (!clipboardHotkeyRegistered) log("Clipboard hotkey (Ctrl+Alt+V) could not be registered.");
        }
        else if (!enabled && clipboardHotkeyRegistered)
        {
            UnregisterHotKey(source.Handle, ClipboardHotkeyId);
            clipboardHotkeyRegistered = false;
        }
    }

    private void UpdateExternalForegroundWindow()
    {
        if (source == null) return;
        var fg = ClipboardNative.GetForegroundWindow();
        if (fg != IntPtr.Zero && fg != source.Handle)
        {
            lastExternalForegroundHwnd = fg;
        }
    }

    private void Header_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!settings.Current.HeaderMouseWheelTabSwitch) return;
        if (e.Delta < 0) navigation.SelectNext();
        else if (e.Delta > 0) navigation.SelectPrevious();
        e.Handled = true;
    }

    private void OnOverlayPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!motion.Expanded) return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            int tabIndex = -1;
            if (e.Key >= Key.D1 && e.Key <= Key.D8)
            {
                tabIndex = (int)e.Key - (int)Key.D1;
            }
            else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad8)
            {
                tabIndex = (int)e.Key - (int)Key.NumPad1;
            }

            if (tabIndex >= 0 && tabIndex < PanelTab.All.Length)
            {
                navigation.Select(tabIndex);
                e.Handled = true;
            }
        }
    }

    internal void OnClipboardUpdate()
    {
        if (suppressNextClipboardUpdate)
        {
            suppressNextClipboardUpdate = false;
            return;
        }

        if (!settings.Current.ClipboardEnabled || clipboardStore.IsPaused)
            return;

        try
        {
            if (ClipboardNative.HasExcludedClipboardFormat())
                return;

            var fg = ClipboardNative.GetForegroundWindow();
            string? appName = ClipboardNative.GetProcessNameForWindow(fg);

            if (!string.IsNullOrEmpty(appName) &&
                ClipboardPrivacyFilter.IsIgnoredApp(appName, settings.Current.ClipboardIgnoredApps))
            {
                return;
            }

            IDataObject? data = null;
            try
            {
                data = Clipboard.GetDataObject();
            }
            catch
            {
                // Clipboard open failed
            }

            if (data == null) return;

            // Check files
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                if (data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                {
                    clipboardStore.AddFiles(files, appName);
                    return;
                }
            }

            // Check bitmap
            if (data.GetDataPresent(DataFormats.Bitmap))
            {
                if (Clipboard.GetImage() is BitmapSource bmp)
                {
                    var enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(bmp));
                    using var ms = new MemoryStream();
                    enc.Save(ms);
                    byte[] bytes = ms.ToArray();
                    clipboardStore.AddImage(bytes, appName);

                    // Pass to screenshot watcher if snips are captured
                    if (settings.Current.ScreenshotsIncludeClipboard)
                    {
                        screenshotsWatcher.ProcessClipboardSnip(bytes);
                    }
                    return;
                }
            }

            // Check HTML
            string? html = null;
            if (data.GetDataPresent(DataFormats.Html))
            {
                html = data.GetData(DataFormats.Html) as string;
            }

            // Check Text
            if (data.GetDataPresent(DataFormats.UnicodeText))
            {
                string? text = data.GetData(DataFormats.UnicodeText) as string;
                if (!string.IsNullOrEmpty(text))
                {
                    if (html != null)
                    {
                        clipboardStore.AddRichText(text, html, appName);
                    }
                    else
                    {
                        clipboardStore.AddText(text, appName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log($"Clipboard listener error: {ex.Message}");
        }
    }

    private void OnPasteClipboardItem(ClipboardItem item, bool plainText)
    {
        suppressNextClipboardUpdate = true;
        try
        {
            var dataObject = new DataObject();
            if (plainText || item.Type == ClipboardItemType.Text)
            {
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.RichText)
            {
                if (!string.IsNullOrEmpty(item.HtmlText))
                    dataObject.SetData(DataFormats.Html, item.HtmlText);
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.Image && item.ImageBytes != null)
            {
                using var ms = new MemoryStream(item.ImageBytes);
                var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count > 0)
                    dataObject.SetData(DataFormats.Bitmap, decoder.Frames[0]);
            }
            else if (item.Type == ClipboardItemType.Files && item.FilePaths != null)
            {
                var sc = new System.Collections.Specialized.StringCollection();
                sc.AddRange(item.FilePaths.ToArray());
                dataObject.SetFileDropList(sc);
            }

            Clipboard.SetDataObject(dataObject, true);

            if (lastExternalForegroundHwnd != IntPtr.Zero)
            {
                ClipboardNative.SetForegroundWindow(lastExternalForegroundHwnd);
                System.Threading.Thread.Sleep(30);
                ClipboardNative.SendCtrlV();
            }

            if (settings.Current.ClipboardCollapseAfterPaste && !Pinned)
            {
                Collapse();
            }
        }
        catch (Exception ex)
        {
            log($"Paste failed: {ex.Message}");
        }
    }

    private void OnCopyClipboardItem(ClipboardItem item)
    {
        suppressNextClipboardUpdate = true;
        try
        {
            var dataObject = new DataObject();
            if (item.Type == ClipboardItemType.Text)
            {
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.RichText)
            {
                if (!string.IsNullOrEmpty(item.HtmlText))
                    dataObject.SetData(DataFormats.Html, item.HtmlText);
                dataObject.SetData(DataFormats.UnicodeText, item.Text ?? "");
            }
            else if (item.Type == ClipboardItemType.Image && item.ImageBytes != null)
            {
                using var ms = new MemoryStream(item.ImageBytes);
                var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count > 0)
                    dataObject.SetData(DataFormats.Bitmap, decoder.Frames[0]);
            }
            else if (item.Type == ClipboardItemType.Files && item.FilePaths != null)
            {
                var sc = new System.Collections.Specialized.StringCollection();
                sc.AddRange(item.FilePaths.ToArray());
                dataObject.SetFileDropList(sc);
            }

            Clipboard.SetDataObject(dataObject, true);
        }
        catch (Exception ex)
        {
            log($"Copy failed: {ex.Message}");
        }
    }

    private void OnPasteScreenshotItem(ScreenshotItem item)
    {
        if (File.Exists(item.FilePath))
        {
            suppressNextClipboardUpdate = true;
            try
            {
                var bmp = new BitmapImage(new Uri(item.FilePath, UriKind.Absolute));
                Clipboard.SetImage(bmp);

                if (lastExternalForegroundHwnd != IntPtr.Zero)
                {
                    ClipboardNative.SetForegroundWindow(lastExternalForegroundHwnd);
                    System.Threading.Thread.Sleep(30);
                    ClipboardNative.SendCtrlV();
                }

                if (settings.Current.ClipboardCollapseAfterPaste && !Pinned)
                {
                    Collapse();
                }
            }
            catch (Exception ex)
            {
                log($"Screenshot paste failed: {ex.Message}");
            }
        }
    }

    private void OnCopyScreenshotItem(ScreenshotItem item)
    {
        if (File.Exists(item.FilePath))
        {
            suppressNextClipboardUpdate = true;
            try
            {
                var bmp = new BitmapImage(new Uri(item.FilePath, UriKind.Absolute));
                Clipboard.SetImage(bmp);
            }
            catch (Exception ex)
            {
                log($"Screenshot copy failed: {ex.Message}");
            }
        }
    }

    private void LaunchSnip()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-screenclip:") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            log($"LaunchSnip error: {ex.Message}");
        }
    }
}
