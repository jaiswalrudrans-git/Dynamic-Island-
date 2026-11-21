# Tasks

Owns the to-do item management, priority filtering, and completion tracking tab.

## Key Files
- `Views/TasksPage.xaml` / `.xaml.cs`: Task list UI with quick inline creation, filtering, and row actions.
- `ViewModels/TasksViewModel.cs`: Task collection management, priority color mapping, and store synchronization.

## Dependencies
- `Core/` (Island.Core.Tasks store, models)
- `Design/` (tokens, typography, colors)
