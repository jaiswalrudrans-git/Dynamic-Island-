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
    public string CollapsedStat { get; init; } = "None";
    public string SystemProcessSort { get; init; } = "Cpu";
    public int SystemGpuIndex { get; init; }
    public bool AlertsEnabled { get; init; } = true;
    public bool AlertBatteryEnabled { get; init; } = true;
    public int AlertBatteryLowThreshold { get; init; } = 20;
    public int AlertBatteryCriticalThreshold { get; init; } = 10;
    public bool AlertChargerEnabled { get; init; } = true;
    public bool AlertTemperatureEnabled { get; init; } = true;
    public int AlertTemperatureThreshold { get; init; } = 85;
    public bool AlertVolumeOsdEnabled { get; init; } = true;
    public bool AlertBrightnessOsdEnabled { get; init; } = true;
    public bool AlertMicOsdEnabled { get; init; } = true;
    public bool AlertSoundEnabled { get; init; } = true;
    public int AlertDurationSeconds { get; init; } = 4;

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalSettings { get; init; }

    public AppSettings Normalize() => this with
    {
        Theme = string.IsNullOrWhiteSpace(Theme) ? "Dark" : Theme.Trim(),
        MusicPreviewText = MusicPreviewText is "Title" or "Lyric" ? MusicPreviewText : "Clock",
        HoverDelayMs = Math.Clamp(HoverDelayMs, 0, 2000),
        CollapseDelayMs = Math.Clamp(CollapseDelayMs, 0, 5000),
        AnimationSpeed = double.IsFinite(AnimationSpeed) ? Math.Clamp(AnimationSpeed, 0.5, 2) : 1,
        CollapsedStat = CollapsedStat is "Cpu" or "Ram" or "Network" or "Battery" or "CpuRam" ? CollapsedStat : "None",
        SystemProcessSort = SystemProcessSort is "Memory" ? "Memory" : "Cpu",
        SystemGpuIndex = Math.Max(0, SystemGpuIndex),
        AlertBatteryLowThreshold = Math.Clamp(AlertBatteryLowThreshold, 1, 50),
        AlertBatteryCriticalThreshold = Math.Clamp(AlertBatteryCriticalThreshold, 1, 25),
        AlertTemperatureThreshold = Math.Clamp(AlertTemperatureThreshold, 50, 115),
        AlertDurationSeconds = Math.Clamp(AlertDurationSeconds, 1, 15)
    };
}
