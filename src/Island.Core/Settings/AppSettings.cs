using System.Text.Json;
using System.Text.Json.Serialization;

namespace Island.Core.Settings;

public sealed record AppSettings
{
    public const int CurrentSchema = 1;
    public int SchemaVersion { get; init; } = CurrentSchema;
    public string Theme { get; init; } = "Dark";
    public bool Use24HourClock { get; init; } = true;
    public bool ShowSeconds { get; init; }
    public bool ReduceMotion { get; init; }
    public double AnimationSpeed { get; init; } = 1;

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalSettings { get; init; }

    internal AppSettings Normalize() => this with
    {
        Theme = string.IsNullOrWhiteSpace(Theme) ? "Dark" : Theme.Trim(),
        AnimationSpeed = double.IsFinite(AnimationSpeed) ? Math.Clamp(AnimationSpeed, 0.5, 2) : 1
    };
}
