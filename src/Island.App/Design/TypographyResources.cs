using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Island.App.Design.Typography;
using Island.Core.Settings;

namespace Island.App.Design;

public sealed class TypographyResources(ResourceDictionary applicationResources)
{
    private static readonly Uri FontBaseUri;

    static TypographyResources()
    {
        EnsurePackSchemeRegistered();
        FontBaseUri = new Uri("pack://application:,,,/Island.App;component/Assets/Fonts/");
    }

    public static void EnsurePackSchemeRegistered()
    {
        if (!UriParser.IsKnownScheme("pack"))
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
        }

        try
        {
            if (Application.Current != null && Application.ResourceAssembly == null)
            {
                Application.ResourceAssembly = typeof(TypographyResources).Assembly;
            }
        }
        catch
        {
            // Ignore in test or detached runner contexts
        }
    }

    public static FontFamily CreateFamily(string fontName)
    {
        EnsurePackSchemeRegistered();
        return new FontFamily(FontBaseUri, $"./#{fontName}, Segoe UI Variable Display, Segoe UI, Segoe UI Symbol, Segoe UI Emoji, Arial");
    }

    // Embedded Font Families with Safe System Fallbacks ending in Segoe UI Variable, Symbol & Arial
    public static readonly FontFamily SegoeFamily = new("Segoe UI Variable Display, Segoe UI, Segoe UI Symbol, Arial");
    public static readonly FontFamily SegoeTextFamily = new("Segoe UI Variable Text, Segoe UI, Segoe UI Symbol, Arial");
    public static readonly FontFamily InterFamily = CreateFamily("Inter");
    public static readonly FontFamily DotoFamily = CreateFamily("Doto");
    public static readonly FontFamily SilkscreenFamily = CreateFamily("Silkscreen");
    public static readonly FontFamily SpaceGroteskFamily = CreateFamily("Space Grotesk");
    public static readonly FontFamily ShareTechMonoFamily = CreateFamily("Share Tech Mono");
    public static readonly FontFamily RajdhaniFamily = CreateFamily("Rajdhani");
    public static readonly FontFamily ChakraPetchFamily = CreateFamily("Chakra Petch");
    public static readonly FontFamily JetBrainsMonoFamily = CreateFamily("JetBrains Mono");
    public static readonly FontFamily BarlowCondensedFamily = CreateFamily("Barlow Condensed");
    public static readonly FontFamily CaveatFamily = CreateFamily("Caveat");
    public static readonly FontFamily BebasNeueFamily = CreateFamily("Bebas Neue");
    public static readonly FontFamily FredokaFamily = CreateFamily("Fredoka");
    public static readonly FontFamily InstrumentSerifFamily = CreateFamily("Instrument Serif");
    public static readonly FontFamily OrbitronFamily = CreateFamily("Orbitron");
    public static readonly FontFamily UnboundedFamily = CreateFamily("Unbounded");

    // The 12 Restored Typography Presets with Exact Zone Map Pairings
    public static readonly TypographyPreset[] Presets =
    [
        // 1. Classic: Segoe UI Variable for all roles.
        new("Classic", "Classic", "Segoe UI Variable for all roles",
            Display: SegoeFamily,
            Heading: SegoeTextFamily,
            Numeric: SegoeTextFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: SegoeTextFamily),

        // 2. Dot Matrix: Display/Heading/Numeric = Doto; companion Geist or Inter.
        new("Dot Matrix", "Dot Matrix", "Doto dot-matrix numerals and headings with Inter companion",
            Display: DotoFamily,
            Heading: DotoFamily,
            Numeric: DotoFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: InterFamily),

        // 3. LED Segment: Display/Numeric = DSEG14 Classic Italic (Share Tech Mono); Heading = Share Tech Mono; companion Inter.
        new("LED", "LED Segment", "Share Tech Mono digital italic numerals with Inter companion",
            Display: ShareTechMonoFamily,
            Heading: ShareTechMonoFamily,
            Numeric: ShareTechMonoFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: InterFamily),

        // 4. Neon Techno: Display/Numeric = Orbitron (or Audiowide); Heading = Rajdhani; companion Rajdhani.
        new("Neon Techno", "Neon Techno", "Orbitron sci-fi display and numeric glow with Rajdhani headings",
            Display: OrbitronFamily,
            Heading: RajdhaniFamily,
            Numeric: OrbitronFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: RajdhaniFamily,
            HasGlow: true),

        // 5. Editorial Serif: Display/Heading = Playfair Display (Instrument Serif); Numeric = Inter tabular; companion Inter.
        new("Editorial Serif", "Editorial Serif", "Instrument Serif display with clean tabular Inter",
            Display: InstrumentSerifFamily,
            Heading: InstrumentSerifFamily,
            Numeric: InterFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: InterFamily),

        // 6. Wide Futuristic: Display/Heading = Syncopate (Unbounded); Numeric = Chakra Petch; companion Chakra Petch.
        new("Wide Futuristic", "Wide Futuristic", "Unbounded wide display with Chakra Petch",
            Display: UnboundedFamily,
            Heading: UnboundedFamily,
            Numeric: ChakraPetchFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: ChakraPetchFamily),

        // 7. Condensed Impact: Display/Heading = Bebas Neue; Numeric = Barlow Condensed; companion Barlow Condensed.
        new("Condensed Impact", "Condensed Impact", "Bebas Neue display with Barlow Condensed",
            Display: BebasNeueFamily,
            Heading: BebasNeueFamily,
            Numeric: BarlowCondensedFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: BarlowCondensedFamily),

        // 8. Pixel Arcade: Display/Heading = Silkscreen; Numeric = Space Mono (Share Tech Mono); companion Space Mono.
        new("Pixel Arcade", "Pixel Arcade", "Silkscreen retro pixel display with Share Tech Mono numerals",
            Display: SilkscreenFamily,
            Heading: SilkscreenFamily,
            Numeric: ShareTechMonoFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: ShareTechMonoFamily),

        // 9. Terminal Mono: JetBrains Mono for the three fancy roles; companion JetBrains Mono.
        new("Terminal Mono", "Terminal Mono", "JetBrains Mono monospace typography throughout",
            Display: JetBrainsMonoFamily,
            Heading: JetBrainsMonoFamily,
            Numeric: JetBrainsMonoFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: JetBrainsMonoFamily),

        // 10. Rounded Soft: Nunito (Fredoka) for the three fancy roles; companion Nunito.
        new("Rounded Soft", "Rounded Soft", "Fredoka rounded curves for fancy roles with Inter",
            Display: FredokaFamily,
            Heading: FredokaFamily,
            Numeric: FredokaFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: FredokaFamily),

        // 11. Handwritten Accent: Caveat for Display only; Heading and Numeric = Segoe UI Variable Semibold.
        new("Handwritten Accent", "Handwritten Accent", "Caveat handwritten display with Segoe UI Variable",
            Display: CaveatFamily,
            Heading: SegoeTextFamily,
            Numeric: SegoeTextFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: SegoeTextFamily),

        // 12. Retro Wave: Audiowide (Rajdhani) for Display/Heading; Numeric = Rajdhani; companion Rajdhani.
        new("Retro Wave", "Retro Wave", "Rajdhani retro-synth typography with subtle glow",
            Display: RajdhaniFamily,
            Heading: RajdhaniFamily,
            Numeric: RajdhaniFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: RajdhaniFamily,
            HasGlow: true)
    ];

    private ResourceDictionary? active;
    private string lastPreset = "";
    private double lastScale = -1;
    private string lastWeight = "";
    private bool lastMatchClock = false;
    private string lastClockStyle = "";
    private string lastClockColor = "";

    public void Apply(AppSettings settings, Color? clockResolvedColor = null)
    {
        TypographyPreset preset;

        if (settings.MatchTypographyToClock)
        {
            preset = ResolveClockPreset(settings.ClockStyle);
        }
        else
        {
            preset = Presets.FirstOrDefault(p => string.Equals(p.Id, settings.TypographyPreset, StringComparison.OrdinalIgnoreCase))
                     ?? Presets[0];
        }

        double scale = Math.Clamp(settings.TypographySizeScale, 0.9, 1.3);

        // Resolve 3 fancy roles (overridable in settings) + 3 default roles (ALWAYS Segoe UI Variable)
        FontFamily display = ResolveCustomFamily(settings.FontFamilyDisplay, preset.Display);
        FontFamily heading = ResolveCustomFamily(settings.FontFamilyHeading, preset.Heading);
        FontFamily numeric = ResolveCustomFamily(settings.FontFamilyNumeric, preset.Numeric);
        FontFamily body = SegoeTextFamily;
        FontFamily label = SegoeTextFamily;
        FontFamily ui = SegoeTextFamily;

        // Startup/debug diagnosis log on change
        if (!string.Equals(lastPreset, preset.Id, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                string report = TypographyResolver.GenerateDiagnosticReport(preset, Presets);
                Debug.WriteLine(report);
            }
            catch { }
        }

        // Font Weights
        var (wDisplay, wHeading, wBody, wLabel, wNumeric) = settings.TypographyWeight switch
        {
            "Bold" => (FontWeights.ExtraBold, FontWeights.Bold, FontWeights.SemiBold, FontWeights.Bold, FontWeights.Bold),
            "Medium" => (FontWeights.Bold, FontWeights.SemiBold, FontWeights.Medium, FontWeights.SemiBold, FontWeights.SemiBold),
            _ => (FontWeights.Bold, FontWeights.SemiBold, FontWeights.Normal, FontWeights.Normal, FontWeights.SemiBold)
        };

        // Text Glow Effect
        DropShadowEffect? textGlow = null;
        bool enableGlow = settings.TypographyTextGlow || (preset.HasGlow && settings.NeonIntensity > 0.1);
        if (enableGlow && clockResolvedColor.HasValue)
        {
            double intensity = settings.TypographyGlowStrength * settings.NeonIntensity;
            textGlow = new DropShadowEffect
            {
                Color = clockResolvedColor.Value,
                BlurRadius = 8 * intensity,
                ShadowDepth = 0,
                Opacity = 0.85 * intensity
            };
            textGlow.Freeze();
        }

        var next = new ResourceDictionary
        {
            // Roles: Families
            ["Typography.Family.Display"] = display,
            ["Typography.Family.Heading"] = heading,
            ["Typography.Family.Body"] = body,
            ["Typography.Family.Label"] = label,
            ["Typography.Family.Numeric"] = numeric,
            ["Typography.Family.UI"] = ui,

            // Legacy & Direct Aliases
            ["Font.FamilyDisplay"] = display,
            ["Font.FamilyText"] = body,
            ["Font.FamilyMono"] = numeric,
            ["Font.Display"] = display,
            ["Font.Text"] = body,
            ["Font.Icons"] = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"),

            // Roles: Weights
            ["Typography.Weight.Display"] = wDisplay,
            ["Typography.Weight.Heading"] = wHeading,
            ["Typography.Weight.Body"] = wBody,
            ["Typography.Weight.Label"] = wLabel,
            ["Typography.Weight.Numeric"] = wNumeric,

            // Scaled Sizes
            ["Typography.Size.HeroLarge"] = 56.0 * scale,
            ["Typography.Size.Hero"] = 32.0 * scale,
            ["Typography.Size.HeaderTime"] = 20.0 * scale,
            ["Typography.Size.SectionTitle"] = 16.0 * scale,
            ["Typography.Size.Value"] = 17.0 * scale,
            ["Typography.Size.Body"] = 14.0 * scale,
            ["Typography.Size.Secondary"] = 13.0 * scale,
            ["Typography.Size.Label"] = 12.5 * scale,
            ["Typography.Size.Mini"] = 11.0 * scale,

            // Backward Compatibility Aliases
            ["FontSize.HeroLarge"] = 56.0 * scale,
            ["FontSize.Hero"] = 32.0 * scale,
            ["FontSize.HeaderTime"] = 20.0 * scale,
            ["FontSize.SectionTitle"] = 16.0 * scale,
            ["FontSize.Value"] = 17.0 * scale,
            ["FontSize.Body"] = 14.0 * scale,
            ["FontSize.Secondary"] = 13.0 * scale,
            ["FontSize.Label"] = 12.5 * scale,

            ["Font.HeroLarge"] = 56.0 * scale,
            ["Font.Hero"] = 32.0 * scale,
            ["Font.HeaderTime"] = 20.0 * scale,
            ["Font.HeaderDate"] = 13.0 * scale,
            ["Font.CollapsedClock"] = 16.0 * scale,
            ["Font.Title"] = 16.0 * scale,
            ["Font.SectionTitle"] = 16.0 * scale,
            ["Font.Value"] = 17.0 * scale,
            ["Font.Body"] = 14.0 * scale,
            ["Font.Secondary"] = 13.0 * scale,
            ["Font.Label"] = 12.5 * scale,

            // Glow & Formatting
            ["Typography.TextGlow"] = (object?)textGlow ?? DependencyProperty.UnsetValue,
            ["Typography.UppercaseLabels"] = settings.TypographyUppercaseLabels,
            ["Typography.TabularNumerals"] = settings.TypographyTabularNumerals
        };

        var dictionaries = applicationResources.MergedDictionaries;
        if (active is null)
            dictionaries.Add(next);
        else
            dictionaries[dictionaries.IndexOf(active)] = next;

        active = next;
        lastPreset = preset.Id;
        lastScale = scale;
        lastWeight = settings.TypographyWeight;
        lastMatchClock = settings.MatchTypographyToClock;
        lastClockStyle = settings.ClockStyle;
        lastClockColor = settings.ClockColor;
    }

    public static TypographyPreset ResolveClockPreset(string clockStyle) => clockStyle switch
    {
        "Stacked" => new("Stacked", "Stacked Grotesk", "Space Grotesk with Inter",
            Display: SpaceGroteskFamily,
            Heading: SpaceGroteskFamily,
            Numeric: SpaceGroteskFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: InterFamily),

        "DotMatrix" => Presets.First(p => p.Id == "Dot Matrix"),
        "Segment" => Presets.First(p => p.Id == "LED"),
        "Neon" => Presets.First(p => p.Id == "Neon Techno"),
        "Serif" => Presets.First(p => p.Id == "Editorial Serif"),
        "Wide" => Presets.First(p => p.Id == "Wide Futuristic"),
        "Condensed" => Presets.First(p => p.Id == "Condensed Impact"),
        "Pixel" => Presets.First(p => p.Id == "Pixel Arcade"),
        "Compact" => Presets.First(p => p.Id == "Rounded Soft"),
        "Terminal" => Presets.First(p => p.Id == "Terminal Mono"),

        "Gradient" or "Outline" => new("Gradient", "Modern Outline", "Space Grotesk Display with Inter",
            Display: SpaceGroteskFamily,
            Heading: SpaceGroteskFamily,
            Numeric: SpaceGroteskFamily,
            Body: SegoeTextFamily,
            Label: SegoeTextFamily,
            UI: SegoeTextFamily,
            Companion: InterFamily),

        _ => Presets.First(p => p.Id == "Classic")
    };

    private static FontFamily ResolveCustomFamily(string? custom, FontFamily fallback)
    {
        if (string.IsNullOrWhiteSpace(custom)) return fallback;
        try { return new FontFamily(custom); }
        catch { return fallback; }
    }

    public static bool HasGeneralTextCoverage(FontFamily family)
    {
        return TypographyResolver.CheckCoverage(family, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?'\"-()");
    }

    public static bool CheckCoverage(FontFamily family, string requiredChars)
    {
        return TypographyResolver.CheckCoverage(family, requiredChars);
    }

    public static bool IsDecorative(FontFamily family)
    {
        return TypographyResolver.IsDecorative(family);
    }
}
