using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Island.App.Themes;
using Island.Core.Motion;
using Island.Core.Settings;
using Island.Core.Themes;
using Xunit;

namespace Island.Tests;

public sealed class StressAndPerformanceTests : TestFolder
{
    [Fact]
    public void ColdStartSettingsAndThemeResolutionUnderBudget()
    {
        var sw = Stopwatch.StartNew();

        var store = new SettingsStore(Folder, Messages.Add);
        store.Load();

        var catalog = new ThemeCatalog(Folder, Messages.Add);
        var res = new ResourceDictionary();
        var theme = new ThemeResources(res, catalog);
        theme.Apply(store.Current.Theme, store.Current.SurfaceStyle);

        sw.Stop();

        // Must initialize well under 600 ms budget
        Assert.True(sw.ElapsedMilliseconds < 600, $"Cold start elapsed time was {sw.ElapsedMilliseconds} ms, expected < 600 ms");
    }

    [Fact]
    public void SettingsStoreRecoversFromCorruptedJsonGracefully()
    {
        string settingsFile = Path.Combine(Folder, "settings.json");
        File.WriteAllText(settingsFile, "{ this is corrupted json broken content !@#$%^&* }");

        var store = new SettingsStore(Folder, Messages.Add);
        store.Load(); // Should not throw

        Assert.NotNull(store.Current);
        Assert.Equal(AppSettings.CurrentSchema, store.Current.SchemaVersion);

        // Verify corrupted file was backed up
        var backupFiles = Directory.GetFiles(Folder, "settings.invalid-*.json");
        Assert.NotEmpty(backupFiles);
    }

    [Fact]
    public void Rapid5000CycleExpandCollapseStressTestHasZeroMemoryLeaks()
    {
        var thread = new Thread(() =>
        {
            var springW = new SpringValue();
            var springH = new SpringValue();
            var springAlpha = new SpringValue();

            // Warm-up 500 cycles
            for (int i = 0; i < 500; i++)
            {
                bool expand = (i % 2) == 0;
                double targetW = expand ? 680 : 136;
                double targetH = expand ? 380 : 35;
                double targetAlpha = expand ? 1.0 : 0.0;

                for (int step = 0; step < 8; step++)
                {
                    springW.Advance(targetW, 0.016);
                    springH.Advance(targetH, 0.016);
                    springAlpha.Advance(targetAlpha, 0.016);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long baselineMemory = GC.GetTotalMemory(true);

            // Execute 5,000 stress cycles
            for (int i = 0; i < 5000; i++)
            {
                bool expand = (i % 2) == 0;
                double targetW = expand ? 680 : 136;
                double targetH = expand ? 380 : 35;
                double targetAlpha = expand ? 1.0 : 0.0;

                for (int step = 0; step < 6; step++)
                {
                    springW.Advance(targetW, 0.016);
                    springH.Advance(targetH, 0.016);
                    springAlpha.Advance(targetAlpha, 0.016);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long postStressMemory = GC.GetTotalMemory(true);
            long diffBytes = postStressMemory - baselineMemory;

            // Memory difference must be negligible (< 2 MB for 5,000 cycles)
            Assert.True(diffBytes < 2 * 1024 * 1024, $"Memory grew by {diffBytes / 1024} KB after 5000 cycles, expected < 2048 KB");
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
