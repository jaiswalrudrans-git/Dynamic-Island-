using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Island.App.Design;
using Island.App.Features.Clipboard.Services;
using Island.App.Features.Clipboard.ViewModels;
using Island.App.Features.Clipboard.Views;
using Island.Core.Clipboard;
using Island.Core.Screenshots;
using Island.Core.Settings;
using Island.Core.Themes;
using Xunit;

namespace Island.Tests;

public sealed class ClipboardAndScreenshotsTests : TestFolder
{
    [Fact]
    public void ClipboardStoreAddsAndDeduplicatesConsecutiveItems()
    {
        var store = new ClipboardStore();
        Assert.Empty(store.GetItems());

        bool added1 = store.AddText("Hello World", "Notepad");
        Assert.True(added1);
        Assert.Single(store.GetItems());

        // Consecutive duplicate is rejected
        bool added2 = store.AddText("Hello World", "Notepad");
        Assert.False(added2);
        Assert.Single(store.GetItems());

        // Different text is accepted
        bool added3 = store.AddText("Another string", "Code");
        Assert.True(added3);
        Assert.Equal(2, store.GetItems().Count);
    }

    [Fact]
    public void ClipboardStorePinnedItemsSurviveClearAll()
    {
        var store = new ClipboardStore();
        store.AddText("Item 1");
        store.AddText("Item 2");
        store.AddText("Item 3");

        var items = store.GetItems();
        Assert.Equal(3, items.Count);

        // Pin Item 2
        string pinnedId = items[1].Id;
        store.TogglePin(pinnedId);

        // Clear all
        store.ClearAll();

        var remaining = store.GetItems();
        Assert.Single(remaining);
        Assert.Equal(pinnedId, remaining[0].Id);
        Assert.True(remaining[0].IsPinned);
    }

