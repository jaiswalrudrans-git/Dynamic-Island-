namespace Island.App.ViewModels;

internal sealed record PanelTab(string Name, string Glyph, string Description)
{
    public static readonly PanelTab[] All =
    [
        new("Music", "\uE8D6", "Music"),
        new("System", "\uE950", "System Performance"),
        new("Tasks", "\uE9D5", "Tasks & To-Dos"),
        new("Controls", "\uE9E9", "Quick Controls"),
        new("Focus", "\uE916", "Focus Timer"),
        new("Calendar", "\uE787", "Calendar & Weather")
    ];
}
