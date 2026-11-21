# Clipboard

Owns clipboard history, snips, pinned clips, search, screenshot gallery, and paste dispatching.

## Key Files
- `Views/ClipboardPage.xaml` / `.xaml.cs`: Dual-pane clipboard item list and screenshot gallery grid.
- `ViewModels/ClipboardViewModel.cs`: Clipboard item filtering, pinning, deletion, and preview state.
- `ViewModels/ScreenshotsViewModel.cs`: Screenshot thumbnail caching and watched folder monitoring.
- `Services/ClipboardNative.cs`: Win32 clipboard listener hooks and foreground window tracking.

## Dependencies
- `Core/` (Island.Core.Clipboard, Island.Core.Screenshots stores and models)
- `Design/` (tokens, typography, colors)
