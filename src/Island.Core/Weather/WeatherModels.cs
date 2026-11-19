namespace Island.Core.Weather;

public sealed record CurrentWeather(
    double Temperature,
    double ApparentTemperature,
    int RelativeHumidity,
    double WindSpeed,
    int WeatherCode,
    bool IsDay,
    string ConditionText,
    string Glyph,
    string TemperatureUnit = "°C"
)
{
    public string FormattedTemperature => $"{Math.Round(Temperature):0}{TemperatureUnit}";
    public string FormattedFeelsLike => $"Feels like {Math.Round(ApparentTemperature):0}{TemperatureUnit}";
    public string FormattedWind => $"{Math.Round(WindSpeed):0} km/h";
    public string FormattedHumidity => $"{RelativeHumidity}%";
}

public sealed record HourlyForecastItem(
    DateTime Time,
    string HourDisplay,
    double Temperature,
    int WeatherCode,
    string Glyph,
    string TemperatureUnit = "°C"
)
{
    public string FormattedTemperature => $"{Math.Round(Temperature):0}°";
}

public sealed record DailyForecastItem(
    DateTime Date,
    string DayDisplay,
    double MaxTemp,
    double MinTemp,
    int WeatherCode,
    string Glyph,
    string TemperatureUnit = "°C"
)
{
    public string FormattedHighLow => $"{Math.Round(MaxTemp):0}° / {Math.Round(MinTemp):0}°";
}

public sealed record WeatherData(
    string City,
    double Latitude,
    double Longitude,
    CurrentWeather Current,
    IReadOnlyList<HourlyForecastItem> Hourly,
    IReadOnlyList<DailyForecastItem> Daily,
    DateTime FetchedAt
);
