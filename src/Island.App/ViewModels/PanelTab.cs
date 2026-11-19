namespace Island.App.ViewModels;

internal sealed record PanelTab(string Name, string Glyph, string Description)
{
    public static readonly PanelTab[] All =
    [
        new("Music", "\uE8D6", ""),
        new("System", "\uE950", ""),
        new("Tasks", "\uE9D5", ""),
        new("Quick Controls", "\uE9E9", ""),
        new("Focus", "\uE916", ""),
        new("Calendar & Weather", "\uE787", "Your agenda and local forecast arrive in M7.")
    ];
}
