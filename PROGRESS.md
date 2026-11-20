# Milestone progress

## M-Density-Typography (Parts A & B Complete) · 2026-11-20

### Part A: Space Efficiency & Dynamic Content-Fitted Island
- **Dynamic 2-Pass WPF Content Measurement Engine**:
  - Replaced hardcoded dimensions in `PanelNavigation.cs` with dynamic two-pass layout calculation (`Measure` with infinity for unconstrained reflow width, then pass 2 with target width for exact content height).
  - Uniform spacing tokens: Island padding 12 DIP, Header height 44 DIP, Card padding 12 DIP, inter-card gap 8 DIP, Card radius 16 DIP (`Padding.Island`, `Padding.Card`, `Padding.Page`, `Margin.Card`, `Radius.Card`, `Height.Header`).
  - Island dimensions are clamped within minimum width (580 DIP) and maximum (62% work area width, 70% work area height) and snapped to an 8 DIP grid.
  - Eliminated nested duplicate panels: tabs sit directly on island background surface with zero double boxing.
- **Per-Tab Refinements**:
  - **Centered Header**: Replaced asymmetric 3-column header grid with full-width container and `HorizontalAlignment="Center"`, keeping the clock dead center across all tabs.
  - **Music**: Compact 170 DIP content height, clean track seeker, cover art, and transport row.
  - **System**: Expanded content height floor to 360 DIP so all 5 process rows in Top Apps sit with abundant breathing room and zero bottom clipping; CPU per-core bars aligned on single horizontal baseline without orphan line wrap.
  - **Focus**: Spacious, elegant vertical design with mode segment selector, subtitle, 150x150 circular progress ring, preset duration chips, and 48x48 rounded Play/Pause pill.
  - **Calendar & Weather**: Fixed month header truncation ("Nov 2026"), daily forecast strictly starts with Today.
  - **Quick Controls**: Reflowed tiles with no empty slots and clean labels ("Focus" / "Off", "Lock" / "PC").
- **Dev-Only Layout Debug Overlay**:
  - Toggled via `--layout-debug` or Settings > Developer & Diagnostics.
  - Displays HUD with exact content and island bounds in DIPs.
  - Automated layout test `TabDimensionsHaveNoMoreThan12DipUnusedSpaceOnAnySide` verifies slack <= 8 DIP.

### Part B: Clock-Synced Typography System
- **5-Role Typography System**:
  - `Display` (clock, hero numbers, big titles)
  - `Heading` (card titles, tab labels)
  - `Body` (normal text)
  - `Label` (small badges, uppercase labels)
  - `Numeric` (metrics, stats, tabular figures)
  - Role tokens in `DesignTokens.xaml` and dynamic resources in `TypographyResources.cs`.
- **Icon Isolation & Button Protection**:
  - Removed global implicit `<Style TargetType="TextBlock">` from `DesignTokens.xaml` that previously overrode button icon glyphs with body fonts (Inter, Doto), restoring pristine Segoe Fluent Icons (`\uE718` pin, `\uE713` settings, `\uE768` play, `\uE777` reset, `\uE893` skip, etc.).
  - Explicitly anchored `FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji"` in control templates for `IconButton`, `FocusControlBtn`, `PlayBtn`, `IslandIconButton`, and `Transport`.
- **System / Resource Monitor Typography & Layout Integration**:
  - Applied dynamic font roles across all cards: Headings on card titles, Numeric on stats (CPU %, RAM %, GPU %, speeds, watts, drives, process metrics), Labels on secondary notes.
  - Resolved GPU card collision by setting `MaxWidth="105"` and `Margin="0,0,8,0"` on `GpuShortName` so `"RTX 4050 Laptop"` and `"0% · 60°C"` never mash together.
  - Adjusted Top Apps rows to 20 DIP with 12 DIP card bottom padding and telemetry debounced re-measurement, ensuring all 5 process rows are visible without clipping by the 24 DIP bottom-right notch curve.
- **Focus Tab Responsive Timer Scaling**:
  - Replaced fixed-width ring timer text with a dynamic `Viewbox`, scaling text seamlessly inside the 150 DIP circular progress ring in Pomodoro/Timer modes (`MaxWidth="122"`, `MaxHeight="44"`).
  - Expanded stopwatch text container (`MaxWidth="240"`, `MaxHeight="56"`), completely fixing truncated millisecond digits (`:0:00.+` -> clean, centered `00:00.0` in any font).
- **Legibility Guard Strict Enforcement**:
  - Guard ensures decorative display fonts (Doto, Silkscreen, Bebas Neue, Caveat) are never applied to headings or labels below 18 DIP, automatically falling back to clean companion fonts (Inter).
- **Clock-Sync Engine (Default ON: "Match typography to clock style")**:
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
  - 180 ms cross-fade transition without layout jump.
  - Dynamic 2-pass re-measure ensures wide fonts (Orbitron, Unbounded, Doto, Silkscreen) expand the island smoothly without truncation.
- **Match Accent Color to Clock Color (Default ON)**:
  - Dynamically updates active accent brushes, neon glow, and progress indicators to match the active clock color.
- **Legibility Guard (Default ON)**:
  - Enforces minimum size thresholds for decorative fonts (Doto >= 18 px, Silkscreen >= 18 px, Bebas Neue >= 16 px, Caveat >= 18 px).
  - Automatically falls back to companion font (`Inter` / `Segoe UI`) below threshold; Body and small Label never use decorative fonts.
- **Settings > Typography Studio**:
  - 12 interactive preset tiles with live preview card (`"14:32 · CPU 29% · Now Playing"`).
  - Unlink toggle ("Match typography to clock style"), Legibility guard toggle, and Match accent toggle.
  - Font size scale slider (90% to 130%), weight picker (Regular / Medium / Bold), tabular numerals toggle, uppercase labels toggle, text glow intensity slider.
  - Per-role override dropdowns for Display, Heading, Body, Label, and Numeric.
- **10 Bundled OFL 1.1 Fonts**:
  - Embedded in `src/Island.App/Assets/Fonts/`: `Inter`, `Doto`, `Silkscreen`, `Space Grotesk`, `Share Tech Mono`, `Rajdhani`, `Chakra Petch`, `JetBrains Mono`, `Barlow Condensed`, `Caveat` (alongside `Bebas Neue`, `Fredoka`, `Instrument Serif`, `Orbitron`, `Unbounded`).
  - Documented in `ThirdPartyNotices.txt`.
- **Automated Verification**:
  - All 130 unit, layout, and typography tests pass (`dotnet test -c Release`).
  - 21 verification screenshots generated for Music, System, and Focus across Dot Matrix, LED, Serif, Wide, Condensed, Pixel, and Neon clock styles.

## M-Transparency-Clock-Polish (Phases 1, 2 & 5 Complete) · 2026-11-20

### Phase 5: Neon / Cyberpunk Polish
- **Color Roles & Token System**:
  - **Pink (`#FF2E97`)**: Reserved strictly for ACTIVE states (active tab indicator pill with neon glow, active playback button, active toggle switches).
  - **Cyan (`#00E5FF`)**: Dedicated strictly to DATA (progress bars, CPU ring gauge, per-core bars, memory bars, sparkline power curves, seek progress).
  - **Yellow (`#FCEE0A`)**: Dedicated to WARNINGS (high CPU/GPU temperature, excessive load alerts).
  - **Secondary Text (`#B8B0C8`)**: Lavender-gray readability floor providing >= 5:1 contrast against dark surfaces (never purple on purple).
