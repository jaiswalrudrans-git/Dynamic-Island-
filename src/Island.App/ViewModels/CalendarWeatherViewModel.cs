using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Island.Core.Calendar;
using Island.Core.Weather;

namespace Island.App.ViewModels;

public sealed record CalendarDayUi(
    int DayNumber,
    DateTime Date,
    bool IsToday,
    bool IsSelected,
    bool IsCurrentMonth,
    bool HasEvents,
    Brush ForegroundBrush,
    Brush BackgroundBrush
);

public sealed record CalendarEventUi(
    string Id,
    string Title,
    string TimeText,
    string? Location,
    string Source
);

public sealed class CalendarWeatherViewModel : ObservableObject, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly CalendarStore calendarStore;
    private readonly WeatherClient weatherClient;
    private readonly Func<DateTime> clock;

    private DateTime displayMonth;
    private DateTime selectedDate;
    private string newEventTitle = "";
    private bool isAddingEvent;

    private WeatherData? weatherData;
    private string city = "New York";
    private double latitude = 40.7128;
    private double longitude = -74.0060;
    private string units = "Celsius";
    private bool isLoadingWeather;
    private readonly DispatcherTimer weatherTimer;
    private bool disposed;

    // --- Calendar Properties ---
    public string MonthYearText => displayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public string SelectedDateHeader
    {
        get
        {
            var now = clock().Date;
            if (selectedDate.Date == now) return $"Today, {selectedDate:MMM d}";
            if (selectedDate.Date == now.AddDays(1)) return $"Tomorrow, {selectedDate:MMM d}";
            return selectedDate.ToString("ddd, MMM d", CultureInfo.InvariantCulture);
        }
    }

    public string NewEventTitle
    {
        get => newEventTitle;
        set => SetProperty(ref newEventTitle, value);
    }

    public bool IsAddingEvent
    {
        get => isAddingEvent;
        set => SetProperty(ref isAddingEvent, value);
    }

    public IReadOnlyList<CalendarDayUi> MonthDays => GenerateMonthDays();

    public IReadOnlyList<CalendarEventUi> SelectedDayEvents => GetEventsForSelectedDay();

    public bool HasEventsForSelectedDay => SelectedDayEvents.Count > 0;

    // --- Weather Properties ---
    public bool HasWeather => weatherData != null;
    public bool IsLoadingWeather => isLoadingWeather;

    public string WeatherCity => weatherData?.City ?? city;
    public string CurrentTemp => weatherData?.Current.FormattedTemperature ?? "--";
    public string CurrentFeelsLike => weatherData?.Current.FormattedFeelsLike ?? "";
    public string CurrentCondition => weatherData?.Current.ConditionText ?? "Loading...";
    public string CurrentGlyph => weatherData?.Current.Glyph ?? "\uE706";
    public string CurrentWind => weatherData?.Current.FormattedWind ?? "--";
    public string CurrentHumidity => weatherData?.Current.FormattedHumidity ?? "--";

    public string HighLow
    {
        get
        {
            if (weatherData?.Daily.Count > 0)
            {
                var today = weatherData.Daily[0];
                return $"H: {Math.Round(today.MaxTemp):0}°  L: {Math.Round(today.MinTemp):0}°";
            }
            return "";
        }
    }

    public IReadOnlyList<HourlyForecastItem> HourlyForecast => weatherData?.Hourly ?? Array.Empty<HourlyForecastItem>();
    public IReadOnlyList<DailyForecastItem> DailyForecast => weatherData?.Daily ?? Array.Empty<DailyForecastItem>();

    public CalendarWeatherViewModel(
        Dispatcher dispatcher,
        CalendarStore calendarStore,
        WeatherClient weatherClient,
        Func<DateTime>? clock = null)
    {
        this.dispatcher = dispatcher;
        this.calendarStore = calendarStore;
        this.weatherClient = weatherClient;
        this.clock = clock ?? (() => DateTime.Now);

        var now = this.clock();
        selectedDate = now.Date;
        displayMonth = new DateTime(now.Year, now.Month, 1);

        calendarStore.EventsChanged += OnEventsChanged;

        // Restore cached weather if available
        var cached = weatherClient.GetCachedForecast();
        if (cached != null)
        {
            weatherData = cached;
            city = cached.City;
            latitude = cached.Latitude;
            longitude = cached.Longitude;
        }

        weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        weatherTimer.Tick += (_, _) => _ = RefreshWeatherAsync();
        weatherTimer.Start();

        _ = RefreshWeatherAsync();
    }

    // --- Calendar Methods ---
    public void PreviousMonth()
    {
        displayMonth = displayMonth.AddMonths(-1);
        RefreshCalendar();
    }

    public void NextMonth()
    {
        displayMonth = displayMonth.AddMonths(1);
        RefreshCalendar();
    }

    public void GoToToday()
    {
        var now = clock().Date;
        selectedDate = now;
        displayMonth = new DateTime(now.Year, now.Month, 1);
        RefreshCalendar();
    }

    public void SelectDay(DateTime date)
    {
        selectedDate = date.Date;
        if (date.Month != displayMonth.Month || date.Year != displayMonth.Year)
        {
            displayMonth = new DateTime(date.Year, date.Month, 1);
        }
        RefreshCalendar();
    }

    public void AddEvent()
    {
        if (string.IsNullOrWhiteSpace(NewEventTitle)) return;
        calendarStore.Add(NewEventTitle, selectedDate.Date.AddHours(12), selectedDate.Date.AddHours(13));
        NewEventTitle = "";
        IsAddingEvent = false;
        RefreshCalendar();
    }

    public void DeleteEvent(string id)
    {
        calendarStore.Delete(id);
        RefreshCalendar();
    }

    private void RefreshCalendar()
    {
        OnPropertyChanged(nameof(MonthYearText));
        OnPropertyChanged(nameof(SelectedDateHeader));
        OnPropertyChanged(nameof(MonthDays));
        OnPropertyChanged(nameof(SelectedDayEvents));
        OnPropertyChanged(nameof(HasEventsForSelectedDay));
    }

    private void OnEventsChanged()
    {
        if (disposed) return;
        dispatcher.InvokeAsync(RefreshCalendar);
    }

    private IReadOnlyList<CalendarDayUi> GenerateMonthDays()
    {
        var list = new List<CalendarDayUi>();
        var firstDayOfMonth = displayMonth;
        int daysInMonth = DateTime.DaysInMonth(displayMonth.Year, displayMonth.Month);
        var today = clock().Date;

        // Monday-based week start (ISO 8601)
        int startOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        var startGridDate = firstDayOfMonth.AddDays(-startOffset);

        var daysWithEvents = calendarStore.GetDaysWithEventsInMonth(displayMonth.Year, displayMonth.Month);

        for (int i = 0; i < 42; i++)
        {
            var date = startGridDate.AddDays(i);
            bool isCurrentMonth = date.Month == displayMonth.Month;
            bool isToday = date.Date == today;
            bool isSelected = date.Date == selectedDate.Date;
            bool hasEvents = isCurrentMonth && daysWithEvents.Contains(date.Day);

            Brush fg = isCurrentMonth
                ? (isToday ? new SolidColorBrush(Color.FromRgb(255, 255, 255)) : new SolidColorBrush(Color.FromRgb(220, 230, 242)))
                : new SolidColorBrush(Color.FromRgb(85, 100, 118));

            Brush bg = isSelected
                ? new SolidColorBrush(Color.FromArgb(60, 120, 180, 255))
                : Brushes.Transparent;

            list.Add(new CalendarDayUi(date.Day, date, isToday, isSelected, isCurrentMonth, hasEvents, fg, bg));
        }

        return list;
    }

    private IReadOnlyList<CalendarEventUi> GetEventsForSelectedDay()
    {
        var evs = calendarStore.GetEventsForDay(selectedDate);
        return evs.Select(e =>
        {
            string time = e.IsAllDay ? "All Day"
                : e.End.HasValue ? $"{e.Start:h:mm tt} – {e.End:h:mm tt}"
                : $"{e.Start:h:mm tt}";

            return new CalendarEventUi(e.Id, e.Title, time, e.Location, e.Source);
        }).ToList();
    }

    // --- Weather Methods ---
    public async Task RefreshWeatherAsync()
    {
        if (isLoadingWeather) return;
        isLoadingWeather = true;
        OnPropertyChanged(nameof(IsLoadingWeather));

        var data = await weatherClient.GetForecastAsync(latitude, longitude, city, units);
        if (data != null)
        {
            weatherData = data;
            await dispatcher.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(HasWeather));
                OnPropertyChanged(nameof(WeatherCity));
                OnPropertyChanged(nameof(CurrentTemp));
                OnPropertyChanged(nameof(CurrentFeelsLike));
                OnPropertyChanged(nameof(CurrentCondition));
                OnPropertyChanged(nameof(CurrentGlyph));
                OnPropertyChanged(nameof(CurrentWind));
                OnPropertyChanged(nameof(CurrentHumidity));
                OnPropertyChanged(nameof(HighLow));
                OnPropertyChanged(nameof(HourlyForecast));
                OnPropertyChanged(nameof(DailyForecast));
            });
        }

        isLoadingWeather = false;
        await dispatcher.InvokeAsync(() => OnPropertyChanged(nameof(IsLoadingWeather)));
    }

    public async Task SetLocationAsync(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return;
        var found = await weatherClient.SearchCityAsync(cityName);
        if (found.HasValue)
        {
            city = found.Value.City;
            latitude = found.Value.Lat;
            longitude = found.Value.Lon;
            await RefreshWeatherAsync();
        }
    }

    public async Task ToggleUnitsAsync()
    {
        units = units == "Celsius" ? "Fahrenheit" : "Celsius";
        await RefreshWeatherAsync();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        weatherTimer.Stop();
        calendarStore.EventsChanged -= OnEventsChanged;
    }
}
