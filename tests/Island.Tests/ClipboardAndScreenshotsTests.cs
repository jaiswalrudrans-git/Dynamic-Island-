using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Island.Core.Clipboard;
using Island.Core.Screenshots;
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
}
