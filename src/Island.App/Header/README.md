# Header

Owns the Island header controls: dead-center clock, tab navigation bar, and pin/settings buttons.

## Key Files
- `Views/ClockFace.cs`: Vector-rendered clock face supporting tabular figures and font scaling.
- `Views/ClockStyles.cs`: Clock style renderers (Segment, DotMatrix, Neon, Pixel, Serif, etc.).
- `Views/PanelNavigation.cs`: Animated tab bar with spring pill indicator and keyboard/wheel switches.
- `ViewModels/ClockViewModel.cs`: Time/date formatting and precision wake timer scheduling.
- `ViewModels/PanelTab.cs`: Tab definitions, metadata, and icons.

## Dependencies
- `Core/` (settings, motion physics)
- `Design/` (tokens, typography, colors)
