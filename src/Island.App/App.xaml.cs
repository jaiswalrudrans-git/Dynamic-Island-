using System.IO;
using System.Windows;
using Island.App.Themes;
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
            resources.Apply(settings.Current.Theme);
            settings.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsStore.Current))
                    resources.Apply(settings.Current.Theme);
            };

            // M0 exercises the actual WPF startup path. M1 supplies the first window.
            log.Write($"{AppIdentity.Name} startup complete. Requested theme: {settings.Current.Theme}. " +
                $"Applied accent: {Resources[ThemeResources.AccentKey]}.");
            Shutdown();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log.Write($"Startup failed: {ex.Message}");
            Shutdown(1);
        }
    }
}
