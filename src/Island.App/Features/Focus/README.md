# Focus

Owns the Pomodoro, countdown timer, and stopwatch focus session tools.

## Key Files
- `Views/FocusPage.xaml` / `.xaml.cs`: Circular progress ring timer interface with preset chips and mode selector.
- `ViewModels/FocusViewModel.cs`: Session state, laps tracking, and control commands.

## Dependencies
- `Core/` (Island.Core.Focus manager and timer physics)
- `Design/` (tokens, typography, colors)
