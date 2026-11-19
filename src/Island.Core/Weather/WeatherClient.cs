using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Island.Core.Weather;

public sealed class WeatherClient : IDisposable
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string cachePath;
    private readonly Action<string>? log;
    private bool disposed;

    public WeatherClient(string directory, Action<string>? log = null)
    {
        this.log = log;
        cachePath = Path.Combine(directory, "weather_cache.json");
    }

    public async Task<WeatherData?> GetForecastAsync(double latitude, double longitude, string cityName,
        string units = "Celsius", CancellationToken ct = default)
    {
        bool useFahrenheit = units.Equals("Fahrenheit", StringComparison.OrdinalIgnoreCase);
        string tempUnitParam = useFahrenheit ? "&temperature_unit=fahrenheit" : "";
        string windUnitParam = useFahrenheit ? "&wind_speed_unit=mph" : "";

        string url = string.Format(CultureInfo.InvariantCulture,
            "https://api.open-meteo.com/v1/forecast?latitude={0:0.0000}&longitude={1:0.0000}&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,weather_code,wind_speed_10m&hourly=temperature_2m,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto{2}{3}",
            latitude, longitude, tempUnitParam, windUnitParam);

        try
        {
            string json = await HttpClient.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tempUnit = useFahrenheit ? "°F" : "°C";

            // Parse Current
            var curElem = root.GetProperty("current");
            double temp = curElem.GetProperty("temperature_2m").GetDouble();
            double feelsLike = curElem.GetProperty("apparent_temperature").GetDouble();
            int humidity = curElem.GetProperty("relative_humidity_2m").GetInt32();
            int code = curElem.GetProperty("weather_code").GetInt32();
            bool isDay = curElem.GetProperty("is_day").GetInt32() == 1;
            double wind = curElem.GetProperty("wind_speed_10m").GetDouble();

            var (condText, glyph) = WmoWeatherCode.Describe(code, isDay);
            var current = new CurrentWeather(temp, feelsLike, humidity, wind, code, isDay, condText, glyph, tempUnit);

            // Parse Hourly (take next 12 hours)
            var hourlyList = new List<HourlyForecastItem>();
            if (root.TryGetProperty("hourly", out var hourlyElem))
            {
                var times = hourlyElem.GetProperty("time");
                var temps = hourlyElem.GetProperty("temperature_2m");
                var codes = hourlyElem.GetProperty("weather_code");

                int count = Math.Min(times.GetArrayLength(), Math.Min(temps.GetArrayLength(), codes.GetArrayLength()));
                var now = DateTime.Now;

                int added = 0;
                for (int i = 0; i < count && added < 8; i++)
                {
                    if (DateTime.TryParse(times[i].GetString(), out var hTime))
                    {
                        if (hTime >= now.AddHours(-1))
                        {
                            double hTemp = temps[i].GetDouble();
                            int hCode = codes[i].GetInt32();
                            var (_, hGlyph) = WmoWeatherCode.Describe(hCode, true);
                            string hourDisplay = added == 0 ? "Now" : hTime.ToString("h tt", CultureInfo.InvariantCulture);

                            hourlyList.Add(new HourlyForecastItem(hTime, hourDisplay, hTemp, hCode, hGlyph, tempUnit));
                            added++;
                        }
                    }
                }
            }

            // Parse Daily (take next 5 days)
            var dailyList = new List<DailyForecastItem>();
            if (root.TryGetProperty("daily", out var dailyElem))
            {
                var dTimes = dailyElem.GetProperty("time");
                var maxTemps = dailyElem.GetProperty("temperature_2m_max");
                var minTemps = dailyElem.GetProperty("temperature_2m_min");
                var dCodes = dailyElem.GetProperty("weather_code");

                int count = Math.Min(dTimes.GetArrayLength(), Math.Min(maxTemps.GetArrayLength(), minTemps.GetArrayLength()));
                var today = DateTime.Today;

                int added = 0;
                for (int i = 0; i < count && added < 5; i++)
                {
                    if (DateTime.TryParse(dTimes[i].GetString(), out var dDate))
                    {
                        double max = maxTemps[i].GetDouble();
                        double min = minTemps[i].GetDouble();
                        int dCode = dCodes[i].GetInt32();
                        var (_, dGlyph) = WmoWeatherCode.Describe(dCode, true);

                        string dayDisplay = dDate.Date == today ? "Today"
                            : dDate.Date == today.AddDays(1) ? "Tomorrow"
                            : dDate.ToString("ddd", CultureInfo.InvariantCulture);

                        dailyList.Add(new DailyForecastItem(dDate, dayDisplay, max, min, dCode, dGlyph, tempUnit));
                        added++;
                    }
                }
            }

            var weatherData = new WeatherData(cityName, latitude, longitude, current, hourlyList, dailyList, DateTime.Now);
            SaveCache(weatherData);
            return weatherData;
        }
        catch (Exception ex)
        {
            log?.Invoke($"Weather fetch error: {ex.Message}");
            return GetCachedForecast();
        }
    }

    public async Task<(double Lat, double Lon, string City)?> SearchCityAsync(string cityName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return null;
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName.Trim())}&count=1&language=en&format=json";

        try
        {
            string json = await HttpClient.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
            {
                var first = results[0];
                double lat = first.GetProperty("latitude").GetDouble();
                double lon = first.GetProperty("longitude").GetDouble();
                string name = first.GetProperty("name").GetString() ?? cityName;
                if (first.TryGetProperty("admin1", out var admin) && !string.IsNullOrWhiteSpace(admin.GetString()))
                {
                    name += $", {admin.GetString()}";
                }
                return (lat, lon, name);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"Geocoding error: {ex.Message}");
        }

        return null;
    }

    public async Task<(double Lat, double Lon, string City)?> AutoDetectLocationAsync(CancellationToken ct = default)
    {
        // Try IP Geolocation API 1
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3.5));
            string json = await HttpClient.GetStringAsync("http://ip-api.com/json/", cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("status", out var status) && status.GetString() == "success")
            {
                double lat = root.GetProperty("lat").GetDouble();
                double lon = root.GetProperty("lon").GetDouble();
                string city = root.GetProperty("city").GetString() ?? "Local City";
                if (root.TryGetProperty("countryCode", out var cc) && !string.IsNullOrWhiteSpace(cc.GetString()))
                {
                    city = $"{city}, {cc.GetString()}";
                }
                return (lat, lon, city);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"IP-API geolocation failed: {ex.Message}");
        }

        // Try IP Geolocation API 2 (Fallback)
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3.5));
            string json = await HttpClient.GetStringAsync("https://ipapi.co/json/", cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("latitude", out var latProp) && root.TryGetProperty("longitude", out var lonProp))
            {
                double lat = latProp.GetDouble();
                double lon = lonProp.GetDouble();
                string city = root.TryGetProperty("city", out var cp) ? cp.GetString() ?? "Local City" : "Local City";
                if (root.TryGetProperty("country_code", out var cc) && !string.IsNullOrWhiteSpace(cc.GetString()))
                {
                    city = $"{city}, {cc.GetString()}";
                }
                return (lat, lon, city);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"ipapi.co geolocation failed: {ex.Message}");
        }

        return null;
    }

    public WeatherData? GetCachedForecast()
    {
        if (!File.Exists(cachePath)) return null;
        try
        {
            string json = File.ReadAllText(cachePath);
            return JsonSerializer.Deserialize<WeatherData>(json);
        }
        catch
        {
            return null;
        }
    }

    private void SaveCache(WeatherData data)
    {
        try
        {
            string dir = Path.GetDirectoryName(cachePath)!;
            Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(cachePath, json);
        }
        catch { }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
    }
}
