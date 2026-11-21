# Shell

Owns the top-level Island window, notch geometry, auto-sizing, tray icon, and expand/collapse motion.

## Key Files
- `OverlayWindow.xaml` / `.xaml.cs`: Transparent topmost window and Win32 hook routing.
- `OverlayWindow.Panel.cs`: Panel lifecycle, tab initialization, and settings dispatch.
- `OverlayWindow.Sizing.cs`: Auto-sizing logic, padding calculations, and tab bar layout.
- `OverlayWindow.Motion.cs`: Physics springs, frame rendering loop, and expand/collapse transitions.
- `OverlayWindow.Render.cs`: Notch clipping, opacity crossfades, and bottom neon glow.
- `OverlayWindow.Interactions.cs`: Keyboard navigation, hotkeys, and clipboard dispatch.
- `NotchClip.cs`: Smooth Bezier notch path geometry.
- `IslandTray.cs`: System tray icon and context menu.

## Dependencies
- `Core/` (desktop placement, settings, identity)
- `Design/` (design tokens, typography, colors)
