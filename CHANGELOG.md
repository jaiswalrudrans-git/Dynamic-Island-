# Changelog

All notable changes to the Island (Dynamic Island for Windows) project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased] - 2026-11-20

### Added
- Rebuilt Clipboard tab into compact BrimDeck-style layout with unified image rows and file transfer drop shelf.
- Fixed tofu boxes on buttons and labels by explicitly assigning Body typography and icon fonts.

### Fixed
- Repaired typography preset pairings by fixing WPF pack font loading with base URI resolution.
- Enforced strict zone map separating fancy zones from default Segoe UI Variable controls and labels.
- Fixed clipboard capture with dedicated HWND listener, background retries, and format exclusion validation.
- Fixed island sizing drift by constraining Clipboard page width to 580 DIP and setting fixed navigation width floor.
- Fixed volume and brightness sliders with click-to-position, pointer capture, and smooth dragging.
- Added non-blocking coalesced background writers with echo suppression and drag lock window.
- Inset content from visible notch walls using `IslandMetrics.WallInset` (30 DIP) and removed rounded content clip.

### Refactoring
- Restructured src/Island.App and tests/Island.Tests into isolated feature folders per AGENTS.md.

### Milestone M-Header-Clipboard-Screenshots

#### Added
- **Two-Row Centered Header Layout**:
  - Row 0 (44 DIP): Time & Date centered dead center of the island surface with optical balance spacer (72 DIP) at the left matching the Pin and Settings action buttons on the right.
  - Row 1 (36 DIP): Centered Tab Bar directly beneath the clock face with a total header height of 80 DIP (112 DIP vertical overhead for auto-sizing).
  - Legacy `LeftOfClock` position remains selectable in Settings > Tabs & Navigation (restoring the single-row 44 DIP header with 76 DIP overhead).
- **Active Tab Spring Pill with Icon + Label Expansion**:
  - The active tab expands to show both its icon and text label inside a spring-animated sliding pill that dynamically tracks button X coordinate and width.
  - Unselected tabs display clean, compact icon buttons.
  - Tab label modes configurable in Settings: `SelectedOnly` (default), `All`, or `None`.
- **Fast Tab Navigation**:
  - Mouse wheel over the header switches tabs (`HeaderMouseWheelTabSwitch`, default ON).
  - Touchpad horizontal scroll / `WM_MOUSEHWHEEL` tilts switch tabs.
  - Keyboard shortcuts `Ctrl+1` through `Ctrl+8` (and numpad 1..8) switch tabs when expanded.
  - Optional "switch tab on hover" with 150 ms intent delay (`TabSwitchOnHover`, default OFF).
  - Directional sliding transitions: incoming pages slide in from the direction of travel (+24 DIP / -24 DIP) while outgoing pages slide away with a smooth cross-fade.
- **Privacy-First Clipboard History Tab**:
  - Pure Win32 event-driven listener via `AddClipboardFormatListener` / `WM_CLIPBOARDUPDATE` on a message hook (zero polling, collapsed CPU ~0%).
  - Captures Plain Text, Rich Text/HTML, Image Thumbnails, and File lists with deduplication of consecutive identical items.
  - Strict privacy protection: ignores formats `ExcludeClipboardContentFromMonitorProcessing`, `CanIncludeInClipboardHistory = 0`, and `CanUploadToCloudClipboard = 0`.
  - Automatically filters password managers (1Password, Bitwarden, KeePass, KeePassXC, LastPass, Dashlane, Windows Credential UI, NordPass, Enpass, RoboForm).
  - Built-in heuristic engine skips secrets (GitHub personal tokens, OpenAI API keys, AWS credentials, RSA private keys, and valid credit card numbers via Luhn algorithm).
  - In-memory storage by default; opt-in restart persistence encrypted with Windows DPAPI in `%LOCALAPPDATA%\Island\clipboard.dat`.
  - Instant search box filtering by text, source app, or file names.
  - Hover actions: Copy, single-click Paste directly into previous app (via `SendInput` `Ctrl+V` to `lastExternalForegroundHwnd` without stealing focus, preserving `WS_EX_NOACTIVATE`), Pin, Delete.
  - Drag-and-drop out into any target application (Word, Discord, Explorer, etc.).
  - Global hotkey: `Ctrl + Alt + V` opens Island directly on the Clipboard tab.
- **Screenshots Tab**:
  - Automatically watches Windows Screenshots folder (`Pictures\Screenshots` via `SHGetKnownFolderPath` + `FileSystemWatcher`) and user-added custom watched folders.
  - Automatically captures clipboard snips from `Win+Shift+S`, Snipping Tool, and `Print Screen`.
  - Fast zero-dependency PNG header parser extracts native width and height from byte streams without loading full bitmaps into memory.
  - Background downsampled thumbnail caching in `%LOCALAPPDATA%\Island\thumbs`.
  - Header "Snip" button triggers `ms-screenclip:` immediately.
  - Hover actions: Copy to clipboard, Paste into target app, Open in default viewer, Show in folder, Delete (to Recycle Bin via `SHFileOperation`), Pin.
  - Drag-and-drop image out into any external application.
