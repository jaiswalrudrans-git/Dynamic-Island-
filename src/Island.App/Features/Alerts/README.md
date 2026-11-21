# Alerts

Owns notch alert banners, audio OSD, brightness OSD, power/battery alerts, and temperature notifications.

## Key Files
- `ViewModels/AlertsViewModel.cs`: Alert presentation state, active alert title, icon, and progress.
- `Services/AudioOsdWatcher.cs`: Audio endpoint volume/mute change listener via NAudio.
- `Services/BrightnessOsdWatcher.cs`: Display brightness level listener.
- `Services/PowerAlertWatcher.cs`: AC online/battery state watcher.
- `Services/TemperatureAlertWatcher.cs`: High temperature warning watcher.

## Dependencies
- `Core/` (Island.Core.Alerts queue and models, Island.Core.Settings)
