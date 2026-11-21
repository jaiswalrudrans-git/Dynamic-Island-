using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Island.App.Header.ViewModels;
using Island.App.Shell;
using Island.Core.Settings;
using Xunit;

namespace Island.Tests.Shell;

public sealed class AppTabSwitchingAcceptanceTests
{
    [Fact]
    public void OpenEachTabOnce_NoExceptions()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IslandTabTest_" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(tempDir);
                var logs = new List<string>();
                var store = new SettingsStore(tempDir, logs.Add);

                if (Application.Current == null)
                {
                    _ = new Application();
                }

                if (Application.Current.Resources.MergedDictionaries.Count == 0)
                {
                    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Island.App;component/Design/DesignTokens.xaml", UriKind.Absolute)
                    });
                }

                var catalog = new Island.Core.Themes.ThemeCatalog(tempDir, logs.Add);
                var themes = new Island.App.Design.ThemeResources(Application.Current.Resources, catalog);
                themes.Apply(store.Current.Theme, store.Current.SurfaceStyle, store.Current.BackgroundTransparency, store.Current.CollapsedTransparency, store.Current.NeonIntensity);

                var window = new OverlayWindow(store, logs.Add);

                window.Show();
                window.OpenPanel();

                for (int i = 0; i < PanelTab.All.Length; i++)
                {
                    window.SelectTab(i);
                }

                window.Close();
                try { System.IO.Directory.Delete(tempDir, true); } catch { }
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        bool finished = thread.Join(TimeSpan.FromSeconds(10));

        Assert.True(finished, "OverlayWindow tab-switching timed out.");
        Assert.Null(threadException);
    }
}
