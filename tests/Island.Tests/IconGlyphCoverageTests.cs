using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;
using Xunit;

namespace Island.Tests;

public sealed class IconGlyphCoverageTests
{
    [Fact]
    public void EveryUIIconGlyphExistsInIconFont()
    {
        var fluentFont = new FontFamily("Segoe Fluent Icons");
        var mdl2Font = new FontFamily("Segoe MDL2 Assets");

        bool hasFluentGtf = fluentFont.GetTypefaces().First().TryGetGlyphTypeface(out var fluentGtf);
        bool hasMdl2Gtf = mdl2Font.GetTypefaces().First().TryGetGlyphTypeface(out var mdl2Gtf);
        Assert.True(hasFluentGtf || hasMdl2Gtf, "Either Segoe Fluent Icons or Segoe MDL2 Assets must be present on the system");

        // Canonical set of UI icon glyphs across Header, Navigation, Focus, TopApps, System, Tasks, Media
        char[] uiGlyphs =
        [
            '\uE8D6', // Music
            '\uE950', // System
            '\uE9D5', // Tasks
            '\uE9E9', // Quick Controls
            '\uE916', // Focus
            '\uE787', // Calendar
            '\uE8C8', // Clipboard
            '\uE718', // Pin
            '\uE713', // Settings
            '\uE777', // Focus Reset
            '\uE768', // Focus Play
            '\uE769', // Focus Pause
            '\uE893', // Focus Skip
            '\uE7C0', // Focus Lap
            '\uE71D', // Generic Top Apps process
            '\uE7B8', // Collapsed RAM
            '\uE839'  // Collapsed Network
        ];

        foreach (var c in uiGlyphs)
        {
            int code = c;
            bool exists = (fluentGtf != null && fluentGtf.CharacterToGlyphMap.ContainsKey(code)) ||
                          (mdl2Gtf != null && mdl2Gtf.CharacterToGlyphMap.ContainsKey(code));
            Assert.True(exists, $"Glyph U+{(int)c:X4} must exist in the icon typeface");
        }
    }

    [Fact]
    public void NoElementUsingIconGlyphHasDecorativeFontFamily()
    {
        string? projectRoot = FindProjectRoot();
        Assert.NotNull(projectRoot);

        string viewsDir = Path.Combine(projectRoot, "src", "Island.App", "Views");
        Assert.True(Directory.Exists(viewsDir));

        var xamlFiles = Directory.GetFiles(viewsDir, "*.xaml", SearchOption.AllDirectories);
        string[] decorativeNames = ["Doto", "Orbitron", "DotMatrix", "Typography.Family.Display", "Typography.Family.Heading"];

        foreach (var file in xamlFiles)
        {
            string content = File.ReadAllText(file);
            // Parse for Text attributes that contain Unicode Private Use Area glyphs (\uE000 - \uF8FF) or entity representations
            var doc = XDocument.Parse(content);
            foreach (var el in doc.Descendants())
            {
                var textAttr = el.Attribute("Text");
                if (textAttr != null && (textAttr.Value.StartsWith("&#xE") || (textAttr.Value.Length > 0 && textAttr.Value[0] >= '\uE000' && textAttr.Value[0] <= '\uF8FF')))
                {
                    var fontAttr = el.Attribute("FontFamily");
                    if (fontAttr != null)
                    {
                        foreach (var dec in decorativeNames)
                        {
                            Assert.False(fontAttr.Value.Contains(dec),
                                $"Icon element with text '{textAttr.Value}' in {Path.GetFileName(file)} must not use decorative font '{dec}'");
                        }
                    }
                }
            }
        }
    }

    private static string? FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Island.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
