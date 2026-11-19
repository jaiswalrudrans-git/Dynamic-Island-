using System.Text.Json;

namespace Island.Core.Themes;

public sealed record ThemePalette(
    string Background,
    string Surface,
    string Foreground,
    string Accent,
    string? SurfaceRaised = null,
    string? SurfaceHover = null,
    string? Border = null,
    string? BorderStrong = null,
    string? TextSecondary = null,
    string? TextTertiary = null,
    string? AccentSoft = null,
    string? Success = null,
    string? Warning = null,
    string? Danger = null,
    string? Shadow = null)
{
    internal bool IsValid => IsColor(Background) && IsColor(Surface) && IsColor(Foreground) && IsColor(Accent);

    public string EffectiveSurfaceRaised => SurfaceRaised ?? Surface;
    public string EffectiveSurfaceHover => SurfaceHover ?? "#222D3D";
    public string EffectiveBorder => Border ?? "#202836";
    public string EffectiveBorderStrong => BorderStrong ?? "#2D3748";
    public string EffectiveTextSecondary => TextSecondary ?? "#8E9DAE";
    public string EffectiveTextTertiary => TextTertiary ?? "#5E6D7E";
    public string EffectiveAccentSoft => AccentSoft ?? "#2A" + Accent.TrimStart('#');
    public string EffectiveSuccess => Success ?? "#4CAF50";
    public string EffectiveWarning => Warning ?? "#FFA726";
    public string EffectiveDanger => Danger ?? "#FF5252";
    public string EffectiveShadow => Shadow ?? "#80000000";

    private static bool IsColor(string? value) =>
        value is { Length: 7 } && value[0] == '#' && value[1..].All(Uri.IsHexDigit);
}

public sealed class ThemeCatalog
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly Dictionary<string, ThemePalette> builtIns;
    private readonly string customPath;
    private readonly Action<string> log;

    public ThemeCatalog(string directory, Action<string> log)
    {
        customPath = Path.Combine(directory, "themes", "custom.json");
        this.log = log;
        using var stream = typeof(ThemeCatalog).Assembly.GetManifestResourceStream("Island.Core.Themes.Palettes.json")
            ?? throw new InvalidDataException("Missing bundled theme palettes.");
        var palettes = JsonSerializer.Deserialize<Dictionary<string, ThemePalette>>(stream, Json)!;
        if (!palettes.ContainsKey("Dark") || palettes.Values.Any(p => p?.IsValid != true))
            throw new InvalidDataException("Invalid bundled theme palette.");
        builtIns = new(palettes, StringComparer.OrdinalIgnoreCase);
    }

    public ThemePalette Resolve(string name)
    {
        if (builtIns.TryGetValue(name, out var palette)) return palette;

        if (string.Equals(name, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var file = File.OpenRead(customPath);
                var custom = JsonSerializer.Deserialize<ThemePalette>(file, Json);
                if (custom?.IsValid == true) return custom;
                throw new JsonException("Every theme color must be #RRGGBB.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                log($"Custom theme unavailable. Using Dark. {ex.Message}");
            }
        }
        else
        {
            log("Unknown theme. Using Dark.");
        }
        return builtIns["Dark"];
    }
}
