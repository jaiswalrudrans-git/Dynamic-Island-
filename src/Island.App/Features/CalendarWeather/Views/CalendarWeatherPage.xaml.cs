using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Island.App.Features.CalendarWeather.ViewModels;

namespace Island.App.Features.CalendarWeather.Views;

public partial class CalendarWeatherPage : UserControl
{
    private readonly CalendarWeatherViewModel model;
    private readonly Action<bool> setTypingFocus;

    public CalendarWeatherPage(CalendarWeatherViewModel model, Action<bool> setTypingFocus)
    {
        InitializeComponent();
        this.model = model;
        this.setTypingFocus = setTypingFocus;
        DataContext = model;
    }

    // --- Calendar Nav ---
    private void PrevMonthClick(object sender, RoutedEventArgs e) => model.PreviousMonth();
    private void NextMonthClick(object sender, RoutedEventArgs e) => model.NextMonth();
    private void TodayClick(object sender, RoutedEventArgs e) => model.GoToToday();

    private void DayCellClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DateTime dt }) model.SelectDay(dt);
    }

    // --- Calendar Event Add / Manage ---
    private void ToggleAddEventClick(object sender, RoutedEventArgs e)
    {
        model.IsAddingEvent = !model.IsAddingEvent;
        if (model.IsAddingEvent)
        {
            setTypingFocus(true);
            EventInput.Focus();
        }
        else
        {
            setTypingFocus(false);
        }
    }

    private void SaveAddEventClick(object sender, RoutedEventArgs e)
    {
        model.AddEvent(syncToLaptop: model.SyncWithLaptopChecked);
        setTypingFocus(false);
    }

    private void AddAndSyncToLaptopClick(object sender, RoutedEventArgs e)
    {
        model.AddEvent(syncToLaptop: true);
        setTypingFocus(false);
    }

    private void CancelAddEventClick(object sender, RoutedEventArgs e)
    {
        model.IsAddingEvent = false;
        setTypingFocus(false);
    }

    private void EventInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            model.AddEvent(syncToLaptop: model.SyncWithLaptopChecked);
            setTypingFocus(false);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            model.IsAddingEvent = false;
            setTypingFocus(false);
            e.Handled = true;
        }
    }

    private void DeleteEventClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) model.DeleteEvent(id);
    }

    private void SyncLaptopCalendarClick(object sender, RoutedEventArgs e) => _ = model.SyncLaptopCalendarAsync();

    private void OpenLaptopCalendarClick(object sender, RoutedEventArgs e) => model.OpenLaptopCalendarApp();

    // --- Weather Controls ---
    private void OpenCitySearchClick(object sender, RoutedEventArgs e)
    {
        model.IsSearchingCity = true;
        setTypingFocus(true);
        CitySearchInput.Focus();
    }

    private void ExecuteCitySearchClick(object sender, RoutedEventArgs e)
    {
        _ = model.SetLocationAsync(model.CitySearchQuery);
        setTypingFocus(false);
    }

    private void CancelCitySearchClick(object sender, RoutedEventArgs e)
    {
        model.IsSearchingCity = false;
        setTypingFocus(false);
    }

    private void CitySearchInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = model.SetLocationAsync(model.CitySearchQuery);
            setTypingFocus(false);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            model.IsSearchingCity = false;
            setTypingFocus(false);
            e.Handled = true;
        }
    }

    private void AutoDetectLocationClick(object sender, RoutedEventArgs e) => _ = model.AutoDetectLocationAsync();

    private void ToggleUnitsClick(object sender, RoutedEventArgs e) => _ = model.ToggleUnitsAsync();

    private void RefreshWeatherClick(object sender, RoutedEventArgs e) => _ = model.RefreshWeatherAsync();

    private void HourlyScrollPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