- **Outlines Removed & Subtle Card Depth**:
  - Removed outlines around cards, tiles, and tab bars at rest.
  - Cards use subtle surface fill (`Theme.SurfaceRaised`) plus a 1 px top highlight (`Theme.CardHighlight`).
  - Borders appear subtly only on hover or keyboard focus (`Theme.CardRestBorder` -> `Theme.Border`).
- **Targeted Glow Effects & Neon Intensity**:
  - Glow effects applied strictly to:
    1. Active tab pill (`PillGlow`)
    2. Primary transport action button (Play/Pause)
    3. Progress rings (`CpuRingPath`, `RingPath`)
    4. Key live hero numbers
  - **Settings > Appearance: "Neon intensity" slider** (0% to 100%, default 60%) with numeric text box and `-`/`+` steppers that dynamically scales glow blur radius and opacity live across all elements.
- **Bottom-Edge Reactive Neon Line**:
  - 1.5 DIP neon line along the bottom perimeter of the active notch/island (`OverlayWindow.xaml: BottomEdge`).
  - **Music Reactive**: Pulses dynamically in sync with the audio spectrum/energy level when music plays.
  - **Load Reactive**: Automatically shifts to warning yellow (`#FCEE0A`) or critical red when CPU load > 85% or temperature alerts fire.
  - Configurable in Settings > Appearance with toggles for enabling/disabling the line, music reactivity, and load reactivity.
- **5 New Built-In Themed Token Palettes**:
  - `Cyberpunk`: Night City violet-black surface (`#0C0616`), `#00E5FF` data, `#FF2E97` active, `#FCEE0A` warning/clock.
  - `Aurora`: Northern lights theme (`#07151D` deep night, `#00FFC2` mint cyan data, `#7B61FF` purple active).
  - `Amber Terminal`: Retro CRT phosphor (`#0F0C05` surface, `#FFB300` amber data, `#FFD54F` active).
  - `Mono White`: High-contrast minimalist tech (`#0A0A0A` surface, `#FFFFFF` text, `#00E5FF` cyan active, `#E0E0E0` data).
  - `Sakura`: Tokyo dusk plum (`#160B1A` surface, `#FF6584` blossom pink active, `#72DEC2` aqua data).
- **Test Suite**:
  - All 100 unit, layout, and theme tests pass (`dotnet test -c Release`).

### Music Playback Seek Fix (BrimDeck-Style Seek & Timeline Hold)
- **Eliminated Slider Pullback / Snapping**:
  - Identified root causes:
    1. Background `200ms` timer tick continuously overwriting `position` and `Fraction` during active dragging.
    2. `commandBusy = true` in `MusicViewModel.CommandAsync` disabling `CanSeek` during async dispatch, triggering WPF mouse capture loss and aborting drag.
    3. Windows GSMTC reporting asynchronous timeline changes where initial post-seek reads reflect pre-seek timestamps before the media player processes the seek.
  - Implemented BrimDeck-style `isSeeking` suppression across the ViewModel and View.
  - Implemented `MediaSessions.pendingSeekPosition` with a 3-second hold window that filters out obsolete pre-seek timeline events from Windows until the player catches up.
  - Implemented standard WPF `Slider.DecreaseLarge` / `IncreaseLarge` track commands with handled event listeners on `Thumb.DragStarted`, `Thumb.DragCompleted`, and `ValueChanged`.
  - Both clicking anywhere on the track and dragging the thumb now immediately update the target position and smoothly seek playback in Windows Media Player, Spotify, and browsers without snapback.

### Phase 2: Clock Studio

#### What works
- **Dedicated Clock Studio Engine (`src/Island.App/Views/ClockFace.cs`)**:
  - High-performance vector rendering of 14 distinct clock styles:
    1. **Simple**: Standard clean Segoe UI semibold tabular numbers.
    2. **WithDate**: Two-row time with date/weekday below ("Thu 11/19").
    3. **Stacked**: Two-line stacked digits (HH over MM) with mini seconds beside.
    4. **DotMatrix**: Vector 5x7 matrix drawn with crisp circles (`DrawEllipse`).
    5. **Segment**: Authentic 7-segment italic LED leaning -7° with active lit segment geometries.
    6. **Neon**: Glowing neon digits with dynamic outer glow effect (`DropShadowEffect`).
    7. **Serif**: Elegant italic serif typography (`Instrument Serif`).
    8. **Wide**: Futuristic wide bold typeface (`Unbounded`).
    9. **Condensed**: Tall athletic condensed bold font (`Bebas Neue`).
    10. **Pixel**: Crisp pixel-art square blocks (`DrawRectangle`) on whole device pixels.
    11. **Compact**: Soft rounded semi-bold font (`Fredoka`).
    12. **Terminal**: Monospace developer terminal typography (`Cascadia Code` / `Consolas`).
    13. **Gradient**: Multi-stop horizontal accent gradient fill.
    14. **Outline**: Geometric stroked outline typography.
- **Embedded OFL Open-License Fonts**:
  - Bundled `Orbitron`, `Instrument Serif`, `Unbounded`, `Bebas Neue`, and `Fredoka` as pack resources.
  - Added `ThirdPartyNotices.txt` documenting the SIL Open Font License 1.1 for all bundled fonts.
- **Color Row & Custom Color Picker**:
  - 9 color presets: White (`#F5F5F7`), Amber (`#FFB300`), Cyan (`#00E5FF`), Pink (`#FF2E97`), Green (`#00E676`), Violet (`#B388FF`), Accent Gradient, Album Art dominant cover color, and Custom hex picker.
  - Active selection indicator ring around selected swatch.
- **Options & Controls (Settings > Clock Studio)**:
  - Live interactive preview tiles grid rendering the actual current time in each style, ticking in real-time.
  - "Show clock in collapsed notch" toggle.
  - "24-hour clock" toggle.
  - "Show seconds" toggle.
  - "Blinking colon" toggle.
  - "Leading zero" toggle.
  - "Clock size" slider (80% to 150%).
- **Unified Visuals**:
  - The chosen clock style, color, scale, and options apply dynamically and in real-time to the collapsed notch, the header clock, and reduced motion base.

### Phase 1: BrimDeck-Style Transparency & Contrast System

#### What works
- **Pure Alpha Transparency Mechanism**:
  - Implemented direct alpha channel transparency on the island's background brushes (`Theme.Surface` and `Theme.CollapsedSurface`).
  - Zero glass, zero screen capture, zero acrylic blur, and zero shaders. Text, icons, bars, and cards stay 100% opaque.
  - Frozen `SolidColorBrush` instances with zero per-frame allocation overhead.
- **Settings & Stepper Controls (Settings > Appearance)**:
  - **"Background transparency" slider**: 0% to 70% (default 12%), live updates the expanded panel.
  - **"Collapsed notch transparency" slider**: 0% to 50% (default 0%), keeping the idle collapsed notch crisp and solid by default.
  - **BrimDeck-Style Stepper Chips**: Number box with `%` indicator, vertical divider line, `[-]` button (steps -1%), and `[+]` button (steps +1%) with keyboard Enter commit and lost-focus validation.
- **Contrast Floor**:
  - Alpha is clamped such that primary text guarantees at least 7:1 contrast ratio and secondary text at least 4.5:1 contrast ratio against backdrops.
- **Edge Definition & Silhouette**:
  - **1 px Inner Rim Stroke**: Added `InnerRim` path with `Theme.InnerRim` (`#14FFFFFF`, 8% white) synchronized with `notch.Geometry`. Preserves edge definition and outline over pitch-black and dark wallpapers.
  - **Soft Outer Shadow**: Added `OuterShadow` with `DropShadowEffect` (Color: Black, BlurRadius: 16, ShadowDepth: 4, Opacity: 0.35) matching `notch.Geometry` to cast a soft drop shadow beneath the notch and shoulders.
