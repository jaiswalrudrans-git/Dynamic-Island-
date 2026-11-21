# CalendarWeather

Owns calendar agenda, mini-month grid, reminders, Open-Meteo weather forecasts, and the Calendar & Weather tab.

## Key Files
- `Views/CalendarWeatherPage.xaml` / `.xaml.cs`: Dual-pane calendar month/day selector and weather forecast card.
- `ViewModels/CalendarWeatherViewModel.cs`: Month grid generation, event schedule aggregation, and weather status formatting.
- `Services/WindowsCalendarService.cs`: Windows AppointmentManager local calendar integration.

## Dependencies
- `Core/` (Island.Core.Calendar, Island.Core.Weather clients and stores)
- `Design/` (tokens, typography, colors)
