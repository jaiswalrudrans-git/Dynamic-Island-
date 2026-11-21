using System;
using System.Collections.Concurrent;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Island.App.Design.Typography;

public enum FontRole
{
    Display,
    Heading,
    Numeric,
    Body,
    Label,
    UI
}

public enum ZoneRole
{
    // Fancy zones
    FocusTimerDigits,
    WeatherCurrentTemp,
    CollapsedNotchTime,
    HeaderClock,
    TabTitleLarge,
    SystemBigStatCpu,
    SystemBigStatGpu,
    SystemBigStatMemory,
    SystemBigStatPower,
    SystemBigStatNetwork,
    VolumeBrightnessLabel,
    TaskCounter,
    MusicTrackTitle,
    SystemSectionTitleLarge,

    // Default zones
    TabIcons,
    Tooltips,
    ButtonsAndLabels,
    Chips,
    SegmentedControls,
    ToggleTileNames,
    SearchBoxes,
    MenusAndContextMenus,
    ScrollbarsText,
    AlertsAndOsdText,
    ListRows,
    UnitsAndSecondaryText,
    Lyrics,
    ArtistName,
    SectionSubtitles,
    MutedSecondaryText,
    HeaderDateLine,
    MediaTimeLabels,
    SettingsWindow,
    PresetTileCaptions
}

public sealed record TypographyPreset(
    string Id,
    string DisplayName,
    string Description,
    FontFamily Display,
    FontFamily Heading,
    FontFamily Numeric,
    FontFamily Body,
    FontFamily Label,
    FontFamily UI,
    FontFamily Companion,
    bool HasGlow = false
);

