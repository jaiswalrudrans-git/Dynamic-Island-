# Clipboard

Owns clipboard history, favorites, search, image previews, file transfer drop shelf, and paste dispatching.

## Key Files
- `Views/ClipboardPage.xaml` / `.xaml.cs`: Compact BrimDeck-style dual-half layout (Clipboard history + File transfer shelf).
- `ViewModels/ClipboardViewModel.cs`: Unified clipboard and screenshot image rows, favorites filtering, drop shelf collection.
- `ViewModels/ScreenshotsViewModel.cs`: Watched screenshots caching.
- `Services/ClipboardNative.cs`: Win32 clipboard listener hooks, background paste dispatching (`WS_EX_NOACTIVATE` + `SendInput`).

## Dependencies
- `Core/` (Island.Core.Clipboard, Island.Core.Screenshots stores and models)
- `Design/` (tokens, typography, colors)
