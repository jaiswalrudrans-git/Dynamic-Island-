using System.Text.Json;
using System.Text.Json.Serialization;

namespace Island.Core.Settings;

public sealed record AppSettings
{
    public const int CurrentSchema = 1;
    public int SchemaVersion { get; init; } = CurrentSchema;
    public string Theme { get; init; } = "Dark";
    public string InterfaceSize { get; init; } = "Auto"; // "Auto", "Compact", "Comfortable", "Large"
    public double UiScale { get; init; } = 1.0; // 0.90 to 1.50
    public string SurfaceStyle { get; init; } = "Opaque"; // "Opaque", "Translucent", "Clear"
    public double BackgroundTransparency { get; init; } = 12.0; // 0% to 70% (default 12%)
    public double CollapsedTransparency { get; init; } = 0.0;   // 0% to 50% (default 0%)
    public bool Use24HourClock { get; init; } = true;
    public bool ShowSeconds { get; init; }
    public string ClockStyle { get; init; } = "Simple";
    public string ClockColor { get; init; } = "White";
    public string ClockCustomColor { get; init; } = "#00E5FF";
    public bool ClockBlinkingColon { get; init; }
    public bool ClockLeadingZero { get; init; } = true;
    public double ClockSizeScale { get; init; } = 1.0;
    public bool ShowNotchClock { get; init; } = true;
    public double NeonIntensity { get; init; } = 0.6; // 0.0 to 1.0 (default 60%)
    public bool BottomNeonLine { get; init; } = true;
    public bool NeonMusicReactive { get; init; } = true;
    public bool NeonLoadReactive { get; init; } = true;
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
    public bool LayoutDebug { get; init; } = false;
    public bool MatchTypographyToClock { get; init; } = true;
    public bool MatchAccentToClock { get; init; } = true;
    public string TypographyPreset { get; init; } = "Classic";
    public double TypographySizeScale { get; init; } = 1.0;
    public string TypographyWeight { get; init; } = "Regular";
    public bool TypographyUppercaseLabels { get; init; } = false;
    public bool TypographyTabularNumerals { get; init; } = true;
    public bool TypographyTextGlow { get; init; } = false;
    public double TypographyGlowStrength { get; init; } = 0.5;
    public bool LegibilityGuard { get; init; } = true;
    public string? FontFamilyDisplay { get; init; }
    public string? FontFamilyHeading { get; init; }
    public string? FontFamilyBody { get; init; }
    public string? FontFamilyLabel { get; init; }
    public string? FontFamilyNumeric { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalSettings { get; init; }

    public AppSettings Normalize() => this with
    {
        Theme = string.IsNullOrWhiteSpace(Theme) ? "Dark" : Theme.Trim(),
        InterfaceSize = InterfaceSize is "Compact" or "Comfortable" or "Large" or "Auto" ? InterfaceSize : "Auto",
        UiScale = double.IsFinite(UiScale) ? Math.Clamp(UiScale, 0.9, 1.5) : 1.0,
        SurfaceStyle = SurfaceStyle is "Translucent" or "Clear" ? SurfaceStyle : "Opaque",
        BackgroundTransparency = double.IsFinite(BackgroundTransparency) ? Math.Clamp(BackgroundTransparency, 0.0, 70.0) : 12.0,
        CollapsedTransparency = double.IsFinite(CollapsedTransparency) ? Math.Clamp(CollapsedTransparency, 0.0, 50.0) : 0.0,
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
        AlertDurationSeconds = Math.Clamp(AlertDurationSeconds, 1, 15),
        ClockStyle = ClockStyle is "Simple" or "WithDate" or "Stacked" or "DotMatrix" or "Segment" or "Neon" or "Serif" or "Wide" or "Condensed" or "Pixel" or "Compact" or "Terminal" or "Gradient" or "Outline" ? ClockStyle : "Simple",
        ClockColor = ClockColor is "White" or "Amber" or "Cyan" or "Pink" or "Green" or "Violet" or "Gradient" or "Cover" or "Custom" ? ClockColor : "White",
        ClockCustomColor = string.IsNullOrWhiteSpace(ClockCustomColor) ? "#00E5FF" : ClockCustomColor.Trim(),
        ClockSizeScale = double.IsFinite(ClockSizeScale) ? Math.Clamp(ClockSizeScale, 0.8, 1.5) : 1.0,
        NeonIntensity = double.IsFinite(NeonIntensity) ? Math.Clamp(NeonIntensity, 0.0, 1.0) : 0.6,
        TypographyPreset = string.IsNullOrWhiteSpace(TypographyPreset) ? "Classic" : TypographyPreset.Trim(),
        TypographySizeScale = double.IsFinite(TypographySizeScale) ? Math.Clamp(TypographySizeScale, 0.9, 1.3) : 1.0,
        TypographyWeight = TypographyWeight is "Medium" or "Bold" ? TypographyWeight : "Regular",
        TypographyGlowStrength = double.IsFinite(TypographyGlowStrength) ? Math.Clamp(TypographyGlowStrength, 0.0, 1.0) : 0.5
    };
}
