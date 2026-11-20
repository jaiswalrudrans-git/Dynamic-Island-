namespace Island.App.ViewModels;

internal sealed record PanelTab(string Id, string Name, string Glyph, string Description)
{
    public static readonly PanelTab[] All =
    [
        new("music", "Music", "\uE8D6", "Music"),
        new("system", "System", "\uE950", "System Performance"),
        new("tasks", "Tasks", "\uE9D5", "Tasks & To-Dos"),
        new("controls", "Controls", "\uE9E9", "Quick Controls"),
        new("focus", "Focus", "\uE916", "Focus Timer"),
        new("calendar", "Calendar", "\uE787", "Calendar & Weather"),
        new("clipboard", "Clipboard", "\uE8C8", "Clipboard History"),
        new("screenshots", "Screenshots", "\uEB9F", "Screenshots")
    ];
}
