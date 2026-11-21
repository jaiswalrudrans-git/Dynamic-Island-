# Controls

Owns quick toggles, master volume, brightness sliders, and device controls.

## Key Files
- `Views/QuickControlsPage.xaml` / `.xaml.cs`: Interactive tile buttons and sliders.
- `ViewModels/QuickControlsViewModel.cs`: Volume, brightness, Wi-Fi, Bluetooth, and mute states.
- `Services/QuickControlsManager.cs`: CoreAudio, WMI brightness, and Windows Radios API management.

## Dependencies
- `Core/` (settings, logging)
- `Design/` (tokens, typography, colors)
