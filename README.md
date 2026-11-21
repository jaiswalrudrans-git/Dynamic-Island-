# Island 2

**Island 2** is a dynamic, hardware-accelerated desktop companion for Windows 10 and 11, inspired by Dynamic Island and modern glass design paradigms. It rests unobtrusively at the top of your display, morphing smoothly between an ultra-compact status pill and rich, content-fitted widgets with zero layout jump and fluid spring physics.

---

## Features & Tab Ecosystem

Island 2 bundles 7 responsive, hardware-accelerated tabs within an 8-DIP grid dynamic layout:

1. **Music & Media (`\uE8D6`)**:
   - Live media session integration via Windows Media Transport Controls (`GlobalSystemMediaTransportControlsSessionManager`).
   - Horizontal marquee title scrolling for long tracks.
   - Media player identification badge (Spotify, Chrome, Edge, Media Player).
   - Real-time LRCLIB synchronized lyrics with backward/forward seek tracking.
   - Smooth spectrum audio visualizer and scrubber.

2. **System Telemetry & Bento Grid (`\uE950`)**:
   - Real-time CPU, GPU (NVIDIA NVML & WMI fallback), RAM, and Battery power telemetry.
   - Proportional Bento Grid with balanced row heights.
   - Top Apps RAM list with native process icons and 5-item visibility with comfortable padding.
   - 5-step elastic overflow protection preventing label truncation.

3. **Tasks & Reminders (`\uE9D5`)**:
   - Lightweight task management with priority tagging (High, Medium, Low).
   - In-line task creation, completion checkmarks, and drag reordering.
   - Timed alerts and reminders integrated into the dynamic pill OSD.

4. **Quick Controls (`\uE9E9`)**:
   - Master volume slider and display brightness slider (WmiMonitorBrightness).
   - Balanced 4x2 uniform grid featuring 8 quick-toggle tiles: Mute Mic, Wi-Fi, Bluetooth, Focus Assist, Snip Tool, Lock PC, Sleep, and Task Manager.
   - Clean two-word unclipped typography.

5. **Focus & Pomodoro (`\uE916`)**:
   - Pomodoro interval timer (Work, Short Break, Long Break cycles), Countdown Timer, and Lap Stopwatch.
   - Visual progress ring with tabular numerals.
   - Crisp, high-contrast transport buttons (Play/Pause, Reset, Skip) with explicit Windows icon fallback chains (`Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji`).

6. **Calendar & Weather (`\uE787`)**:
   - Real-time weather via Open-Meteo API with WMO condition glyphs, temperature, humidity, and wind speed.
   - Integrated calendar agenda parsing standard `.ics` feeds and local schedules.
   - Multi-day forecast cards starting with Today.

7. **Clipboard History & Screenshots Dual-Half (`\uE8C8`)**:
   - **Unified Dual-Half View**: Left half displays searchable recent copies (text, code, HTML, images, files); right half hosts a 3x2 screenshot gallery.
   - **Privacy Filtering**: Built-in heuristic engine automatically excludes password managers (1Password, Bitwarden, KeePass, LastPass), secret tokens (GitHub, OpenAI, AWS), and credit card numbers.
   - **Non-Activating Paste**: Direct paste into the previous window via `SendInput` without stealing window focus.
   - **Encrypted Restart Storage**: Opt-in DPAPI encryption for persistent history in `%LOCALAPPDATA%\Island`.
   - **Global Hotkey**: Press <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>V</kbd> to open directly to the Clipboard tab.

---

## Design & Typography Architecture

- **Two-Row Centered Header**:
  - Row 0 (44 DIP): Center-aligned clock face and date with optical balance actions (Pin and Settings).
  - Row 1 (36 DIP): Centered tab bar with sliding accent pill indicator.
  - Legacy `LeftOfClock` header mode supported via Settings.
- **Font Role System & Composite Fallbacks**:
  - 5 abstract roles: `Display`, `Heading`, `Body`, `Label`, `Numeric`.
  - Every role is backed by a verified composite fallback chain terminating in `Segoe UI Variable Text, Segoe UI, Arial`.
  - `GlyphCoverageChecker` verifies Unicode glyph coverage at boot using `GlyphTypeface.CharacterToGlyphMap`.
  - `LegibilityGuard` strictly prevents decorative fonts from being assigned to functional labels, buttons, or settings text.
- **Pure Alpha Transparency**:
  - 100% shaderless, pure alpha transparency engine.
  - Three surface styles: `Opaque` (100% alpha), `Translucent` (88% alpha), and `Clear` (50% alpha).
- **Non-Activating Window (`WS_EX_NOACTIVATE`)**:
  - Never steals keyboard or mouse focus from games, IDEs, or full-screen applications.

---

## Keyboard & Navigation Shortcuts

| Shortcut | Action |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>V</kbd> | Open Island directly to Clipboard & Screenshots |
| <kbd>Ctrl</kbd> + <kbd>1</kbd> to <kbd>Ctrl</kbd> + <kbd>7</kbd> | Switch directly to tabs 1 through 7 |
| <kbd>Mouse Wheel</kbd> over Header | Cycle through tabs rapidly |
| <kbd>Touchpad Horizontal Scroll</kbd> | Slide through tabs with directional animation |
| <kbd>Esc</kbd> | Collapse expanded Island to compact pill |

---

## Building from Source

### Prerequisites
- Windows 10 (version 19041+) or Windows 11
- .NET 8 SDK (`8.0.400` or higher)

### Build Pipeline
Run the automated build script from PowerShell:

```powershell
.\build.ps1
```

The script automatically:
1. Verifies the .NET 8 SDK environment.
2. Restores NuGet dependencies.
3. Compiles the entire solution in `Release` configuration with `-p:TreatWarningsAsErrors=true`.
4. Executes the full 172-test suite (including layout matrix, typography fallbacks, memory stress tests, and UI capture tests).
5. Copies all deployable release binaries to `artifacts\`.

To build without running tests:
```powershell
.\build.ps1 -SkipTests
```

---

## Packaging & Installer

Island 2 includes an Inno Setup 6 packaging definition (`installer.iss`):
- Per-user installation to `%LOCALAPPDATA%\Programs\Island` (requires no administrative elevation or UAC prompts).
- Creates optional Desktop and Startup shortcuts (`shell:startup`).
- Bundles all required runtime dependencies, assets, and third-party notices.
- Clean uninstallation preserving user settings and themes in `%LOCALAPPDATA%\Island` by default.

To compile the installer:
```powershell
iscc.exe installer.iss
```
The output installer executable will be generated at `artifacts\installer\Island2-Setup-v2.1.0.exe`.

---

## License & Third-Party Notices

Island 2 is released under the MIT License. See [ThirdPartyNotices.txt](file:///d:/Island%202/ThirdPartyNotices.txt) for licenses and copyright notices of bundled fonts (OFL 1.1) and third-party libraries (CommunityToolkit.Mvvm, NAudio).