public static class TypographyResolver
{
    private static readonly ConcurrentDictionary<string, bool> CoverageCache = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryGetGlyphTypeface(FontFamily family, out GlyphTypeface? glyphTypeface)
    {
        glyphTypeface = null;
        try
        {
            var typeface = new Typeface(family, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            if (typeface.TryGetGlyphTypeface(out glyphTypeface) && glyphTypeface != null)
            {
                return true;
            }

            foreach (var tf in family.GetTypefaces())
            {
                if (tf.TryGetGlyphTypeface(out glyphTypeface) && glyphTypeface != null)
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore resolution errors
        }

        return false;
    }

    public static bool CheckCoverage(FontFamily family, string requiredChars)
    {
        if (string.IsNullOrEmpty(requiredChars)) return true;

        string key = $"{family.Source ?? family.ToString()}::{requiredChars}";
        if (CoverageCache.TryGetValue(key, out bool cached)) return cached;

        bool covered = false;
        if (TryGetGlyphTypeface(family, out var glyphTypeface) && glyphTypeface != null)
        {
            covered = true;
            var map = glyphTypeface.CharacterToGlyphMap;
            for (int i = 0; i < requiredChars.Length; i++)
            {
                int codepoint = char.ConvertToUtf32(requiredChars, i);
                if (char.IsSurrogatePair(requiredChars, i)) i++;

                if (!map.ContainsKey(codepoint))
                {
                    covered = false;
                    break;
                }
            }
        }
        else
        {
            covered = !IsDecorative(family);
        }

        CoverageCache[key] = covered;
        return covered;
    }

    public static bool IsDecorative(FontFamily family)
    {
        string source = family.Source ?? family.ToString();
        return source.Contains("Doto", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Silkscreen", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Bebas", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Caveat", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Audiowide", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Syncopate", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Michroma", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("DotGothic", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("DSEG", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Pixelify", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsDotOrPixelFont(FontFamily family)
    {
        string source = family.Source ?? family.ToString();
        return source.Contains("Doto", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Silkscreen", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Pixelify", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("DotGothic", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("DSEG", StringComparison.OrdinalIgnoreCase);
    }

    public static double GetMinimumSize(FontFamily family)
    {
        return IsDotOrPixelFont(family) ? 18.0 : 16.0;
    }

    public static FontFamily ResolveElementFont(
        FontRole role,
        TypographyPreset preset,
        double renderedSize,
        string? text = null,
        bool legibilityGuardEnabled = true)
    {
        if (role is FontRole.Body or FontRole.Label or FontRole.UI)
        {
            return TypographyResources.SegoeTextFamily;
        }

        FontFamily candidate = role switch
        {
            FontRole.Display => preset.Display,
            FontRole.Heading => preset.Heading,
            FontRole.Numeric => preset.Numeric,
            _ => TypographyResources.SegoeTextFamily
        };

        if (!legibilityGuardEnabled)
        {
            return candidate;
        }

        if (IsDecorative(candidate))
        {
            double minSize = GetMinimumSize(candidate);
            if (renderedSize < minSize)
            {
                return preset.Companion ?? TypographyResources.SegoeTextFamily;
            }

            if (!string.IsNullOrEmpty(text) && !CheckCoverage(candidate, text))
            {
                return preset.Companion ?? TypographyResources.SegoeTextFamily;
            }
        }

        return candidate;
    }

    public static FontFamily ResolveZoneFont(
        ZoneRole zone,
        TypographyPreset preset,
        double renderedSize = 16.0,
        string? text = null,
        bool legibilityGuardEnabled = true)
    {
        return zone switch
        {
            ZoneRole.FocusTimerDigits => ResolveElementFont(FontRole.Display, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.WeatherCurrentTemp => ResolveElementFont(FontRole.Display, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.CollapsedNotchTime => ResolveElementFont(FontRole.Display, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.HeaderClock => ResolveElementFont(FontRole.Display, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.TabTitleLarge when renderedSize >= 18.0 => ResolveElementFont(FontRole.Display, preset, renderedSize, text, legibilityGuardEnabled),

            ZoneRole.SystemBigStatCpu => ResolveElementFont(FontRole.Numeric, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.SystemBigStatGpu => ResolveElementFont(FontRole.Numeric, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.SystemBigStatMemory => ResolveElementFont(FontRole.Numeric, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.SystemBigStatPower => ResolveElementFont(FontRole.Numeric, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.SystemBigStatNetwork => ResolveElementFont(FontRole.Numeric, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.VolumeBrightnessLabel => ResolveElementFont(FontRole.Numeric, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.TaskCounter => ResolveElementFont(FontRole.Numeric, preset, renderedSize, text, legibilityGuardEnabled),

            ZoneRole.MusicTrackTitle => ResolveElementFont(FontRole.Heading, preset, renderedSize, text, legibilityGuardEnabled),
            ZoneRole.SystemSectionTitleLarge when renderedSize >= 16.0 => ResolveElementFont(FontRole.Heading, preset, renderedSize, text, legibilityGuardEnabled),

            _ => TypographyResources.SegoeTextFamily
        };
    }

    public static string GenerateDiagnosticReport(TypographyPreset activePreset, TypographyPreset[] allPresets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Island Typography Diagnostic Report ===");
        sb.AppendLine($"Active Preset: {activePreset.DisplayName} ({activePreset.Id})");
        sb.AppendLine("-------------------------------------------");

        const string testChars = "0123456789: APMCPUNow Playing";

        foreach (var preset in allPresets)
        {
            sb.AppendLine($"[Preset: {preset.DisplayName}]");
            var roles = new (FontRole Role, FontFamily Family)[]
            {
                (FontRole.Display, preset.Display),
                (FontRole.Heading, preset.Heading),
                (FontRole.Numeric, preset.Numeric),
                (FontRole.Body, preset.Body),
                (FontRole.Label, preset.Label),
                (FontRole.UI, preset.UI)
            };

            foreach (var (role, fam) in roles)
            {
                bool loaded = TryGetGlyphTypeface(fam, out _);
                bool covered = CheckCoverage(fam, testChars);
                string famSource = fam.Source ?? fam.ToString();
                sb.AppendLine($"  - {role,-7}: {famSource,-40} | Loaded: {loaded,-5} | Covered: {covered}");
            }
        }

        sb.AppendLine("===========================================");
        return sb.ToString();
    }

    public static string GenerateZoneMapReport(TypographyPreset preset)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Zone Map Resolution for Preset '{preset.DisplayName}' ===");
        foreach (ZoneRole zone in Enum.GetValues<ZoneRole>())
        {
            var fam = ResolveZoneFont(zone, preset, renderedSize: 20.0, text: "14:32 CPU 29%");
            string isDefault = fam == TypographyResources.SegoeTextFamily ? "[DEFAULT: Segoe]" : "[FANCY]";
            sb.AppendLine($"  Zone: {zone,-26} -> {isDefault} {fam.Source ?? fam.ToString()}");
        }
        return sb.ToString();
    }
}
