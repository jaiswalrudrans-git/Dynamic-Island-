using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Island.App.ViewModels;

namespace Island.App.Views;

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

    private void PrevMonthClick(object sender, RoutedEventArgs e) => model.PreviousMonth();
    private void NextMonthClick(object sender, RoutedEventArgs e) => model.NextMonth();
    private void TodayClick(object sender, RoutedEventArgs e) => model.GoToToday();

    private void DayCellClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DateTime dt }) model.SelectDay(dt);
    }

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
        model.AddEvent();
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
            model.AddEvent();
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

    private void RefreshWeatherClick(object sender, RoutedEventArgs e) => _ = model.RefreshWeatherAsync();

    private void CityClick(object sender, RoutedEventArgs e) => _ = model.ToggleUnitsAsync();
}
