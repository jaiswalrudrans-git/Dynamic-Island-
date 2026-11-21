# Island Code Map

## Directory Tree
- `src/Island.App/`: Main WPF desktop application (.NET 8 Windows)
  - `Assets/`: Embedded app icons, font TTF files (Doto, Inter, JetBrains Mono, etc.)
  - `Core/`: Low-level desktop integration (single instance, system metrics, DPI)
  - `Design/`: Design system tokens, color palettes, theme manager, typography presets
  - `Header/`: Header bar, clock face styles, tab bar strip, pin/settings buttons
  - `Features/Alerts/`: Collapsed notch alerts, audio volume/mute OSD, brightness, battery, temp
  - `Features/CalendarWeather/`: Calendar events, weather forecast, location tracking
  - `Features/Clipboard/`: Clipboard history, snip captures, screenshots gallery, paste dispatch
  - `Features/Controls/`: Quick control toggle tiles, audio devices, power modes
  - `Features/Focus/`: Pomodoro timer, stopwatch, focus intervals
  - `Features/Music/`: Media session player, lyrics view, loopback audio visualizer
  - `Features/Settings/`: Multi-page settings window (Appearance, Clock, Typography, Tabs, Motion, System, About)
  - `Features/System/`: Hardware telemetry monitors (CPU, GPU, RAM, Disk, Net), process tree
  - `Features/Tasks/`: Daily task manager, reordering, completions, reminders
  - `Shell/`: Island window chrome, notch shape geometry, springs, hover/expand interactions, auto-sizing
- `src/Island.Core/`: Cross-platform models, stores, services, and domain contracts
- `tests/Island.Tests/`: Unit tests and layout linters mirroring the App feature structure

## Fix Index ("To fix X, look in Y")
- **Header and tab icons**: `src/Island.App/Header/Views/ClockFace.cs`, `src/Island.App/Header/Views/PanelNavigation.cs`, `src/Island.App/Design/DesignTokens.xaml`
- **Island sizing**: `src/Island.App/Shell/OverlayWindow.Sizing.cs`, `src/Island.App/Header/Views/PanelNavigation.cs` (demands)
- **Island padding**: WallInset (30 DIP = shoulder 14 + padding 16) in DesignMetrics;
  Body.Width = Header.Width = IslandWidth - 2 * WallInset; Pages clips to rectangular bounds.
- **Fonts**: `src/Island.App/Design/TypographyResources.cs`, `src/Island.App/Assets/Fonts/`
- **Themes**: `src/Island.App/Design/ThemeResources.cs`, `src/Island.App/Design/DesignTokens.xaml`
- **Tab switching**: `src/Island.App/Header/Views/PanelNavigation.cs`, `src/Island.App/Shell/OverlayWindow.Interactions.cs`
- **Clipboard and paste**: `src/Island.App/Features/Clipboard/Services/ClipboardNative.cs`, `src/Island.App/Features/Clipboard/ViewModels/ClipboardViewModel.cs`, `src/Island.Core/Clipboard/`
- **Volume and brightness**: `src/Island.App/Features/Alerts/Services/AudioOsdWatcher.cs`, `src/Island.App/Features/Alerts/Services/BrightnessOsdWatcher.cs`
- **Weather**: `src/Island.App/Features/CalendarWeather/ViewModels/CalendarWeatherViewModel.cs`, `src/Island.Core/Weather/`
- **Calendar**: `src/Island.App/Features/CalendarWeather/Services/WindowsCalendarService.cs`, `src/Island.Core/Calendar/`
- **Alerts**: `src/Island.App/Features/Alerts/ViewModels/AlertsViewModel.cs`, `src/Island.Core/Alerts/`
- **System telemetry**: `src/Island.App/Features/System/Services/SystemHardwareMonitor.cs`, `src/Island.App/Features/System/ViewModels/SystemViewModel.cs`, `src/Island.Core/Telemetry/`
- **Settings**: `src/Island.App/Features/Settings/Views/MotionSettingsWindow.xaml`, `src/Island.App/Features/Settings/Views/MotionSettingsWindow.xaml.cs` (and partial sections under `Features/Settings/`)
