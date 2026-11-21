namespace Island.App.Design;

public static class IslandMetrics
{
    /// <summary>
    /// Shoulder 14 DIP + side padding 16 DIP = 30 DIP from bounding box edge to content.
    /// Inset from visible straight notch wall to content is exactly 16 DIP.
    /// </summary>
    public const double WallInset = 30.0;

    /// <summary>
    /// Total horizontal content inset (both sides): 2 * WallInset = 60 DIP.
    /// </summary>
    public const double HorizontalInset = WallInset * 2.0;
}
