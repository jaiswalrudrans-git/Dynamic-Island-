using System.Drawing;
using System.Windows.Forms;
using Island.Core;

namespace Island.App.Shell;

internal sealed class IslandTray : IDisposable
{
    private readonly ContextMenuStrip menu = new();
    private readonly Icon icon;
    private readonly NotifyIcon tray;

    public IslandTray(OverlayWindow overlay)
    {
        using var stream = typeof(IslandTray).Assembly.GetManifestResourceStream("Island.App.Assets.Island.ico")!;
        icon = new Icon(stream, 32, 32);
        menu.Items.Add("Open", null, (_, _) => overlay.OpenPanel());
        var pin = menu.Items.Add("Pin", null, (_, _) => overlay.TogglePin());
        menu.Items.Add("Settings", null, (_, _) => overlay.OpenSettings());
        var pause = menu.Items.Add("Pause", null, (_, _) => overlay.SetPaused(!overlay.Paused));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => overlay.Close());
        menu.Opening += (_, _) =>
        {
            overlay.SetMenuOpen(true);
            pin.Text = overlay.Pinned ? "Unpin" : "Pin";
            pause.Text = overlay.Paused ? "Resume" : "Pause";
        };
        menu.Closed += (_, _) => overlay.SetMenuOpen(false, checkLeave: false);
        tray = new NotifyIcon { Icon = icon, Text = AppIdentity.Name, ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => overlay.OpenSettings();
    }

    public void Refresh()
    {
        try
        {
            tray.Visible = false;
            tray.Visible = true;
        }
        catch { }
    }

    public void Dispose()
    {
        tray.Visible = false;
        tray.Dispose();
        menu.Dispose();
        icon.Dispose();
    }
}
