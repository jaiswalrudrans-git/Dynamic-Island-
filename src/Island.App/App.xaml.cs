using System.IO;
using System.Windows;
using Island.App.Themes;
using Island.App.Views;
using Island.App.Interop;
using Island.Core;
using Island.Core.Services;
using Island.Core.Settings;
using Island.Core.Themes;

namespace Island.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        string directory = AppIdentity.DataDirectory;
        var log = new RollingLog(directory);
        try
        {
            var settings = new SettingsStore(directory, log.Write);
            settings.Load();
            var catalog = new ThemeCatalog(directory, log.Write);
            var resources = new ThemeResources(Resources, catalog);
            resources.Apply(settings.Current.Theme, settings.Current.SurfaceStyle);
            settings.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsStore.Current))
                    resources.Apply(settings.Current.Theme, settings.Current.SurfaceStyle);
            };

            var overlay = new OverlayWindow(settings, log.Write) { Title = AppIdentity.Name };
            MainWindow = overlay;
            overlay.Show();
            var tray = new IslandTray(overlay);
            overlay.Closed += (_, _) => tray.Dispose();
            log.Write($"{AppIdentity.Name} startup complete. Requested theme: {settings.Current.Theme}. " +
                $"Applied accent: {Resources[ThemeResources.AccentKey]}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            log.Write($"Startup failed: {ex.Message}");
            Shutdown(1);
        }
    }
}
