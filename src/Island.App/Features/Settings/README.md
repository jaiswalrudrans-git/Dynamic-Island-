# Settings

Owns the standalone Island settings window, tabbed customization pages, and configuration persistence.

## Key Files
- `Views/MotionSettingsWindow.xaml`: Main tabbed settings window layout.
- `Appearance/`: Themes, neon glow, surface translucency, and scale settings.
- `Clock/`: Clock face studio selector, color swatches, seconds/24h toggles.
- `Typography/`: Typography themes, preset browser, font fallbacks, and glow effects.
- `Tabs/`: Tab navigation positions, label modes, clipboard & screenshot preferences.
- `Motion/`: Animation speed, hover/collapse timing, and reduced motion toggles.
- `SystemAlerts/`: Collapsed stat badges, top processes sort, and alert OSD toggles.
- `About/`: App metadata, engine architecture notes, and open source licenses.

## Dependencies
- `Core/` (AppSettings store, AppIdentity)
- `Design/` (TypographyResources, theme tokens)
- `Header/` (ClockFace preview renderer)
