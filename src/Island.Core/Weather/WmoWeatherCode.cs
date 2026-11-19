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
            51 or 53 or 55 => ("Drizzle", "\uEB42"),
            56 or 57 => ("Freezing Drizzle", "\uEB42"),
            61 => ("Slight Rain", "\uEB42"),
            63 => ("Moderate Rain", "\uEB42"),
            65 => ("Heavy Rain", "\uEB42"),
            66 or 67 => ("Freezing Rain", "\uEB42"),
            71 or 73 or 75 => ("Snow Fall", "\u2744"),
            77 => ("Snow Grains", "\u2744"),
            80 or 81 => ("Rain Showers", "\uEB42"),
            82 => ("Violent Showers", "\uEB42"),
            85 or 86 => ("Snow Showers", "\u2744"),
            95 => ("Thunderstorm", "\uE945"),
            96 or 99 => ("Thunderstorm with Hail", "\uE945"),
            _ => ("Cloudy", "\uE753")
        };
    }
}