- **Settings Pages in MotionSettingsWindow**:
  - **Tabs & Navigation**: Tab bar position (`BelowClock` / `LeftOfClock`), Label mode (`SelectedOnly` / `All` / `None`), mouse wheel tab switch toggle, switch tab on hover toggle.
  - **Clipboard History**: On/off toggle, capacity slider (10 to 200 items), remember across restarts (DPAPI), auto-expire dropdown, secret filtering toggle, click-to-paste action, collapse after paste toggle.
  - **Screenshots**: On/off toggle, include clipboard snips toggle, save clipboard snips to Screenshots folder, thumbnail size, click action.

#### Fixed
- **System Tab Top Apps Headroom**:
  - Raised System tab height floor to 415 DIP, providing abundant clearance so all 5 process rows in Top Apps (including `msedgewebview2`) render with zero bottom clipping.
- **AppSettings Record Equality**:
  - Implemented sequence equality for collection properties (`TabOrder`, `HiddenTabs`, `ClipboardIgnoredApps`, `ScreenshotsWatchedFolders`), ensuring settings round-trips and equality checks behave consistently.
- **Number Format Specifier in Metrics**:
  - Fixed numeric format specifiers in `ScreenshotItem.cs` and `ClipboardItem.cs` to use invariant culture `0.0 MB` instead of literal `0.1 MB`.

### Milestone M-Density-Typography

#### Fixed
- **Header Tab Navigation Glyphs**:
  - Restored `<ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>` inside `IconButton` template in `OverlayWindow.xaml`.
  - Enabled WPF to render tab `iconBlock` directly as a `UIElement` instead of string coercion (`.ToString()`), completely resolving invisible tab navigation buttons and restoring all 6 Segoe Fluent Icons (`\uE8D6` Music, `\uE950` System, `\uE9D5` Tasks, `\uE713` Quick Controls, `\uE916` Focus, `\uE787` Calendar & Weather).
- **Icon Button Glyph Corruption**:
  - Eliminated missing glyph vertical boxes on Pin (`\uE718`) and Settings (`\uE713`) buttons in `OverlayWindow.xaml` by embedding explicit child TextBlocks bound to `FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"`.
  - Updated `FocusControlBtn` and `PlayBtn` in `FocusPage.xaml` to use `<ContentPresenter/>` with explicit child icon TextBlocks for Reset (`\uE777`), Play/Pause (`\uE768`/`\uE769`), Skip (`\uE893`), and Lap (`\uE7C0`).
  - Removed global implicit `<Style TargetType="TextBlock">` from `DesignTokens.xaml` that previously overrode button icon glyphs with theme body fonts.
- **Focus Timer Scaling & Margin Clearance**:
  - Constrained Pomodoro and Timer digital timer Viewbox to `MaxWidth="92"` and `MaxHeight="32"`, guaranteeing a 23 DIP margin on all sides from the 138 DIP circular progress arc in wide fonts (e.g. Dot Matrix / Doto).
  - Constrained Stopwatch Viewbox to `MaxWidth="220"` and `MaxHeight="52"` for millisecond readout clarity (`00:00.0`).
- **Focus Subtitle Deduplication**:
  - Bound `CycleSubtitle` visibility to `{Binding IsPomodoro, Converter={StaticResource Visible}}`, removing redundant "Countdown Timer" and "Stopwatch" subtitle text below the mode switch segment.
- **Resource Monitor (System Tab) Bento Grid**:
  - Restructured `SystemPage.xaml` into a unified 3-column × 2-row Bento Grid (`Grid Margin="12,4,12,14"`):
    - **Row 0**: CPU (Ring + single-row cores) | GPU (Name, usage %, temp, VRAM) | Network & Storage (Speeds, today totals, drives). All three cards share equal row height.
    - **Row 2**: Memory | Power & Battery (Watts, sparkline, status) | Top Apps (5 process rows). All three cards share equal row height.
  - Adjusted Top Apps rows to 19 DIP with a 14 DIP bottom margin, guaranteeing all 5 processes (including `msedgewebview2`) render with abundant clearance above the bottom-right curved corner.
- **CPU Core Line Wrapping**:
  - Replaced wrap panel with a single horizontal stack panel so logical core activity bars remain on a single line without orphan cores dropping to row 2.
- **Quick Controls Text Truncation**:
  - Replaced "Screenshot" with "Capture" so tile 5 never truncates to "Screensho".
  - Reflowed tile widths with zero truncation at 620 DIP.
- **Header Clock Centering & Width Synchronization**:
  - In `OverlayWindow.Panel.cs`, `DrawPanel()` dynamically updates `Header.Width` and `Body.Width` to track `curW - 24` at every frame, keeping the Header Clock centered in the physical notch throughout all spring animations.

