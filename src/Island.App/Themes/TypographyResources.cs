using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Island.Core.Settings;

namespace Island.App.Themes;

public sealed class TypographyResources(ResourceDictionary applicationResources)
{
    private const string BaseFontUri = "pack://application:,,,/Island.App;component/Assets/Fonts/#";

    public static void EnsurePackSchemeRegistered()
    {
        if (!UriParser.IsKnownScheme("pack"))
        {
            _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
        }
    }

    private static FontFamily CreateFamily(string fontNameWithFallback)
    {
        EnsurePackSchemeRegistered();
        return new FontFamily(new Uri("pack://application:,,,/Island.App;component/"), "./Assets/Fonts/#" + fontNameWithFallback);
    }

    // Embedded Font Families with Safe System Fallbacks
    public static readonly FontFamily SegoeFamily = new("Segoe UI Variable Display, Segoe UI, Arial");
    public static readonly FontFamily SegoeTextFamily = new("Segoe UI Variable Text, Segoe UI, Arial");
    public static readonly FontFamily InterFamily = CreateFamily("Inter, Segoe UI Variable Text, Segoe UI");
    public static readonly FontFamily DotoFamily = CreateFamily("Doto, Segoe UI");
    public static readonly FontFamily SilkscreenFamily = CreateFamily("Silkscreen, Segoe UI");
    public static readonly FontFamily SpaceGroteskFamily = CreateFamily("Space Grotesk, Segoe UI Variable Display, Segoe UI");
    public static readonly FontFamily ShareTechMonoFamily = CreateFamily("Share Tech Mono, Consolas, Courier New");
    public static readonly FontFamily RajdhaniFamily = CreateFamily("Rajdhani, Segoe UI");
    public static readonly FontFamily ChakraPetchFamily = CreateFamily("Chakra Petch, Segoe UI");
    public static readonly FontFamily JetBrainsMonoFamily = CreateFamily("JetBrains Mono, Cascadia Code, Consolas");
    public static readonly FontFamily BarlowCondensedFamily = CreateFamily("Barlow Condensed, Bahnschrift, Arial");
    public static readonly FontFamily CaveatFamily = CreateFamily("Caveat, Segoe UI");
    public static readonly FontFamily BebasNeueFamily = CreateFamily("Bebas Neue, Bahnschrift, Arial");
    public static readonly FontFamily FredokaFamily = CreateFamily("Fredoka, Segoe UI");
    public static readonly FontFamily InstrumentSerifFamily = CreateFamily("Instrument Serif, Georgia, Times New Roman");
    public static readonly FontFamily OrbitronFamily = CreateFamily("Orbitron, Segoe UI Variable Display, Segoe UI");
    public static readonly FontFamily UnboundedFamily = CreateFamily("Unbounded, Segoe UI");

    public record PresetDefinition(
        string Id,
        string DisplayName,
        string Description,
        FontFamily Display,
        FontFamily Heading,
        FontFamily Body,
        FontFamily Label,
        FontFamily Numeric,
        bool HasGlow = false
    );

