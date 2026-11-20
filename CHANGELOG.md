# Changelog

All notable changes to the Island (Dynamic Island for Windows) project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased] - 2026-11-20

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