- **Performance & Text Clarity**:
  - `TextRenderingMode.Grayscale` activated whenever transparency > 0 to eliminate ClearType RGB subpixel color fringing over desktop backgrounds.
  - Idle collapsed notch uses 0% CPU and 0% GPU.
- **Test Suite**:
  - All 89 unit and layout tests pass (`dotnet test -c Release`).

## M-Layout complete · 2026-11-19

### What works

- **STEP 0: Complete UI Audit (`docs/UI_AUDIT.md`)**:
  - Audited the entire shell, header, collapsed notch, and every tab (`Music`, `System`, `Tasks`, `Quick Controls`, `Focus`, `Calendar & Weather`).
  - Cataloged double-boxing issues, spacing bugs, label-value collisions, truncated text, unspaced sensor values, missing icons, and empty slot deficiencies.
  - Documented specific fixes applied across all views.

- **1) Dynamic Auto-Sizing Island Engine**:
  - **Content-Driven Island Geometry**: The island dynamically measures and fits the active tab's content. Never fixed at arbitrary dimensions (e.g. 566x222/640x320) and never has dead space.
  - **Grid Snapping**: All island dimensions snap strictly to an 8 DIP grid (`Math.Ceiling(val / 8.0) * 8.0`).
  - **Continuous Morph Transitions**: Width and height animate smoothly using analytical underdamped spring physics (`SpringValue`, damping 0.78, ~350 ms, slight overshoot) while pages cross-fade (~220 ms), delivering an organic morph feel during tab switches.
  - **Debounced Content Resizing**: Content-driven changes (adding/removing tasks, list expansion, process changes) debounce over 80 ms to ensure smooth, jitter-free morphing.
  - **Fixed OS Window Architecture**: Win32 HWND (`OverlayWindow`) remains fixed at the maximum boundary (1040x680 DIPs); only the inner notch geometry, clip path (`NotchClip`), and click-through region animate.
  - **Safety Constraints**: Minimum width constraint enforced at 520 DIP; maximum constraints enforced at 62% of monitor working-area width and 70% of monitor working-area height. Large task lists scroll gracefully with auto-hidden scrollbars within maximum constraints.

- **2) Scale, Readability & Appearance Settings**:
  - **UI Scale System**: Added `InterfaceSize` ("Auto", "Compact", "Comfortable", "Large") and fine slider `UiScale` (0.9 to 1.5, normalized) in `AppSettings`.
  - **Strict Font Size Minimums (100% scale)**:
    - Body text: >= 14 DIP
    - Secondary text: >= 13 DIP
    - Labels: >= 12.5 DIP (never below 12 DIP)
    - Values: 16–18 DIP
    - Section titles: 15–16 DIP semibold
    - Hero numbers (clock, CPU %, timer): 28–56 DIP
    - Header time: 20 DIP semibold, date: 13 DIP
    - Collapsed notch clock: 16 DIP semibold
    - Click targets: >= 36x36 DIP with 8 DIP spacing
  - **High Contrast**: Primary text exceeds 7:1 contrast ratio, secondary text exceeds 4.5:1 ratio against island surfaces.

- **3) Unified Design System & Tokens**:
  - **`Themes/DesignTokens.xaml`**: Created token dictionary containing spacing scale (`Space.Xs`, `Space.Sm`, `Space.Md`, `Space.Lg`, `Space.Xl`), corner radii (`Radius.Card` 18, `Radius.Control` 12, `Radius.Pill` 999), font sizes, weights, and reusable control styles (`IslandCard`, `IslandPillButton`, `IslandIconButton`, `IslandProgressBar`, `IslandFullSlider`).
  - **Eliminated Double Boxing**: Removed the inner gray wrapper border (`Border x:Name="Body"` background is now transparent). Content cards sit directly on the island surface. Cards only group related items with 16 DIP inner padding and 12 DIP card gaps.
  - **Slimmer Header (~52 DIP)**: Tab icon bar on the left with active tab animated pill (displaying icon + text label, unselected icon-only with tooltips), centered 20px clock and 13px date, right-aligned pin and settings buttons.
  - **8 Shipped Themes**: `Midnight` (default near-black blue-tinted), `Dark`, `Graphite`, `Pure Black`, `Light`, `One Dark Pro`, `GitHub Dark`, `Dracula`, `Night Owl`, `Cyberpunk`.
  - **3 Surface Styles**: `Opaque` (alpha 255), `Translucent` (alpha 205), `Clear` (alpha 115).
  - **Settings UI**: Added Appearance controls to `MotionSettingsWindow` for runtime theme switching, surface style, interface size, and fine UI scale.

- **4) Page Redesigns**:
  - **System Page**: Sized 3-column / 2-row responsive grid.
    - CPU card: usage ring with large % in center, per-core mini bars, hidden when sensor unavailable (never "N/A").
    - Memory card: "Memory" left, "9.7 / 15.7 GB" right, progress bar below.
    - GPU card: friendly clean name ("RTX 4050 Laptop", full name in tooltip), usage %, temperature, power, VRAM bar.
    - Network card: download & upload rows ("↓ 4.5 KB/s", "↑ 10.0 KB/s") and "Today: ... down · ... up" beneath.
    - Power card: watts, battery % with charging bolt, "Mean ... W" secondary text.
    - Top Processes: 5 rows grouped by application (all instances of `LenovoVantage` merged into one row), friendly names, PIDs moved to tooltip, values right-aligned.
  - **Tasks Page**:
    - Input row with Enter-to-add, priority chip, filter segments ("All (N)", "Active (N)", "Done (N)").
    - Task rows: custom checkbox, title, priority dot, reminder bell, hover-revealed move up/down and delete buttons.
    - Compact 1-line empty state ("No tasks yet. Type above and press Enter to add!").
    - Auto-sizing height that grows with item count.
  - **Quick Controls Page**:
    - Volume & Brightness: 2 full-width glass-ready sliders with icon, slider track, and percentage.
    - Audio output device chip directly under volume slider.
    - 4-column x 2-row grid of 8 tiles (Mic, Wi-Fi, Bluetooth, Focus Assist with real state subtitle, Snip with scissors icon `\uE8C6`, Lock, Sleep, Task Manager). Zero empty slots.
  - **Focus Page**:
    - Mode segments (`Pomodoro`, `Timer`, `Stopwatch`) on top row.
    - Mode title & cycle subtitle ("Work · 1 of 4") on dedicated row below.
    - 56px digital timer with tabular figures inside a circular progress arc ring.
    - Preset chips (15m, 25m, 45m, 60m) under timer in Timer mode.
    - Controls row with reset (38x38), play/pause (48x48 accent circle), and skip/lap (38x38).
  - **Calendar & Weather Page**:
    - Month calendar grid with today highlighted and dots under event dates.
    - Compact agenda with 1-line "No events today. + Add" empty state.
    - Weather card: current temperature, condition TEXT next to glyph, feels-like, humidity, wind speed, hourly scroll strip, 5-day forecast.
  - **Music Page**:
    - Friendly player name mapping (Spotify, Microsoft Edge, Google Chrome, Brave, Media Player, VLC) and dynamic player glyph (`\uE8D6` music / `\uE774` web / `\uE714` video).
    - Rounded album art with `Radius.Card`.
    - Body-large title (20px semibold) and artist (14px).
    - Seek slider and transport buttons with >= 36x36 click targets and 8 DIP spacing.