#### Added
- **Clock-Synced Typography System**:
  - 5 semantic roles (`Display`, `Heading`, `Body`, `Label`, `Numeric`) bound via design tokens to typography themes.
  - Synchronizes the typography of the entire dynamic island to the active clock style:
    - *Simple / With date*: Segoe UI Variable
    - *Stacked*: Space Grotesk + Inter
    - *Dot matrix*: Doto + Inter
    - *LED italic*: Share Tech Mono / DSEG + Inter
    - *Neon glow*: Orbitron + Rajdhani (+ soft glow tied to clock color)
    - *Serif italic*: Instrument Serif + Inter
    - *Wide bold*: Unbounded + Chakra Petch
    - *Condensed bold*: Bebas Neue + Barlow Condensed
    - *Pixel*: Silkscreen + Share Tech Mono + Inter
    - *Compact bold*: Fredoka + Inter
    - *Terminal mono*: JetBrains Mono
    - *Gradient / Outline*: Space Grotesk + Inter
  - 180 ms smooth cross-fade transition without layout jumps.
- **10 Bundled OFL 1.1 Fonts**:
  - Embedded in `src/Island.App/Assets/Fonts/`: `Inter`, `Doto`, `Silkscreen`, `Space Grotesk`, `Share Tech Mono`, `Rajdhani`, `Chakra Petch`, `JetBrains Mono`, `Barlow Condensed`, `Caveat` (alongside `Bebas Neue`, `Fredoka`, `Instrument Serif`, `Orbitron`, `Unbounded`).
  - Added licensing notices to `ThirdPartyNotices.txt`.
- **Settings > Typography Studio**:
  - 12 interactive preset tiles with live preview card (`"14:32 · CPU 29% · Now Playing"`).
  - Unlink toggle ("Match typography to clock style"), Legibility guard toggle, and Match accent toggle.
  - Font size scale slider (90% to 130%), weight picker, tabular numerals toggle, uppercase labels toggle, text glow intensity slider.
  - Per-role override dropdowns for Display, Heading, Body, Label, and Numeric.
- **Legibility Guard (Default ON)**:
  - Enforces minimum size thresholds for decorative fonts (Doto >= 18 px, Silkscreen >= 18 px, Bebas Neue >= 16 px, Caveat >= 18 px).
  - Automatically falls back to companion font (`Inter` / `Segoe UI`) below threshold; Body and small Label never use decorative fonts.
- **Dynamic Content-Fitted Auto-Sizing Engine**:
  - Dynamic two-pass layout calculation (`Measure` with infinity for unconstrained reflow width, then pass 2 with target width for exact content height).
  - Uniform spacing tokens: Island padding 12 DIP, Header height 44 DIP, Card padding 12 DIP, inter-card gap 8 DIP, Card radius 16 DIP.
  - Snapped to 8 DIP grid within min 580 DIP and max 62% work area width / 70% height.
- **Dev-Only Layout Debug HUD**:
  - Toggled with `--layout-debug` or Settings > Developer & Diagnostics.
  - Displays HUD with exact content and island bounds in DIPs.
- **134 Automated Tests**:
  - Full suite of unit, layout, typography token, and glyph isolation tests (`dotnet test -c Release`).

---

## [0.8.0] - 2026-11-20

### Milestone M-Transparency-Clock-Polish (Phase 5: Neon & Cyberpunk)

#### Added
- **Semantic Color Tokens**:
  - Pink (`#FF2E97`): Reserved strictly for active states (active tab pill, active playback button).
  - Cyan (`#00E5FF`): Dedicated strictly to data metrics and progress indicators.
  - Yellow (`#FCEE0A`): Dedicated to temperature and load warnings.
  - Secondary Text (`#B8B0C8`): Readability floor providing >= 5:1 contrast against dark surfaces.
- **Bottom-Edge Reactive Neon Line**:
  - 1.5 DIP neon line along the bottom perimeter of the island.
  - Music Reactive: Pulses dynamically in sync with audio spectrum energy.
  - Load Reactive: Automatically shifts to warning yellow or critical red under high CPU/GPU load.
- **5 New Built-In Token Palettes**:
  - `Cyberpunk`, `Aurora`, `Amber Terminal`, `Mono White`, `Dark`.
- **Neon Intensity Slider**:
  - Configurable in Settings > Appearance (0% to 100%, default 60%) to scale blur radius and glow opacity across all elements.

---

## [0.5.0] - 2026-11-19

### Milestone M-Core-Shell

#### Added
- Pure alpha transparency on Windows 10/11 without blur shaders.
- Liquid spring physics for notch expansion and collapse animations.
- GSMTC media integration with live timeline seeking and spectrum visualizer.
- System hardware telemetry (CPU, GPU, RAM, VRAM, Power/Battery, Network, Storage).
- Pomodoro focus timer and stopwatch with progress ring.
- Quick controls toggles (Mic, Wi-Fi, Bluetooth, Snip, Lock, Sleep, Task Manager).
- Mini calendar with upcoming events and WMO weather forecast strip.
