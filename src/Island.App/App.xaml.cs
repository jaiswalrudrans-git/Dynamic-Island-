using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Island.App.Design;
using Island.App.Header.Views;
using Island.App.Shell;
using Island.App.Core;
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

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);

    public static readonly uint WM_ISLAND_ACTIVATE = RegisterWindowMessage("ISLAND_ACTIVATE_NOTIFICATION");
    private static System.Threading.Mutex? singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        singleInstanceMutex = new System.Threading.Mutex(true, @"Local\Island_App_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            PostMessage((nint)0xffff, WM_ISLAND_ACTIVATE, 0, 0);
            Shutdown(0);
            return;
        }

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
            args.Handled = true; // Recover from recoverable dispatch exceptions
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            string err = $"AppDomain unhandled exception: {args.ExceptionObject}";
            log.Write(err);
            try { File.AppendAllText(Path.Combine(directory, "crash.log"), $"{DateTime.Now:O} {err}{Environment.NewLine}"); } catch { }
        };
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            log.Write($"TaskScheduler unobserved task exception: {args.Exception}");
            args.SetObserved();
        };

        try
        {
            var settings = new SettingsStore(directory, log.Write);
            settings.Load();
            var catalog = new ThemeCatalog(directory, log.Write);
            var resources = new ThemeResources(Resources, catalog);
            var typography = new TypographyResources(Resources);

            void UpdateThemeAndTypography(AppSettings cur)
            {
                Color? clockCol = cur.MatchAccentToClock ? ClockFace.ResolveClockColor(cur) : null;
                resources.Apply(cur.Theme, cur.SurfaceStyle, cur.BackgroundTransparency, cur.CollapsedTransparency, cur.NeonIntensity, clockCol);
                typography.Apply(cur, clockCol ?? ClockFace.ResolveClockColor(cur));
            }

            UpdateThemeAndTypography(settings.Current);
            settings.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(SettingsStore.Current))
                    UpdateThemeAndTypography(settings.Current);
            };

            var overlay = new OverlayWindow(settings, log.Write) { Title = AppIdentity.Name };
            MainWindow = overlay;
            overlay.Show();

            if (e.Args.Contains("--layout-debug") || e.Args.Contains("-layout-debug"))
            {
                settings.Update(s => s with { LayoutDebug = true });
            }

            if (e.Args.Contains("--open") || e.Args.Contains("-open"))
            {
                overlay.OpenPanel();
            }

            for (int i = 0; i < e.Args.Length; i++)
            {
                if ((e.Args[i] == "--tab" || e.Args[i] == "-tab") && i + 1 < e.Args.Length && int.TryParse(e.Args[i + 1], out int tabIdx))
                {
                    overlay.SelectTab(tabIdx);
                }
            }

            var tray = new IslandTray(overlay);
            overlay.AttachTray(tray);
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

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        if (singleInstanceMutex != null)
        {
            try
            {
                singleInstanceMutex.ReleaseMutex();
            }
            catch { }
            singleInstanceMutex.Dispose();
            singleInstanceMutex = null;
        }
    }
}