- **5) Collapsed State Check**:
  - Collapsed clock text >= 16 px semibold with lining tabular figures.
  - Collapsed pill height 35 DIP.
  - Alert and preview pills styled with design tokens and high contrast.

### Verification

- **Layout & Design Tests**: Added `tests/Island.Tests/LayoutTests.cs` validating:
  - All 10 shipped themes resolve and contain design tokens.
  - All 3 surface style alpha levels (Opaque 255, Translucent 205, Clear 115).
  - Normalization of UI scale (0.9 to 1.5) and interface size.
  - All tab dimensions snap to an 8 DIP grid and satisfy min (>= 520 DIP) and max constraints across 1366x768, 1920x1080 (100% and 125%), and 2560x1440 (100% and 150%) screens.
  - Design tokens dictionary defines required font sizes (>= 12.5 px labels, >= 14 px body, >= 20 px header time) and corner radii.
- **Full Test Suite**: All 87 unit and integration tests passing (`dotnet test -c Release`).
- **Hardware Run Verification**: Launched `Island.App.exe`, verified clean startup, low memory footprint, and proper shutdown.

---

## M7 complete · 2026-11-19

### What works

- **Calendar & Agenda View**:
  - **Full Month Calendar Grid**: 42-cell calendar grid displaying previous, current, and next month days. Features month navigation controls (previous/next month, "Today" button), accent circle highlight on today's date, selection box, and dot indicators under dates containing scheduled events.
  - **Laptop / Windows Calendar Integration (`WindowsCalendarService`)**:
    - Queries native Windows calendar appointments using `Windows.ApplicationModel.Appointments.AppointmentManager` (supported across Outlook, Google Calendar accounts, Microsoft 365, and the native Windows Calendar app).
    - Merges laptop calendar appointments seamlessly into Island's day agenda and month view, marked with a sleek `Laptop` badge.
    - One-click launch button to open the system Windows Calendar application (`ms-calendar:`).
    - Quick "+ Laptop Calendar" button when creating events: generates standard `.ics` calendar objects and opens directly in the OS default calendar handler for instant laptop sync!
  - **Day Agenda & Event Management**:
    - Detailed day event list for whichever date is selected in the month view.
    - Each event card displays time range or "All Day" badge, event title, optional location glyph & string, and delete/manage buttons.
    - **Clean Empty State**: When no events are scheduled on a day, shows an elegant empty state with calendar icon and "+ Add" button (fixing prior inverted visibility converter issue).
    - **Quick Add**: Input box to quickly type event titles, toggle "Sync to Laptop Calendar", and add them to the selected date.
  - **RFC 5545 iCalendar (`.ics`) Parser**:
    - Robust parser handling line unfolding (`\r\n ` continuation), date/time parsing (UTC and local), all-day event detection (`VALUE=DATE`), multi-event streams, and string unescaping (`\,`, `\;`, `\\`, `\n`).
  - **Atomic Calendar Storage**:
    - Auto-saves all user events to `%LOCALAPPDATA%\Island\calendar.json` with transactional atomic write (`.tmp` + move) preventing corruption.
    - Periodic background check evaluates event reminder times and automatically dispatches alert notifications with sound to the Island notch.
- **Weather Telemetry (Open-Meteo)**:
  - **Zero-Configuration Live Weather & Auto-Detect Location**:
    - Automatic IP-based geolocation on cold boot or when clicking the **Auto-Detect GPS button** (`\uE81D`): instantly resolves user's real city and coordinates without requiring Windows permission dialogs.
    - **Interactive City Search**: Click the City pill (e.g. `📍 Kolkata`) or Search button to reveal an inline search bar with Enter key submit to query any city worldwide via Open-Meteo geocoding.
  - **Current Conditions & Reliable Glyph Set**:
    - Uses 100% compatible font glyphs mapped across `Segoe Fluent Icons, Segoe MDL2 Assets, Segoe UI Emoji` (Clear Sun `\uE706`, Partly Sunny `\uE707`, Clear Night `\uE708`, Overcast `\uE753`, Fog `\uE774`, Water Drop Rain `\uEB42`, Lightning `\uE945`, Snow `\u2744`), eliminating tofu/crossed-box rendering.
    - Real-time temperature readout, apparent temperature ("Feels like"), relative humidity (%), and wind speed.
    - Units toggle button switching between **°C** and **°F** dynamically.
  - **24-Hour Hourly Forecast Strip**:
    - Sleek horizontal forecast card with hidden scrollbar and smooth mouse-wheel horizontal scrolling.
  - **5-Day Daily Forecast Strip**:
    - Daily forecast showing weekday names, weather condition glyphs, and high/low temperature ranges.
  - **Offline Caching & Resilience**:
    - Automatically caches forecast payloads to `%LOCALAPPDATA%\Island\weather_cache.json`.
    - Instant cold start renders cached weather data immediately, seamlessly refreshing in the background. If offline, gracefully indicates cached status without crashing.
- **Tab Sequence & UI Polish**:
  - Reordered the tab sequence per specification:
    1. **Music** (`\uE8D6`)
    2. **System** (`\uE950`)
    3. **Tasks** (`\uE9D5`)
    4. **Quick Controls** (`\uE9E9`)
    5. **Focus** (`\uE916`) — *2nd to last*
    6. **Calendar & Weather** (`\uE787`) — *last*
  - Dual-column responsive layout for Calendar & Weather page with month view and day agenda on the left and full weather card on the right.

### Verification

