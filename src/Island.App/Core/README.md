# Core
Owns core application lifecycle, window desktop interop, and system metrics.

## Key Files
- `OverlayDesktop.cs`: Win32 desktop window level, click-through, and monitor placement.
- `SystemMetricsNative.cs`: Win32 memory, CPU times, power status, and monitor resolution.

## Dependencies
- Interacts with Win32 user32, kernel32, and ntdll.
- Consumed by Shell, Hardware Monitor, and Power Watcher.