    public static readonly PresetDefinition[] Presets =
    [
        new("Classic", "Classic", "Segoe UI Variable system typography",
            SegoeFamily, SegoeTextFamily, SegoeTextFamily, SegoeTextFamily, SegoeTextFamily),

        new("Dot Matrix", "Dot Matrix", "Doto matrix numerals paired with Inter",
            DotoFamily, DotoFamily, InterFamily, InterFamily, DotoFamily),

        new("LED", "LED Segment", "Share Tech Mono digital italic feel",
            ShareTechMonoFamily, ShareTechMonoFamily, InterFamily, ShareTechMonoFamily, ShareTechMonoFamily),

        new("Neon Techno", "Neon Techno", "Orbitron sci-fi glow with Rajdhani headings",
            OrbitronFamily, RajdhaniFamily, RajdhaniFamily, RajdhaniFamily, OrbitronFamily, HasGlow: true),

        new("Editorial Serif", "Editorial Serif", "Instrument Serif with clean tabular Inter",
            InstrumentSerifFamily, InstrumentSerifFamily, InterFamily, InterFamily, InterFamily),

        new("Wide Futuristic", "Wide Futuristic", "Unbounded geometric wide display with Chakra Petch",
            UnboundedFamily, UnboundedFamily, ChakraPetchFamily, ChakraPetchFamily, ChakraPetchFamily),

        new("Condensed Impact", "Condensed Impact", "Bebas Neue display with Barlow Condensed",
            BebasNeueFamily, BebasNeueFamily, BarlowCondensedFamily, BarlowCondensedFamily, BarlowCondensedFamily),

        new("Pixel Arcade", "Pixel Arcade", "Silkscreen retro pixel display with Share Tech Mono",
            SilkscreenFamily, SilkscreenFamily, InterFamily, InterFamily, ShareTechMonoFamily),

        new("Terminal Mono", "Terminal Mono", "JetBrains Mono monospace throughout",
            JetBrainsMonoFamily, JetBrainsMonoFamily, JetBrainsMonoFamily, JetBrainsMonoFamily, JetBrainsMonoFamily),

        new("Rounded Soft", "Rounded Soft", "Fredoka rounded friendly curves with Inter",
            FredokaFamily, FredokaFamily, InterFamily, InterFamily, FredokaFamily),

        new("Handwritten Accent", "Handwritten Accent", "Caveat handwritten display with clean body text",
            CaveatFamily, InterFamily, InterFamily, InterFamily, InterFamily),

        new("Retro Wave", "Retro Wave", "Orbitron with Rajdhani retro-synth typography",
            OrbitronFamily, RajdhaniFamily, RajdhaniFamily, RajdhaniFamily, RajdhaniFamily, HasGlow: true)
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
        PresetDefinition preset;

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
        bool guard = settings.LegibilityGuard;

        // Resolve 5 roles with Legibility Guard
        FontFamily display = ResolveFamily(settings.FontFamilyDisplay, preset.Display);
        FontFamily heading = ResolveFamily(settings.FontFamilyHeading, preset.Heading);
        FontFamily body = ResolveFamily(settings.FontFamilyBody, preset.Body);
        FontFamily label = ResolveFamily(settings.FontFamilyLabel, preset.Label);
        FontFamily numeric = ResolveFamily(settings.FontFamilyNumeric, preset.Numeric);

        if (guard)
        {
            // Decorative fonts never used for Body or small Label
            if (IsDecorative(body)) body = InterFamily;
            if (IsDecorative(label) && (12.5 * scale) < 15.0) label = InterFamily;
            if (IsDecorative(heading) && (16.0 * scale) < 15.0) heading = InterFamily;
            if (IsDecorative(numeric) && (17.0 * scale) < 14.0) numeric = InterFamily;
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

            // Legacy & Direct Aliases
            ["Font.FamilyDisplay"] = display,
            ["Font.FamilyText"] = body,
            ["Font.FamilyMono"] = numeric,
            ["Font.Display"] = display,
            ["Font.Text"] = body,

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

    private static PresetDefinition ResolveClockPreset(string clockStyle) => clockStyle switch
    {
        "Stacked" => new("Stacked", "Stacked Grotesk", "Space Grotesk with Inter",
            SpaceGroteskFamily, SpaceGroteskFamily, InterFamily, InterFamily, SpaceGroteskFamily),

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
            SpaceGroteskFamily, SpaceGroteskFamily, InterFamily, InterFamily, SpaceGroteskFamily),

        _ => Presets.First(p => p.Id == "Classic") // Simple / WithDate / Default
    };

    private static FontFamily ResolveFamily(string? custom, FontFamily fallback)
    {
        if (string.IsNullOrWhiteSpace(custom)) return fallback;
        try { return new FontFamily(custom); }
        catch { return fallback; }
    }

    public static bool IsDecorative(FontFamily family)
    {
        string source = family.Source ?? "";
        return source.Contains("Doto", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Silkscreen", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Bebas", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Caveat", StringComparison.OrdinalIgnoreCase) ||
               source.Contains("Share Tech", StringComparison.OrdinalIgnoreCase);
    }
}
