namespace Island.Core.Weather;

public static class WmoWeatherCode
{
    public static (string Condition, string Glyph) Describe(int code, bool isDay = true)
    {
        return code switch
        {
            0 => isDay ? ("Clear Sky", "\uE706") : ("Clear Night", "\uE708"),
            1 => isDay ? ("Mainly Clear", "\uE707") : ("Mainly Clear", "\uE708"),
            2 => ("Partly Cloudy", "\uE707"),
            3 => ("Overcast", "\uE753"),
            45 or 48 => ("Fog", "\uE774"),
            51 or 53 or 55 => ("Drizzle", "\uE77A"),
            56 or 57 => ("Freezing Drizzle", "\uE77C"),
            61 => ("Slight Rain", "\uE77A"),
            63 => ("Moderate Rain", "\uE77A"),
            65 => ("Heavy Rain", "\uE77B"),
            66 or 67 => ("Freezing Rain", "\uE77C"),
            71 or 73 or 75 => ("Snow Fall", "\uE77C"),
            77 => ("Snow Grains", "\uE77C"),
            80 or 81 => ("Rain Showers", "\uE77A"),
            82 => ("Violent Showers", "\uE77B"),
            85 or 86 => ("Snow Showers", "\uE77C"),
            95 => ("Thunderstorm", "\uE776"),
            96 or 99 => ("Thunderstorm with Hail", "\uE776"),
            _ => ("Cloudy", "\uE753")
        };
    }
}
