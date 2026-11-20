using System.IO;
using System.Runtime.InteropServices;
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
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AttachConsole(ATTACH_PARENT_PROCESS);

        Console.WriteLine();
        Console.WriteLine("========================================================");
        Console.WriteLine($"[Island] {AppIdentity.Name} is running!");
        Console.WriteLine("[Island] Positioned at the TOP-CENTER of your primary display.");
        Console.WriteLine("[Island] Move your mouse to the top edge of your screen to expand.");
        Console.WriteLine("[Island] Check your Windows taskbar system tray for the Island icon.");
        Console.WriteLine("========================================================");

        string directory = AppIdentity.DataDirectory;
        var log = new RollingLog(directory);
        DispatcherUnhandledException += (s, args) =>
        {
            log.Write($"Dispatcher unhandled exception: {args.Exception}");
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            log.Write($"AppDomain unhandled exception: {args.ExceptionObject}");
        };

        try
        {
            var settings = new SettingsStore(directory, log.Write);
            settings.Load();
            var catalog = new ThemeCatalog(directory, log.Write);
            var resources = new ThemeResources(Resources, catalog);
            resources.Apply(settings.Current.Theme, settings.Current.SurfaceStyle, settings.Current.BackgroundTransparency, settings.Current.CollapsedTransparency, settings.Current.NeonIntensity);
            settings.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsStore.Current))
                    resources.Apply(settings.Current.Theme, settings.Current.SurfaceStyle, settings.Current.BackgroundTransparency, settings.Current.CollapsedTransparency, settings.Current.NeonIntensity);
            };

            var overlay = new OverlayWindow(settings, log.Write) { Title = AppIdentity.Name };
            MainWindow = overlay;
            overlay.Show();

            if (e.Args.Contains("--open") || e.Args.Contains("-open"))
            {
                overlay.OpenPanel();
            }

            var tray = new IslandTray(overlay);
            overlay.Closed += (_, _) =>
            {
                log.Write("OverlayWindow Closed fired.");
                tray.Dispose();
            };
            log.Write($"{AppIdentity.Name} startup complete. Requested theme: {settings.Current.Theme}. " +
                $"Applied accent: {Resources[ThemeResources.AccentKey]}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Console.WriteLine($"[Island] Startup failed: {ex.Message}");
            log.Write($"Startup failed: {ex.Message}");
            Shutdown(1);
        }
    }
}