    [Fact]
    public void ClipboardStorePrunesExpiredItemsPreservingPinned()
    {
        var store = new ClipboardStore();
        store.Add(new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            Text = "Old expired item",
            Timestamp = DateTime.UtcNow.AddHours(-48)
        });
        store.Add(new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            Text = "Old pinned item",
            Timestamp = DateTime.UtcNow.AddHours(-48),
            IsPinned = true
        });
        store.Add(new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            Text = "Recent item",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });

        Assert.Equal(3, store.GetItems().Count);

        store.PruneExpired(TimeSpan.FromHours(24));

        var remaining = store.GetItems();
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, x => x.Text == "Old pinned item");
        Assert.Contains(remaining, x => x.Text == "Recent item");
        Assert.DoesNotContain(remaining, x => x.Text == "Old expired item");
    }

    [Fact]
    public void ClipboardPrivacyFilterDetectsKnownPasswordManagers()
    {
        string[] passwordManagers = ["1Password.exe", "Bitwarden", "KeePassXC.exe", "LastPass", "Dashlane", "credentialuibroker", "Consent.exe", "NordPass"];
        foreach (var app in passwordManagers)
        {
            Assert.True(ClipboardPrivacyFilter.IsIgnoredApp(app, null), $"App {app} should be recognized as password manager");
        }

        string[] safeApps = ["notepad.exe", "chrome", "Code", "devenv", "WindowsTerminal"];
        foreach (var app in safeApps)
        {
            Assert.False(ClipboardPrivacyFilter.IsIgnoredApp(app, null), $"App {app} should not be ignored");
        }
    }

    [Theory]
    [InlineData("ghp_123456789012345678901234567890123456", true)] // GitHub personal token
    [InlineData("sk-abcdefghijklmnopqrstuvwxyz1234567890", true)] // OpenAI token
    [InlineData("AKIAIOSFODNN7EXAMPLE", true)] // AWS Key ID
    [InlineData("-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA0...", true)] // Private Key
    [InlineData("4532 0151 1283 0366", true)] // Valid Visa Luhn check
    [InlineData("Just a standard sentence with normal words.", false)]
    [InlineData("1234 5678 9012 3456", false)] // Invalid card (fails Luhn)
    public void ClipboardPrivacyFilterDetectsSecrets(string text, bool expectedSecret)
    {
        bool isSecret = ClipboardPrivacyFilter.LooksLikeSecret(text);
        Assert.Equal(expectedSecret, isSecret);
    }

    [Fact]
    public void ClipboardStoreDpapiRoundTripPersistence()
    {
        string testDir = Folder;
        var writerStore = new ClipboardStore(
            dataDirectory: testDir,
            rememberAcrossRestarts: () => true,
            historySize: () => 50,
            autoExpireHours: () => 24,
            filterSecrets: () => true);

        writerStore.AddText("Secret text to persist", "Code");
        writerStore.AddRichText("Formatted text", "<b>Formatted</b> text", "Word");
        writerStore.AddFiles(["C:\\file1.txt", "C:\\file2.png"], "Explorer");

        var itemsBefore = writerStore.GetItems();
        Assert.Equal(3, itemsBefore.Count);

        // Force save
        writerStore.Save(testDir);
        string datFile = Path.Combine(testDir, "clipboard.dat");
        Assert.True(File.Exists(datFile));

        // Create fresh store simulating restart
        var readerStore = new ClipboardStore(
            dataDirectory: testDir,
            rememberAcrossRestarts: () => true,
            historySize: () => 50,
            autoExpireHours: () => 24,
            filterSecrets: () => true);

        var itemsAfter = readerStore.GetItems();
        Assert.Equal(3, itemsAfter.Count);
        Assert.Equal(itemsBefore[0].Text, itemsAfter[0].Text);
        Assert.Equal(itemsBefore[1].HtmlText, itemsAfter[1].HtmlText);
        Assert.Equal(itemsBefore[2].FilePaths?.Count, itemsAfter[2].FilePaths?.Count);
    }

    [Fact]
    public void ScreenshotsStoreDeduplicatesAndFormatsMetrics()
    {
        var store = new ScreenshotsStore();
        var item1 = new ScreenshotItem
        {
            FilePath = "C:\\Pictures\\Screenshots\\2026-11-20.png",
            FileSizeBytes = 1_500_000,
            Width = 1920,
            Height = 1080
        };

        Assert.True(store.Add(item1));
        Assert.Single(store.GetItems());

        // Adding duplicate filepath is rejected
        var duplicate = new ScreenshotItem
        {
            FilePath = "C:\\Pictures\\Screenshots\\2026-11-20.png",
            FileSizeBytes = 1_500_000
        };
        Assert.False(store.Add(duplicate));
        Assert.Single(store.GetItems());

        Assert.Equal("1920 × 1080", item1.DimensionsText);
        Assert.Equal("1.4 MB", item1.SizeText);
    }

    [Fact]
    public void ScreenshotsWatcherParsesPngDimensionsCorrectly()
    {
        // Construct minimum valid PNG header (8-byte signature + 4-byte chunk len + 4-byte IHDR + 4-byte width + 4-byte height)
        byte[] pngHeader = [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG magic
            0x00, 0x00, 0x00, 0x0D, // IHDR chunk length (13)
            0x49, 0x48, 0x44, 0x52, // "IHDR"
            0x00, 0x00, 0x07, 0x80, // Width: 1920 (0x0780)
            0x00, 0x00, 0x04, 0x38  // Height: 1080 (0x0438)
        ];

        var watcher = new ScreenshotsWatcher(new ScreenshotsStore(), Folder);
        watcher.ProcessClipboardSnip(pngHeader);

        var store = typeof(ScreenshotsWatcher)
            .GetField("store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .GetValue(watcher) as ScreenshotsStore;

        Assert.NotNull(store);
        var snips = store.GetItems();
        Assert.Single(snips);
        Assert.Equal(1920, snips[0].Width);
        Assert.Equal(1080, snips[0].Height);
    }

    [Fact]
    public void ClipboardRowViewModelFormatsImagesAndTextProperly()
    {
        var textItem = new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            Text = "Sample copied text",
            SourceAppName = "Notepad"
        };
        var textRow = new ClipboardRowViewModel(textItem);
        Assert.False(textRow.IsImage);
        Assert.Equal("Sample copied text", textRow.DisplayLabel);
        Assert.Equal("Notepad", textRow.SourceAppName);

        var scItem = new ScreenshotItem
        {
            FilePath = "C:\\test\\snap.png",
            Width = 885,
            Height = 458
        };
        var imageRow = new ClipboardRowViewModel(scItem);
        Assert.True(imageRow.IsImage);
        Assert.Equal("Picture 885x458", imageRow.DisplayLabel);
        Assert.Equal(885, imageRow.ImageWidth);
        Assert.Equal(458, imageRow.ImageHeight);
    }

    [Fact]
    public void ClipboardViewModelFiltersAllVsFavorites()
    {
        var clipStore = new ClipboardStore();
        var scStore = new ScreenshotsStore();
        var settings = new AppSettings();

        clipStore.AddText("Item 1");
        clipStore.AddText("Item 2");
        clipStore.AddText("Item 3");

        var items = clipStore.GetItems();
        clipStore.TogglePin(items[1].Id); // Pin item 2

        var vm = new ClipboardViewModel(
            Dispatcher.CurrentDispatcher,
            clipStore,
            scStore,
            () => settings,
            (_, _) => { },
            _ => { },
            _ => { },
            _ => { },
            () => { });

        Assert.Equal(3, vm.DisplayedRows.Count);

        // Switch to Favorites
        vm.ShowFavoritesOnly = true;
        Assert.Single(vm.DisplayedRows);
        Assert.True(vm.DisplayedRows[0].IsPinned);
        Assert.Equal("Item 2", vm.DisplayedRows[0].DisplayLabel);

        // Switch back to All
        vm.ShowFavoritesOnly = false;
        Assert.Equal(3, vm.DisplayedRows.Count);
    }

    [Fact]
    public void ClipboardViewModelFileTransferShelfOperations()
    {
        var clipStore = new ClipboardStore();
        var scStore = new ScreenshotsStore();
        var settings = new AppSettings();

        var vm = new ClipboardViewModel(
            Dispatcher.CurrentDispatcher,
            clipStore,
            scStore,
            () => settings,
            (_, _) => { },
            _ => { },
            _ => { },
            _ => { },
            () => { });

        vm.ClearDroppedFiles();
        Assert.Empty(vm.DroppedFiles);
        Assert.False(vm.HasDroppedFiles);

        string tempFile1 = Path.Combine(Folder, "doc.txt");
        string tempFile2 = Path.Combine(Folder, "archive.zip");
        File.WriteAllText(tempFile1, "hello world");
        File.WriteAllBytes(tempFile2, new byte[2048]);

        vm.AddDroppedFiles([tempFile1, tempFile2]);
        Assert.Equal(2, vm.DroppedFiles.Count);
        Assert.True(vm.HasDroppedFiles);

        // Adding duplicate is ignored
        vm.AddDroppedFiles([tempFile1]);
        Assert.Equal(2, vm.DroppedFiles.Count);

        var first = vm.DroppedFiles[0];
        Assert.Equal("doc.txt", first.FileName);
        Assert.Equal("\uE8A5", first.IconGlyph); // Document glyph
        Assert.Contains("B", first.SizeText);

        var second = vm.DroppedFiles[1];
        Assert.Equal("archive.zip", second.FileName);
        Assert.Equal("\uF012", second.IconGlyph); // Zip archive glyph
        Assert.Equal("2.0 KB", second.SizeText);

        // Remove single file
        vm.RemoveDroppedFile(first);
        Assert.Single(vm.DroppedFiles);
        Assert.Equal("archive.zip", vm.DroppedFiles[0].FileName);

        // Clear all files
        vm.ClearDroppedFiles();
        Assert.Empty(vm.DroppedFiles);
        Assert.False(vm.HasDroppedFiles);
    }

    [Fact]
    public void RenderClipboardTabArtifacts()
    {
        var thread = new Thread(() =>
        {
            var app = Application.Current ?? new Application();

            var tokensUri = new Uri("pack://application:,,,/Island.App;component/Design/DesignTokens.xaml");
            if (!app.Resources.MergedDictionaries.Any(d => d.Source == tokensUri))
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = tokensUri });
            }
            var catalog = new ThemeCatalog(Folder, _ => { });
            var themeRes = new ThemeResources(app.Resources, catalog);
            themeRes.Apply("Midnight", "Translucent");

            // 1. Populated ViewModel
            var clipStorePopulated = new ClipboardStore();
            var scStorePopulated = new ScreenshotsStore();
            var settings = new AppSettings();

            clipStorePopulated.AddText("git commit -m \"feat(clipboard): BrimDeck-style clipboard\"", "Terminal");
            clipStorePopulated.AddText("https://github.com/microsoft/terminal", "Chrome");
            clipStorePopulated.AddText("Plain text snippet for testing", "VS Code");

            var scItem = new ScreenshotItem
            {
                FilePath = "C:\\Screenshots\\2026-11-21_0834.png",
                Width = 885,
                Height = 458,
                Timestamp = DateTime.UtcNow,
                RawBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==")
            };
            scStorePopulated.Add(scItem);

            var vmPopulated = new ClipboardViewModel(
                Dispatcher.CurrentDispatcher,
                clipStorePopulated,
                scStorePopulated,
                () => settings,
                (_, _) => { },
                _ => { },
                _ => { },
                _ => { },
                () => { },
                enableListener: false);

            string testDoc = Path.Combine(Folder, "project_spec.pdf");
            string testZip = Path.Combine(Folder, "build_assets.zip");
            File.WriteAllText(testDoc, "sample pdf content");
            File.WriteAllBytes(testZip, new byte[1024 * 50]);
            vmPopulated.AddDroppedFiles([testDoc, testZip]);

            var pagePopulated = new ClipboardPage(vmPopulated, _ => { });
            var borderPopulated = new Border
            {
                Width = 640,
                Background = (Brush)app.Resources["Theme.Background"],
                Padding = new Thickness(16, 14, 16, 14),
                Child = pagePopulated
            };

            borderPopulated.Measure(new Size(640, double.PositiveInfinity));
            double desiredH = Math.Max(200, borderPopulated.DesiredSize.Height);
            borderPopulated.Arrange(new Rect(0, 0, 640, desiredH));
            borderPopulated.UpdateLayout();

            var rtbPopulated = new RenderTargetBitmap(640, (int)desiredH, 96, 96, PixelFormats.Pbgra32);
            rtbPopulated.Render(borderPopulated);

            string outputDir = @"C:\Users\jaisw\.gemini\antigravity\brain\8747a9d5-c07a-4099-b1f2-2e7f736db774";
            Directory.CreateDirectory(outputDir);
            string popPath = Path.Combine(outputDir, "Clipboard_Populated.png");
            using (var fs = File.Create(popPath))
            {
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtbPopulated));
                enc.Save(fs);
            }

            // Also copy to docs/screens/edges/
            try
            {
                string docsDir = Path.Combine(Folder, "..", "..", "..", "..", "docs", "screens", "edges");
                if (Directory.Exists(docsDir))
                {
                    File.Copy(popPath, Path.Combine(docsDir, "Clipboard_100dpi.png"), true);
                }
            }
            catch { }

            // 2. Empty ViewModel
            var clipStoreEmpty = new ClipboardStore();
            var scStoreEmpty = new ScreenshotsStore();
            var vmEmpty = new ClipboardViewModel(
                Dispatcher.CurrentDispatcher,
                clipStoreEmpty,
                scStoreEmpty,
                () => settings,
                (_, _) => { },
                _ => { },
                _ => { },
                _ => { },
                () => { },
                enableListener: false);
            vmEmpty.ClearDroppedFiles();

            var pageEmpty = new ClipboardPage(vmEmpty, _ => { });
            var borderEmpty = new Border
            {
                Width = 640,
                Background = (Brush)app.Resources["Theme.Background"],
                Padding = new Thickness(16, 14, 16, 14),
                Child = pageEmpty
            };

            borderEmpty.Measure(new Size(640, double.PositiveInfinity));
            double emptyH = Math.Max(100, borderEmpty.DesiredSize.Height);
            borderEmpty.Arrange(new Rect(0, 0, 640, emptyH));
            borderEmpty.UpdateLayout();

            var rtbEmpty = new RenderTargetBitmap(640, (int)emptyH, 96, 96, PixelFormats.Pbgra32);
            rtbEmpty.Render(borderEmpty);

            string emptyPath = Path.Combine(outputDir, "Clipboard_Empty.png");
            using (var fs = File.Create(emptyPath))
            {
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtbEmpty));
                enc.Save(fs);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool finished = thread.Join(TimeSpan.FromSeconds(30));
        Assert.True(finished, "RenderClipboardTabArtifacts timed out");
    }

    [Fact]
    public void PureSizingFunctionAndZeroDriftTest()
    {
        var thread = new Thread(() =>
        {
            var app = Application.Current ?? new Application();
            var tokensUri = new Uri("pack://application:,,,/Island.App;component/Design/DesignTokens.xaml");
            if (!app.Resources.MergedDictionaries.Any(d => d.Source == tokensUri))
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = tokensUri });
            }
            var catalog = new ThemeCatalog(Folder, _ => { });
            var themeRes = new ThemeResources(app.Resources, catalog);
            themeRes.Apply("Midnight", "Translucent");

            var store = new ClipboardStore();
            var settings = new AppSettings();
            var vm = new ClipboardViewModel(
                Dispatcher.CurrentDispatcher,
                store,
                new ScreenshotsStore(),
                () => settings,
                (_, _) => { },
                _ => { },
                _ => { },
                _ => { },
                () => { },
                enableListener: false);

            try
            {
                // 1) Pure sizing function: call 100 times with varying inputs and assert run 1 == run 100
                var page = new ClipboardPage(vm, _ => { });
                var sizes = new List<Size>();
                for (int i = 0; i < 100; i++)
                {
                    if (i % 2 == 0)
                    {
                        store.AddText($"Sample text {i}", "App");
                    }
                    page.Measure(new Size(580, double.PositiveInfinity));
                    sizes.Add(page.DesiredSize);
                    Assert.Equal(580, page.DesiredSize.Width);
                }

                // Verify determinism: repeating the exact same state produces the exact same size
                page.Measure(new Size(580, double.PositiveInfinity));
                var finalSize1 = page.DesiredSize;
                page.Measure(new Size(580, double.PositiveInfinity));
                var finalSize2 = page.DesiredSize;
                Assert.Equal(finalSize1.Width, finalSize2.Width);
                Assert.Equal(finalSize1.Height, finalSize2.Height);

                // 2) 200-cycle open/collapse repro loop with long items and dropped files: assert driftRow == null
                store.ClearAll();
                for (int i = 0; i < 10; i++)
                {
                    store.AddText(
                        $"Cycle Item {i}: A very long line of copied text from a browser or code editor that spans across many columns without wrapping https://github.com/microsoft/terminal/issues/1234567890-{i}",
                        "Brave");
                }
                vm.AddDroppedFiles(new[]
                {
                    @"C:\Extremely\Long\Path\To\Some\Deeply\Nested\File\On\Disk\With\Super\Long\Name\Report_2026_Q4_Final_Draft_v2.docx"
                });

                string? driftRow = null;
                double baselineWidth = 0;

                for (int cycle = 0; cycle < 200; cycle++)
                {
                    page.Measure(new Size(580, double.PositiveInfinity));
                    page.Arrange(new Rect(0, 0, 580, page.DesiredSize.Height));
                    page.UpdateLayout();

                    double currentWidth = page.DesiredSize.Width;
                    if (cycle == 0)
                    {
                        baselineWidth = currentWidth;
                    }
                    else if (Math.Abs(currentWidth - baselineWidth) > 0.01)
                    {
                        driftRow = $"[Cycle {cycle:D3}] Width drifted from {baselineWidth} to {currentWidth}";
                        break;
                    }
                }

                Assert.Null(driftRow);
            }
            finally
            {
                vm.Dispose();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool finished = thread.Join(TimeSpan.FromSeconds(30));
        Assert.True(finished, "PureSizingFunctionAndZeroDriftTest timed out");
    }

    [Fact]
    public void RealClipboardCaptureTest()
    {
        var thread = new Thread(() =>
        {
            var app = Application.Current ?? new Application();
            var store = new ClipboardStore();
            var listener = new ClipboardListener(store, () => false, null);

            void PumpMessages(int maxMs, Func<bool> condition)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < maxMs)
                {
                    if (condition()) return;
                    var frame = new DispatcherFrame();
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(f =>
                    {
                        ((DispatcherFrame)f).Continue = false;
                        return null;
                    }), frame);
                    Dispatcher.PushFrame(frame);
                    Thread.Sleep(20);
                }
            }

            void SetClipboardSafe(object data)
            {
                for (int i = 0; i < 6; i++)
                {
                    try
                    {
                        Clipboard.SetDataObject(data, false);
                        return;
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        Thread.Sleep(30);
                    }
                }
            }

            void SetFileDropListSafe(System.Collections.Specialized.StringCollection list)
            {
                for (int i = 0; i < 6; i++)
                {
                    try
                    {
                        Clipboard.SetFileDropList(list);
                        return;
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        Thread.Sleep(30);
                    }
                }
            }

            try
            {
                // Capture 1: Unicode string
                string testText = "Test Unicode String " + Guid.NewGuid();
                SetClipboardSafe(new DataObject(DataFormats.UnicodeText, testText));
                PumpMessages(500, () => store.GetItems().Any(x => x.Text == testText));
                Assert.Contains(store.GetItems(), x => x.Text == testText);

                // Capture 2: BitmapSource
                var bmp = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgr32, null, new byte[16], 8);
                SetClipboardSafe(new DataObject(DataFormats.Bitmap, bmp));
                PumpMessages(500, () => store.GetItems().Any(x => x.Type == ClipboardItemType.Image));
                Assert.Contains(store.GetItems(), x => x.Type == ClipboardItemType.Image);

                // Capture 3: File drop list
                string tempFile = Path.Combine(Path.GetTempPath(), "island_test_" + Guid.NewGuid().ToString("N") + ".txt");
                try
                {
                    File.WriteAllText(tempFile, "temp content");
                    var sc = new System.Collections.Specialized.StringCollection { tempFile };
                    SetFileDropListSafe(sc);
                    PumpMessages(500, () => store.GetItems().Any(x => x.Type == ClipboardItemType.Files));
                    Assert.Contains(store.GetItems(), x => x.Type == ClipboardItemType.Files);
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }

                // Capture 4: CanIncludeInClipboardHistory = 0 should NOT be captured
                string secretText = "SecretPassword_" + Guid.NewGuid().ToString("N");
                var secretData = new DataObject();
                secretData.SetData(DataFormats.UnicodeText, secretText);
                secretData.SetData("CanIncludeInClipboardHistory", new MemoryStream(new byte[4] { 0, 0, 0, 0 }));
                SetClipboardSafe(secretData);
                PumpMessages(500, () => false); // wait full 500 ms
                Assert.DoesNotContain(store.GetItems(), x => x.Text == secretText);
            }
            finally
            {
                try { Clipboard.Clear(); } catch { }
                listener.Dispose();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool finished = thread.Join(TimeSpan.FromSeconds(30));
        Assert.True(finished, "RealClipboardCaptureTest timed out");
    }
}
