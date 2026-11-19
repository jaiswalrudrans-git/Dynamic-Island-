namespace Island.App.ViewModels;

internal sealed record PanelTab(string Name, string Glyph, string Description)
{
    public static readonly PanelTab[] All =
    [
        new("Music", "\uE8D6", ""),
        new("System", "\uE950", ""),
        new("Calendar & Weather", "\uE787", "Your agenda and local forecast arrive in M7."),
        new("Tasks", "\uE9D5", "To-dos and reminders arrive in M6."),
        new("Focus", "\uE916", "Focus sessions, timers and stopwatch arrive in M6."),
        new("Quick Controls", "\uE9E9", "Device and audio controls arrive in M6.")
    ];
}
