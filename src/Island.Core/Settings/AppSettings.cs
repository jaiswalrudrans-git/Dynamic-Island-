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
    public string? MonitorDeviceName { get; init; }
    public string? PreferredPlayer { get; init; }
    public bool ShowMusicPreview { get; init; } = true;
    public bool MusicEqualizer { get; init; } = true;
    public bool LyricsEnabled { get; init; } = true;
    public bool CoverGlow { get; init; } = true;
    public string MusicPreviewText { get; init; } = "Clock";
    public bool ReduceMotion { get; init; }
    public bool IsPinned { get; init; }
    public int HoverDelayMs { get; init; } = 120;
    public int CollapseDelayMs { get; init; } = 300;
    public double AnimationSpeed { get; init; } = 1;

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalSettings { get; init; }

    internal AppSettings Normalize() => this with
    {
        Theme = string.IsNullOrWhiteSpace(Theme) ? "Dark" : Theme.Trim(),
        MusicPreviewText = MusicPreviewText is "Title" or "Lyric" ? MusicPreviewText : "Clock",
        HoverDelayMs = Math.Clamp(HoverDelayMs, 0, 2000),
        CollapseDelayMs = Math.Clamp(CollapseDelayMs, 0, 5000),
        AnimationSpeed = double.IsFinite(AnimationSpeed) ? Math.Clamp(AnimationSpeed, 0.5, 2) : 1
    };
}
