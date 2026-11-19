# Island implementation plan

## Architecture

Use C# 12 and .NET 8. The x64 WPF executable owns application lifetime, native window integration, resource dictionaries, views and view models. A UI-independent core owns versioned settings, validated theme palettes and per-user persistence. Add feature services to their owning layer when their milestone starts. Keep OS handles and UI-thread dispatch out of domain logic.

The overlay will use a fixed-size HWND with an animated inner shape. Draw, clip and hit-test from the same geometry. Subscribe to frame callbacks only while animating. Start expensive data subscriptions only while their consumers are visible.

## Packages and Windows integration

Use CommunityToolkit.Mvvm for observable state and commands, System.Text.Json for local files, and xUnit for domain tests. Add NAudio with Music, the Windows SDK .NET projections for SMTC and radios, and PDH, WMI and optional dynamic NVML with System. Call Win32 directly for monitor placement, activation and hit testing. Keep runtime network calls limited to the approved services.

## Risks and verification

- Verify input pass-through against another process, focus retention, mixed DPI and monitor hot-plug on the desktop in M1. Measure idle CPU and frame times on real displays during polish.
- Treat missing hardware counters, radio access and media capabilities as unavailable. Keep collecting independent readings.
- Write settings through same-directory replacement, retain malformed input for diagnosis, and guard newer schema versions against overwrite. Validate palettes before constructing WPF brushes.
- The requested IP location and remote ICS options can require network destinations outside the allowlist. Resolve that constraint in M7. Manual location and local ICS can run within it.

## Delivery sequence

| Milestone | Deliverable |
| --- | --- |
| M0 | Solution, Git, settings and theme foundation, tests and rolling logs |
| M1 | Collapsed clock notch, click-through, focus, monitor selection and DPI |
| M2 | Expand/collapse animation, tabs, pin, Escape and tray |
| M3 | Music, compact preview, equalizer and lyrics |
| M4 | System readings and compact readouts |
| M5 | Queued alerts and system OSD |
| M6 | Quick Controls, Focus, Tasks and Reminders |
| M7 | Calendar and Weather |
| M8 | Settings pages, theme editing and hotkeys |
| M9 | Fullscreen and maximized behavior, profiling, polish and installer |

At every boundary, build, run, self-test, record results and manual checks in PROGRESS.md, commit, and wait for the user's instruction to continue.