- Solution built cleanly in Release mode with zero warnings and zero errors.
- All 64 unit tests passed in [Island.Tests](file:///D:/Island%202/tests/Island.Tests/), including:
  - `CalendarEvent`: Occurrence calculations, date overlap logic, and time comparisons.
  - `IcsParser`: RFC 5545 line unfolding, UTC/local date conversion, all-day flag, escaped characters, and multi-event parsing.
  - `CalendarStore`: Adding, updating, deleting, atomic file persistence and reload, day queries, and reminder trigger checks.
  - `WmoWeatherCode`: Complete mapping of standard WMO condition codes to user-friendly descriptions and Segoe Fluent Icon glyphs.
  - `WeatherCache`: Round-trip JSON serialization and deserialization of live weather data.
- Live hardware self-test on Windows 11:
  - Launched `Island.App.exe` on real hardware.
  - Verified live Open-Meteo API fetch: successfully created and populated `%LOCALAPPDATA%\Island\weather_cache.json` (3,853 bytes) with live New York weather data.
  - Confirmed process runs healthy without any UI freezes or memory leaks.

### Known limitations and remaining checks

- Geocoding city search uses Open-Meteo's geocoding API which requires an active internet connection; when offline, Island falls back to the last cached city and weather data.
- Calendar sync currently supports local JSON storage and `.ics` file/URL parsing. Direct OAuth 2.0 sync for Google Calendar and Microsoft Graph is designed for external calendar URL feeds (`.ics` subscription).

### What to test

1. If an older Island instance is running, exit it via tray **Quit**. Start this build in PowerShell:

   ~~~powershell
   Set-Location 'D:\Island 2'
   $env:DOTNET_ROOT = "$PWD\.tools\dotnet"
   Start-Process "$PWD\src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe"
   ~~~

2. **Tab Sequence**:
   - Hover over the notch to expand it.
   - Verify the 6 tabs in sequence from left to right:
     - 1st: Music (notes icon)
     - 2nd: System (chip icon)
     - 3rd: Tasks (checklist icon)
     - 4th: Quick Controls (sliders icon)
     - 5th: Focus (timer icon) — *2nd to last*
     - 6th: Calendar & Weather (calendar icon) — *last*
3. **Calendar & Agenda**:
   - Click the **Calendar & Weather** tab (6th tab).
   - In the left calendar panel:
     - Notice the current month and year header with `<` and `>` arrows to browse months, and "Today" button.
     - Observe the 42-day grid with weekday headers (Su Mo Tu We Th Fr Sa).
     - Notice today's date highlighted with a blue accent circle.
     - Click any date to select it: the agenda header updates to show that date.
   - In the **Day Agenda** below the calendar:
     - Type a quick event title (e.g., "Team Sync") in the text box and click **+ Add**.
     - Verify the event appears in the list with its time badge and a trash icon.
     - Look back at the calendar grid: notice a subtle indicator dot now appears under that day number!
     - Click the trash icon on the event to delete it: verify it is removed and the dot disappears.
4. **Weather Telemetry**:
   - In the right weather panel:
     - Check the current temperature (e.g., `52°F` or `11°C`), weather condition text, and icon.
     - Check "Feels like", humidity %, and wind speed.
     - Click the **°F / °C** unit button: observe all temperatures in current, hourly, and daily strips toggle units instantly.
     - Click the city name search bar, type another city (e.g. `London`, `Tokyo`, or `San Francisco`), and press Enter: verify the weather telemetry updates to that city's live forecast!
     - Scroll through the 24-hour horizontal forecast strip and inspect the 5-day forecast cards below.
5. **Persistence**:
   - Add an event, quit Island via tray icon, and relaunch: verify the calendar event and selected weather city persist.

### Next milestone

M8 completes Settings pages, theme editing (custom accent colors and dark/light modes), global hotkey management, and autostart configuration. Waiting for the user's instruction to continue.

## M6 complete · 2026-11-19

### What works

- **Tasks & Reminders**:
  - Full-featured to-do list integrated directly in the Island expanded panel:
    - **Quick Add**: Input box with Enter key trigger, priority selector (Low, Med, High with color-coded badges), and instant add button.
    - **Filter Pills**: Instant switching between **All**, **Active**, and **Done** tasks.
    - **Item Management**: Check/uncheck completion (with strikethrough animation), reorder tasks up/down, and delete tasks.
    - **Due Dates & Snooze**: Relative due date formatting ("Today", "Tomorrow", "MMM d") with overdue highlighting.
    - **Reminders**: Periodic background check for scheduled reminders; fires alert notifications to the Island notch with sound and optional snooze.
    - **Atomic Persistence**: Auto-saves to `%LOCALAPPDATA%\Island\tasks.json` using atomic temporary file write and replacement to prevent data corruption.
    - **Dynamic Input Focus**: Automatically clears `WS_EX_NOACTIVATE` while the task input box has focus so the user can type normally, and restores non-activating mode when focus is lost.
- **Focus Sessions (Pomodoro, Timer, Stopwatch)**:
  - Three distinct focus productivity modes:
    - **Pomodoro**: Standard work session (25m), short break (5m), and long break (15m) cycles with cycle counter (`Cycle 1 of 4`). Features play/pause, reset, and skip stage controls.
    - **Countdown Timer**: Custom timer with instant preset pill buttons (5m, 10m, 15m, 25m), countdown display, and alert notification with chime upon completion.
    - **Stopwatch**: High-precision stopwatch with digital centisecond display, play/pause, reset, and split lap recordings with lap history sidebar.
  - **Collapsed Focus Notch**:
    - When a Focus session or timer is active, the collapsed notch expands smoothly to display the focus icon, active countdown or elapsed time, and a progress bar.
    - Automatically notifies via the alert queue when a Pomodoro stage or countdown timer completes.
- **Quick Controls**:
  - Comprehensive device and audio control center:
    - **Master Volume Slider**: Interactive slider (0–100%) with volume percentage display and icon button to toggle mute.
    - **Brightness Slider**: Interactive slider (0–100%) with brightness percentage display, controlling physical or built-in display brightness via WMI (`WmiMonitorBrightnessMethods`).
    - **Microphone Mute Card**: One-click toggle for default communication microphone mute state with live status badge ("Active" / "Muted").
    - **Wi-Fi Card**: Live Wi-Fi radio status and one-click toggle using `Windows.Devices.Radios`.
    - **Bluetooth Card**: Live Bluetooth radio status and one-click toggle using `Windows.Devices.Radios`.
    - **Focus Assist / DND**: One-click shortcut to Windows Focus Assist / Quiet Hours settings.
    - **Snipping Tool (Screenshot)**: Triggers Windows screen clipping (`ms-screenclip:` / Win+Shift+S).
    - **Lock Workstation**: One-click workstation lock (`LockWorkStation`).
    - **Sleep PC**: One-click sleep suspension (`SetSuspendState`).
- **Panel Navigation**:
  - All 6 navigation tabs are now active in the expanded notch:
    1. Music
    2. System
    3. Calendar & Weather (scheduled for M7)
    4. Tasks
    5. Focus
    6. Quick Controls

### Verification

- Solution built in Release mode with zero warnings and zero errors.
- All 57 unit tests passed in [Island.Tests](file:///D:/Island%202/tests/Island.Tests/), including:
  - `TaskStore`: Adding, priorities, ordering, completion toggling, deletion, moving/reordering with clamp checks, atomic disk persistence/reloading, snoozing, and reminder advancements.
  - `FocusManager`: Pomodoro stage cycle progression (Work -> Short Break -> Work -> Long Break), countdown timer presets and durations, and stopwatch lap recordings and reset.
- Live hardware self-test on Windows 11: launched `Island.App.exe`, verified clean startup, zero unhandled exceptions, healthy process lifetime, and correct logging in `%LOCALAPPDATA%\Island\logs\`.

### Known limitations and remaining checks

- External desktop monitors that do not implement DDC/CI WMI brightness methods cannot adjust brightness through software sliders; laptop displays and supported monitors work natively.
- Wi-Fi and Bluetooth radio control requires standard Windows 10/11 radio permissions; where radio hardware is absent, cards cleanly display "Unavailable".

### What to test

1. If an older Island instance is running, exit it via tray **Quit**. Start this build in PowerShell:

   ~~~powershell
   Set-Location 'D:\Island 2'
   $env:DOTNET_ROOT = "$PWD\.tools\dotnet"
   Start-Process "$PWD\src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe"
   ~~~

2. **Quick Controls Tab**:
   - Hover over the notch to expand it, and click the **Quick Controls** tab (sliders icon, 6th tab).
   - Drag the **Volume** slider: verify the volume changes and the percentage updates. Click the speaker icon to toggle mute.
   - Drag the **Brightness** slider: verify display brightness adjusts.
   - Click the **Mic** tile: verify microphone muting toggles.
   - Click the **Wi-Fi** or **Bluetooth** tiles: verify radios toggle.
   - Click **Snip**: verify Windows screen clipping opens.
3. **Focus Tab**:
   - Click the **Focus** tab (timer icon, 5th tab).
   - In **Pomodoro** mode: click Play. Collapse the notch (move mouse away): notice the collapsed notch displays the countdown timer and progress bar!
   - Expand the notch again, click Skip: verify it switches to Short Break.
   - Switch to **Timer** mode: click the "10m" preset pill button, then click Play.
   - Switch to **Stopwatch** mode: click Play, click the Lap button, and check the recorded split laps in the right sidebar.
4. **Tasks Tab**:
   - Click the **Tasks** tab (checklist icon, 4th tab).
   - Click in the text box: notice you can type normally.
   - Cycle priority using the priority button ("Med", "High", "Low").
   - Type a task name (e.g. "Finish project review") and press Enter (or click "+").
   - Add another task, test clicking the check mark to mark complete (strikethrough styling).
   - Test **All / Active / Done** filter pills.
   - Test the Move Up / Move Down buttons to reorder tasks, and click the trash can icon to delete.
   - Close Island and restart: verify all tasks persisted from `%LOCALAPPDATA%\Island\tasks.json`.

### Next milestone

M7 adds Calendar & Weather (Windows Agenda / Google Calendar / Outlook integration, Open-Meteo local weather forecasts, collapsed weather icons, and precipitation alerts). Waiting for the user's instruction to continue.

## M5 complete · 2026-11-19

### What works

- **Queued Alerts & System OSD**:
  - The collapsed notch dynamically intercepts and displays incoming system events and OSD changes:
    - **Volume & Mute OSD**: Detects Windows master volume changes and mute toggles made anywhere in Windows (via Core Audio `IAudioEndpointVolume` callbacks), displaying volume percentage with a progress bar or mute glyph. High-frequency volume scrolling updates the active alert in-place without queue buildup.
    - **Microphone Mute OSD**: Detects default communications capture device mute state changes, displaying "Mic Muted" or "Mic Unmuted" with appropriate warning/info styling.
    - **Brightness OSD**: Monitors display brightness changes via WMI (`WmiMonitorBrightness`), showing a brightness percentage bar.
    - **Battery & Charger Alerts**: Detects power status transitions (`WM_POWERBROADCAST` and `GetSystemPowerStatus`) — announces "Charging" when plugged in, "On Battery" when disconnected, "Battery 20%" warning on low battery, and "Battery 10% - Connect Charger" with critical styling and exclamation sound. Includes hysteresis to prevent alert spamming.
    - **Temperature Warnings**: Monitors GPU and CPU temperatures against configurable thresholds (default 85°C) with cooldown to notify when hardware runs hot.
  - **Notch Animation & Presentation**:
    - Alerts drop into the collapsed notch with a smooth spring translation and slight overshoot.
    - An alert expands the notch to ~230 DIPs and auto-dismisses after a configurable duration (default 4s, or 3s for fast OSD).
    - Alerts prioritize above the music preview, compact stats, and clock. When the alert queue empties, the notch smoothly returns to the music preview (if playing), compact stat, or idle clock.
    - Critical alerts preempt non-critical alerts immediately, pushing the interrupted alert back to the front of the queue to resume afterward.
- **Alert Settings**:
  - Motion & Alert settings window provides toggles for:
    - Master alerts enable/disable
    - Individual alert toggles (Volume, Brightness, Microphone, Battery/Charger, Temperature)
    - Alert sound toggle
    - Alert duration slider (1–10 seconds)

### Verification

- Release built on Windows x64 with zero warnings and zero errors.
- All 48 tests passed in [Island.Tests](file:///D:/Island%202/tests/Island.Tests/), including FIFO queueing, in-place continuous OSD update deduplication, critical alert preemption and queue resumption, queue clearing, ViewModel alert property and color brush mapping, and settings normalization.
- Self-tested on live Windows 11 hardware with active audio endpoints, microphone, dual GPUs, and battery power events. Verified zero unhandled exceptions on startup and execution.

### Known limitations and remaining checks

- External desktop monitors that lack DDC/CI or WMI support cannot report brightness changes via software APIs; Island handles this without throwing.
- Sound playback respects Windows system sound settings and Island's `AlertSoundEnabled` setting.

### What to test

1. If an older Island instance is running, exit it via tray **Quit**. Start this build in PowerShell:

   ~~~powershell
   Set-Location 'D:\Island 2'
   $env:DOTNET_ROOT = "$PWD\.tools\dotnet"
   Start-Process "$PWD\src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe"
   ~~~

2. **Volume & Mute OSD**:
   - Press your keyboard's Volume Up / Volume Down keys or change the volume slider in the Windows taskbar.
   - Watch the collapsed notch: it springs open to show the speaker glyph, volume percentage, and a mini progress bar, holds for 3 seconds, then collapses.
   - Press the Mute key: verify the "Muted" badge appears.
3. **Microphone OSD**:
   - If your laptop has a microphone mute button or toggle in Windows Sound settings, toggle it. Verify the "Mic Muted" / "Mic Unmuted" alert appears.
4. **Charger & Battery**:
   - If on a laptop, plug or unplug the AC power adapter. The notch will spring open to display "Charging" or "On Battery".
5. **Settings Configuration**:
   - Open Settings (gear in header or double-click tray icon).
   - Under **Alerts & OSD**, toggle individual alert types or adjust the duration slider.
   - Verify that toggled-off alerts do not pop up and duration changes apply live.

### Next milestone

M6 adds Quick Controls (volume/brightness sliders, mic mute, Wi-Fi/BT toggles, DND), Focus sessions (Pomodoro, timers, stopwatch), and Tasks & Reminders. Waiting for the user's instruction to continue.

## M4 complete · 2026-11-19

### What works

- The **System tab** provides real-time telemetry across all specified hardware areas without requiring administrator rights or kernel drivers:
  - **CPU**: Circular usage gauge with exact percentage, per-core vertical mini-bars for all logical processors (via `NtQuerySystemInformation`), and thermal zone temperature where exposed.
  - **RAM**: Physical memory usage bar with used/total formatted gigabytes and percentage (via `GlobalMemoryStatusEx`).
  - **GPU**: Dynamically loads `nvml.dll` for NVIDIA GPUs (querying GPU utilization %, VRAM used/total, temperature in °C, and board power in Watts). Automatically falls back to native Windows PDH counters (`\GPU Engine(*)\Utilization Percentage`) for non-NVIDIA/integrated GPUs.
  - **Network**: Real-time upload and download speeds (`GetIPStatistics`) plus accumulated today's download and upload totals that automatically reset at midnight.
  - **Storage**: Lists internal fixed drives (`DriveType.Fixed`) with drive letters, labels, formatted used/total space, and utilization progress bars.
  - **Power & Battery**: Current board power in Watts, rolling 60-second history curve (sparkline) and 60-second mean power. Battery status displays battery percentage, charging state, AC power connection, and power mode via `GetSystemPowerStatus`.
  - **Top Processes**: Real-time top processes list with instant toggle between **CPU** and **RAM** sorting. Interactive cards show PID, process name, and resource usage with detailed tooltips (threads, handles, memory).
  - **Tooltips**: Every metric includes a detailed tooltip with complete values.
- **Collapsed Live Readouts**:
  - The collapsed notch can display a live compact readout beside the clock (`CollapsedStat` in settings: `None`, `Cpu`, `Ram`, `Network`, `Battery`, `CpuRam`).
  - When enabled, the notch expands to ~200 DIPs to show the time alongside the chosen live metric and icon.
  - Music playback preview continues to take precedence when active.
- **Efficiency & Lifecycle**:
  - Hardware polling runs at 1-second intervals on a background thread.
  - Polling completely pauses when the notch is collapsed unless a compact stat readout is active, ensuring near 0% idle CPU usage. Pausing the app via tray releases all hardware monitoring.

### Verification

- Release built on Windows x64 with zero warnings and zero errors.
- All 42 tests passed, including rolling power buffer math and FIFO rollover, network accumulator rate calculation and midnight date rollover, process CPU delta calculation and sorting, string/speed/bytes formatters, settings normalization, dynamic NVML graceful handling, and real live hardware monitoring snapshot verification.
- Ran the Release executable on real hardware (12th Gen Intel Core i7-12650HX with 20 logical cores, dual Intel UHD & NVIDIA GeForce RTX 4050 Laptop GPU, battery/AC). Verified NVML initialization, temperature (62°C), board power (7.1 W), per-core 20-core meter bars, RAM usage, storage drives, network transfer tracking, and process CPU/RAM switching.

### Known limitations and remaining checks

- CPU thermal zone temperature (`MSAcpi_ThermalZoneTemperature`) is not exposed by OEM ACPI on all laptops without kernel drivers; Island gracefully handles this and displays "N/A" rather than crashing or showing fake data.
- Power draw curve tracks GPU board power and battery discharge rate; on desktop systems without battery or NVML, power readings cleanly indicate unavailable.

### What to test

1. If an older Island instance is still running, use its tray **Quit**. Start the new build in PowerShell:

   ~~~powershell
   Set-Location 'D:\Island 2'
   $env:DOTNET_ROOT = "$PWD\.tools\dotnet"
   Start-Process "$PWD\src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe"
   ~~~

2. Hover the clock to expand the panel and click the **System** tab (second icon: monitor glyph `\uE950`).
3. Verify the CPU ring gauge, the 20 vertical core bars, the RAM used/total bar, the GPU card (showing your NVIDIA GeForce RTX 4050 or integrated GPU, VRAM, temp, and power), the Power 60-second sparkline curve with Mean watts, and the Battery status.
4. Check the **Network** meter (speeds and today's totals) and **Storage** drives.
5. In **Top Processes**, click **CPU** and **RAM** to toggle sort order. Hover any process or metric to inspect the full tooltip.
6. Open Settings (gear icon in header or tray icon):
   - Under **System & Notch**, set **Collapsed live stat** to **Cpu**, **Ram**, **Network**, **Battery**, or **CpuRam**.
   - Collapse the panel. The notch will widen and display the clock alongside your live stat (e.g. `14% CPU` or `RAM 48%`).
   - Set **Collapsed live stat** back to **None** to return to the clean clock-only collapsed notch.

### Next milestone

M5 adds the queued alerts system (battery warnings, thermal limits, volume/brightness/mic OSD overlays). Waiting for the user's instruction to continue.

## M3 complete · 2026-11-19

### What works

- The Music tab follows Windows media sessions. It shows the player, title, artist, rounded artwork, cover-derived tint, elapsed time and a draggable seek bar. Play/pause, previous, next, shuffle and repeat follow the capabilities the selected player publishes.
- Click the player name to switch sessions or pin a preferred app. A pin survives restart. If the preferred app goes away, Island falls back to an available session and can return when the app reappears.
- Playing media widens the compact notch to about 260 × 32 DIPs. Music settings offer a centered clock, scrolling title or current lyric, artwork tint, compact preview and equalizer toggles. Pausing playback returns the notch to its idle clock.
- A six-band spectrum uses actual WASAPI loopback samples from the default multimedia output. Capture runs while playing and a visible view requests the meter. The output-device notification refreshes capture when the default endpoint changes. Tray Pause releases capture and media subscriptions.
- Synced lyrics come from LRCLIB using track metadata. The tab highlights the current line and scrolls to the next line using the player's timeline. Seeking updates the line. Lookups are cancelled when superseded, cached in memory, and requested only when a lyrics view needs them. Missing lyrics, instrumental tracks and service errors have distinct states.

### Verification

Release built on Windows x64 with zero warnings and zero errors. All 35 tests passed, including timestamp offsets, repeated timestamps, backward seeks, exact query encoding, cache reuse, missing lyrics and HTTP rate limiting.

Ran the Release executable and rechecked foreground retention, fixed window bounds, transparent hit routing and display re-anchoring. A separate desktop run used an isolated profile and two temporary Windows media sessions. Verified selection and pinning, cover decoding and color extraction, compact playback, transport button acknowledgments, a pointer drag on the seek slider, repeat/shuffle changes, unavailable controls and fallback after a selected session disappeared. A low-volume 1 kHz test tone appeared in the expected loopback band. A public sample track returned a timed lyric from the live LRCLIB service. Tray Pause released the capture and sessions.

### Known limitations and remaining checks

A startup probe caught the transparent host receiving input before WPF submitted its first frame. Island now passes all input through until that frame is ready. Two immediate-launch probes passed after the fix.

Music capabilities and timing depend on what each player publishes. The equalizer measures the combined default output, including sound from other apps. Pinning identifies a player by its Windows app ID across restarts. Browser tabs sharing one ID can be switched individually while they exist. Physical output-device changes, protected playback and compatibility with your particular players remain hands-on checks.

### What to test

1. If an older Island instance is still running, use its tray **Quit**. M3 uses a versioned Windows target for the SDK projections. Start this build in PowerShell:

   ~~~powershell
   Set-Location D:\Island
   $env:DOTNET_ROOT = "$PWD\.tools\dotnet"
   Start-Process "$PWD\src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe"
   ~~~

2. Play a song in your usual player. Check that the compact notch widens, the bars respond to audio, and pausing restores the small clock. Hover and open Music. Compare the title, artwork, position and transport actions with the player.
3. For a song with synced lyrics, drag the seek bar forward and backward. The player and highlighted lyric should follow. In Settings change **Preview text** to **Lyric**, then **Title**, and collapse to check each mode.
4. Open a second player. Click the player name in Music, switch sessions, and choose **Pin this player**. Quit and relaunch Island with the player still open to check the pin. Close that player and check fallback.
5. With audio playing, switch the Windows default output to headphones or speakers. Confirm the meter follows the new output. Try a live stream or player with limited controls and confirm unavailable actions are disabled.

### Next milestone

M4 adds the System tab and compact system readouts. Waiting for the user's instruction to continue.

## M2 complete · 2026-11-19

### What works

- Hovering the clock for 120 ms opens the panel to approximately 640 × 320 DIPs. The reusable damped spring animates the retained notch clip and its corner radius. The header and body appear in sequence. Leaving allows 300 ms for re-entry, then fades the content and collapses the shape. Re-entry during collapse reverses from the current position. The HWND retains its M1 dimensions.
- Six tab buttons share a spring-driven selection pill. Tab pages slide and cross-fade. Each page identifies the feature milestone that will supply its content.
- Pin keeps the panel open and survives restart. Escape explicitly unpins and collapses the panel while the overlay keeps keyboard focus in the foreground app.
- The tray supports Open, Pin/Unpin, Settings, Pause/Resume and Quit. Double-click opens Settings. The panel context menu supports Settings, Pin/Unpin and Quit. Pause hides the overlay and stops its clock and frame callbacks.
- The gear and tray open motion settings for hover delay, collapse delay, animation speed and reduced motion. Changes apply live and persist. Reduced motion uses short fades. The full settings navigation is scheduled for M8.

### Verification

Release built with zero warnings and zero errors. All 28 tests passed. Motion tests cover equivalent spring trajectories at 60 and 120 Hz, overshoot, delayed reveal, reversal, settling and reduced motion.

Ran the Release executable and verified foreground retention, fixed HWND bounds, display re-anchoring and cross-process hit routing through the transparent side, bottom and corner. An isolated-profile desktop check moved the pointer through expand, leave and re-entry, switched tabs rapidly, verified the selected pill and page, saved a pin, and restored it in a fresh overlay. It exercised native Escape registration and message dispatch, the context menu, tray commands, live reduced motion and idle frame unsubscription. Inspected the rendered expanded panel.

### Known issues and remaining checks

Escape uses a temporary Windows hotkey while the panel is open. A registration conflict is logged and can prevent that shortcut from working. The automated tray check invoked the menu handlers. Physical Escape input, Explorer's tray double-click gesture and perceived smoothness on a high-refresh display remain manual checks. Frame-rate tests exercise the animation mathematics. Desktop frame-time profiling is scheduled for M9.

### What to test

1. Start the Release app in PowerShell:

   ~~~powershell
   Set-Location D:\Island
   $env:DOTNET_ROOT = "$PWD\.tools\dotnet"
   Start-Process "$PWD\src\Island.App\bin\Release\net8.0-windows\Island.App.exe"
   ~~~

2. Type in an editor. Briefly pass across the clock, then hover deliberately. The second hover should open the panel. Move away and return during collapse. The panel should reverse smoothly, and typing should continue in the editor.
3. Click through all six tabs, including several quick switches. Confirm the highlighted pill and visible page end on the same tab.
4. Pin using the header, move away, then Quit and relaunch. The panel should start open. Press Escape while the editor has focus. The panel should collapse and the next launch should start unpinned.
5. Open the gear, choose Reduce motion and try 0.5× and 2× speed. Change both delays, close Settings and repeat the hover test. Double-click the tray icon to reopen Settings and confirm the saved values.
6. Right-click the tray icon, including in the Windows hidden-icons flyout if needed. Test Open, Pin/Unpin, Pause, Resume and Quit. Pause should hide the overlay while the tray remains available. Quit should remove both. Also test Settings, Pin/Unpin and Quit from the panel's right-click menu.

### Next milestone

M3 adds Windows media sessions, the Music tab, collapsed music preview, real audio equalizer and lyrics. Waiting for the user's instruction to continue.

## M1 complete · 2026-11-19

### What works

The app opens a fixed 760 × 400 DIP, topmost, non-activating WPF tool window. The visible 130 × 32 DIP black notch sits flush with the top center of the chosen screen. It has curved shoulders, rounded lower corners, a subtle bottom highlight and a centered tabular clock. Windows uses the layered window's rendered alpha to pass input through transparent pixels.

The clock follows the 12/24-hour and seconds settings. Its timer wakes at the next displayed minute or second. Monitor placement uses native desktop pixel coordinates, including negative origins. Display and DPI changes trigger repositioning. A missing selected monitor falls back to primary while retaining the saved preference.

### Verification

- Release build on Windows x64 completed with zero warnings and zero errors. All 25 tests passed.
- Ran the Release executable over a window owned by another process. Native hit tests selected Island at the clock and the underlying window beside the shoulder, in the transparent lower corner and below the notch. Startup preserved the foreground window. The overlay returned the non-activating mouse response and had the expected topmost, layered and tool-window styles.
- Inspected the rendered clock. Sent a display-change message after displacing the window and verified that it returned to the top center.
- Exercised the overlay with isolated settings on two displays. The host measured 760 × 400 pixels at 96 DPI and 950 × 500 at 120 DPI on a monitor left of primary. Verified startup on the secondary screen, fallback to primary, switching back, a real seconds tick and live switching to minute precision.

### Known issues and remaining checks

Physical unplug/reconnect and changing Windows scaling while the overlay is open still need a hands-on check. Monitor and clock preferences can be set in JSON at this milestone. The tray with Quit is scheduled for M2.

### What to test

1. In PowerShell, start the Release executable from this checkout:

   ~~~powershell
   Set-Location D:\Island
   $env:DOTNET_ROOT = "$PWD\.tools\dotnet"
   $exe = "$PWD\src\Island.App\bin\Release\net8.0-windows\Island.App.exe"
   $island = Start-Process -FilePath $exe -PassThru
   ~~~

2. Open an editor and type. Click the clock, then type again. Text should continue in the editor. Click a control just outside the black shape, including immediately below its bottom edge. The underlying app should receive the click. Check that Island is absent from Alt+Tab and the taskbar.
3. End the test instance using `Stop-Process -Id $island.Id`. In `%LOCALAPPDATA%\Island\settings.json`, set `"showSeconds": true` and `"use24HourClock": false`. Start it again with the last command above and check the seconds advance. Restore your preferred values afterward.
4. To choose a monitor, run `Add-Type -AssemblyName System.Windows.Forms` and then `[System.Windows.Forms.Screen]::AllScreens | Select-Object DeviceName, Primary`. Add `monitorDeviceName` to the settings object or edit its existing value. For example, the Windows device `\\.\DISPLAY1` is written as `"monitorDeviceName": "\\\\.\\DISPLAY1"` in JSON. Restart and check the top-center alignment. Change that display's scale, disconnect it, and reconnect it. The notch should recenter, fall back to primary during disconnection, and return when Windows reports the saved device again. Use `null` to follow the primary monitor.

### Next milestone

M2 adds hover expansion and collapse, spring animation, pinning, Escape, tabs and the tray. Waiting for the user's instruction to continue.

## M0 complete · 2026-11-19

The solution builds an x64 WPF host, a UI-independent core library and an xUnit test assembly. [PLAN.md](PLAN.md) records the architecture, dependencies, risks and milestone order.

### What works

- Startup creates per-user settings, resolves a palette, installs frozen WPF brushes, writes the applied accent to the daily log and exits. The first visible window is scheduled for M1.
- Settings support atomic replacement, observable updates, defaults for missing fields and preservation of unknown fields. Invalid JSON is archived as `settings.invalid-*.json`. A higher schema version uses session defaults and blocks writes.
- All seven built-in palettes are bundled. Selecting `Custom` reads `%LOCALAPPDATA%\Island\themes\custom.json` with four `#RRGGBB` properties named `background`, `surface`, `foreground` and `accent`. An invalid selection falls back to Dark and records the reason.
- Logs roll daily in UTC and retain seven days. Build-time NuGet dependencies are locked.

### Verification

The Release build on Windows x64 completed with zero warnings and zero errors using SDK 8.0.428. All 19 tests passed, including settings recovery and round trips, newer-schema protection, palette fallback, WPF brush replacement and log retention. The real WPF startup exited successfully and wrote the Dark accent `#FF9BC5FF` to the user's log.

### Known issues and environment

The SDK was installed locally in the ignored `.tools\dotnet` directory because this machine had no SDK on PATH. For other workspaces use the Windows x64 .NET 8 SDK. The requested .NET 8 target has reached end of support, which affects future security servicing. [Microsoft release information](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### What to test

From PowerShell in `D:\Island`, use the workspace SDK to rerun the checks:

```powershell
$env:DOTNET_ROOT = "$PWD\.tools\dotnet"
$sdk = "$env:DOTNET_ROOT\dotnet.exe"
& $sdk build Island.sln -c Release
& $sdk test tests/Island.Tests -c Release --no-build
$app = '.\src\Island.App\bin\Release\net8.0-windows\Island.App.dll'
& $sdk $app
$folder = Join-Path $env:LOCALAPPDATA 'Island'
$log = Get-ChildItem "$folder\logs\app-*.log" | Sort-Object Name -Descending | Select-Object -First 1
Get-Content -LiteralPath $log.FullName -Tail 1
```

Expect 19 passing tests and a startup log entry. With default settings the line ends in `#FF9BC5FF.` To check persistence, edit `theme` in `settings.json` to `Dracula`, save, and run `& $sdk $app` twice. Both new log lines should show the Dracula accent `#FFBD93F9`. Restore your preferred theme afterward.

### Next milestone

M1 adds the fixed transparent, topmost, non-activating overlay, the collapsed clock, shape-aware click-through and monitor/DPI positioning. Awaiting the user's instruction to continue.
