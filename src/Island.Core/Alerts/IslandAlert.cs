namespace Island.Core.Alerts;

public enum AlertType
{
    Volume,
    Brightness,
    Mic,
    BatteryLow,
    BatteryCritical,
    Charger,
    Temperature,
    Custom
}

public enum AlertLevel
{
    Info,
    Warning,
    Critical
}

public sealed record IslandAlert(
    string Id,
    AlertType Type,
    string Title,
    string Glyph,
    AlertLevel Level = AlertLevel.Info,
    double? Progress = null,
    TimeSpan? Duration = null,
    bool PlaySound = false
)
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
