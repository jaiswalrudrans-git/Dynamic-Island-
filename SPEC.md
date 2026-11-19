You are building a production-quality Dynamic Island for Windows 11, similar in concept to BrimDeck (github.comMoFeng2223BrimDeck), but with better visuals, smoother animation, and extra everyday features. First read this entire spec and write a short plan (architecture, packages, risks). Then build it milestone by milestone as defined at the end. After each milestone build, run, self-test, commit to git, and STOP and tell me exactly what to test. Do not skip ahead.

=== TECH STACK ===
- C# 12, .NET 8, WPF, MVVM (CommunityToolkit.Mvvm), Windows 11 x64 (Windows 10 2004+ best effort).
- Packages NAudio (audio levels, volume, mic), Microsoft.Windows.SDK.NET (WinRT mediaradios via Windows.Media.Control), LibreHardwareMonitor NOT allowed (needs admindriver). Use PDH performance counters, WMI, NVML (load nvml.dll dynamically; app must work without NVIDIA), Win32 PInvoke.
- No administrator rights, no kernel drivers. Per-monitor-v2 DPI aware.
- Storage JSON in %LOCALAPPDATA%AppName (settings, tasks, reminders, events). Logs to a rolling file.
- Network only for Open-Meteo (weather), LRCLIB (lyrics), cover art from the media session. No telemetry.
- App name use Island as placeholder in one constant so I can rename later.

=== WINDOW ARCHITECTURE ===
- One borderless, transparent, topmost window, tool-window style (no taskbar entry, not in Alt+Tab), WS_EX_NOACTIVATE so it never steals focus (temporarily allow activation only while typing in a text box such as adding a to-do).
- Use ONE fixed-size window (e.g. 760x400 DIPs) anchored to top-center of the chosen monitor. NEVER resize the OS window to animate. Animate only the inner island shape using RenderTransform, Clip geometry, and Opacity (GPU-composited). Avoid layout-driven animation and per-frame allocations. Target locked 60 fps, and 120 fps on high-refresh displays; idle CPU near 0%.
- Click-through everything outside the visible island shape must pass clicks to apps underneath. Update the window hit-test region in sync with the animated shape every frame (or use a custom hit-test with WM_NCHITTEST returning HTTRANSPARENT).
- Multi-monitor choose monitor in settings (default primary); reposition on display or DPI changes.
- Fullscreen gamesvideo auto-hide (SHQueryUserNotificationState and window-rect check), configurable.
- When any window is maximized optionally shrink to a thin indicator line (setting).

=== THE NOTCH VISUAL DESIGN ===
- Shape true black (#000) notch attached flush to the top edge of the screen, with inverted (outward-curving) top shoulders so it looks like it grows out of the bezel, and rounded bottom corners. Subtle 1px inner highlight on the bottom edge and a very soft shadow below when expanded. Anti-aliased, crisp at all DPI scales.
- Collapsed (idle) size about 130x32 DIP, shows ONLY the current time centered (HHmm, 1224h setting, optional seconds, tabular figures so digits never jitter). Nothing else.
- Collapsed with music playing widens slightly (about 260x32) tiny rounded album cover at left, animated equalizer bars at right (driven by real audio level via WASAPI loopback, not fake), time stays centered or hidden per setting; optionally scrolling title or current lyric line.
- Collapsed alerts the notch temporarily widensdrops to show icon + short text (e.g. Battery 20%, Charging, Reminder ..., Timer done, Volume 60 with a bar, Mic muted) for about 4 s, then returns. Alerts queue; critical ones can expand more and play a sound. Each alert type togglable.
- Expanded size about 640x320 DIP.
- Colors black base, one accent color, tinted dark surfaces (never flat gray). Album-art dominant color glow when music is playing. Corner radius animates from about 16 to 32.

=== ANIMATION SPEC (important, most of the polish lives here) ===
- Hover to expand after a hover-intent delay (default 120 ms, configurable), the notch slides down from the top width and height grow together with a spring (damping ratio about 0.78, response about 380 ms, slight overshoot of 2-3%). Implement a reusable spring animator, not just built-in easing.
- Content fades in 180 ms with 30 ms stagger per element, moving up 6 px to final position; content is revealed only after the shape is about 60% grown.
- Collapse when the pointer leaves, wait 300 ms grace, then collapse with ease-in-out cubic, about 320 ms, content fades first (120 ms). Esc also collapses. If the pointer re-enters mid-animation, reverse smoothly from the current value (never jump or restart).
- Tab switch content slides horizontally 24 px with cross-fade 220 ms; the selected-tab indicator is a pill that slides between tabs on a spring.
- Numbers (CPU %, temperatures, speeds) tween over 250 ms; bars and rings animate smoothly; sparklines scroll continuously.
- Buttons hover scale 1.04, press scale 0.96, 90 ms. Sliders have smooth thumb and fill.
- Alerts drop-in with a small spring, auto-dismiss with fade-out.
- Pinlock pin icon in the header keeps the panel open and ignores mouse-leave; pinned state persists.
- Reduce motion setting replaces springs with short fades. Animation speed setting (0.5x - 2x).

=== PANEL LAYOUT (expanded) ===
Header row tab icon bar at left (icons with sliding selection pill), timedate centered, pin icon and settings gear at right. Below tab content. Right-click panel opens a context menu (Settings, Pin, Quit).

TABS
1. MUSIC Windows media sessions (SMTC) cover, title, artist, seekable progress bar, prevplay-pausenext, shufflerepeat when supported, multiple players with switchpin. Synced lyrics from LRCLIB (current line highlighted, scrolling). Real audio equalizer. Dominant-color theming from the cover.
2. SYSTEM (like BrimDeck's monitor page) CPU total ring plus per-core vertical bars, RAM bar (usedtotal), GPU usage + VRAM + temperature + board power (NVML, with PDH fallback for non-NVIDIA), network updown speed plus today's totals, per-drive disk usage (internal drives only), power draw in watts with 60s curve and mean, battery (percent, charging plug icon, time left, health, power mode), CPUGPUSSD temperatures where available, top processes list with CPUMemory toggle and click for details, tooltips on every metric. Sample at 1 s only while this tab is visible or a compact readout needs it.
3. CALENDAR & WEATHER month view with today highlighted, today'supcoming events (local events with addeditdelete, optional ICS fileURL import), weather via Open-Meteo current, hourly strip, 5-day forecast, auto-location by IP with manual override, units setting.
4. TASKS to-dos (add, check, delete, drag reorder, priority, due date) and reminders (one-time or repeating, time picker). When a reminder fires alert in collapsed notch with Snooze (51030) and Done; sound optional.
5. FOCUS Pomodoro (workshortlong break, cycles), plain timer, stopwatch; global hotkey to startstop; while running, a progress ring shows in the collapsed notch; finishing triggers an alert.
6. QUICK CONTROLS volume slider (Core Audio) and brightness slider (WMI for built-in display, optional DDCCI), mic mute toggle, Wi-Fi and Bluetooth toggles (Windows.Devices.Radios), Do Not DisturbFocus toggle, screenshot (opens snipping), lock PC, sleep. Volumebrightnessmic changes made anywhere in Windows also trigger a small collapsed-notch OSD.

Everything is organized into these tabs; no giant dropdown list.

=== SETTINGS WINDOW ===
A separate, polished window (same visual language, Micadark) with a LEFT SIDEBAR of pages, each page with grouped cards, toggles, sliders, and pickers (no single long dropdown)
- General start with Windows, tray icon, language, 1224h time, show seconds, startup behavior.
- Appearance theme, accent color, notch size, corner radius, blurglow, animation speed, reduce motion.
- Behavior hover delay, collapse delay, pin default, monitor choice, verticalhorizontal offset, maximized-window behavior (indicatorhidenormal), fullscreen auto-hide.
- Collapsed what appears (time, music preview, lyric line, equalizer, live stat readout such as CPURAMnetworkbattery, timer ring), alert toggles.
- Music preferred player, lyrics onoff, equalizer source, cover glow.
- System Monitor readings shown, GPU selection, temperature toggles, power display, update interval, per-core view, top-process default.
- Calendar & Weather location, units, ICS sources, week start.
- Tasks & Reminders default snooze, sound, repeat options.
- Focus durations, cycles, auto-start breaks, sound.
- Quick Controls which toggles show and order.
- Alerts per-alert enable, thresholds (battery 2010%, temperature limits), sound, duration, do-not-disturb schedule.
- Hotkeys global hotkeys (openpin, start timer, mute mic, etc.), conflict detection.
- Themes built-in Dark, Light, One Dark Pro, GitHub Dark, Dracula, Night Owl, Cyberpunk; a custom theme editor (background, foreground, accent, surfaces) saved as JSON; importexport.
- About & Updates version, logs folder, reset settings.
Settings apply live and persist. A live mini preview of the notch is shown in Appearance.

=== TRAY ===
Tray icon with menu Open, PinUnpin, Settings, Pause, Quit. Double-click opens Settings.

=== QUALITY RULES ===
- Every sensorAPI must fail gracefully (hide the reading, never crash, never show fake numbers).
- Pause all polling and animation timers when collapsed unless a compact readout needs them.
- Separate layers Services (Media, Audio, System, Battery, Weather, Calendar, Tasks, Timers, Hotkeys), ViewModels, Views, Theme resources.
- Unit tests for tasksreminders logic, timers, settings, and theme loading.
- Clean code, comments only where non-obvious. Do not add features not listed here. Out of scope AI quota tracking, notification mirroring, clipboard history.

=== MILESTONES (stop after each) ===
M0 Solution, plan, git, settings and theme infrastructure.
M1 Transparent, no-focus, topmost window; collapsed notch with time; click-through region; multi-monitor and DPI.
M2 Hover expandcollapse with the full spring animation system; pin; Esc; tab bar with sliding indicator; tray.
M3 Music tab + collapsed music preview + real equalizer + lyrics.
M4 System tab (all readings) + collapsed live readouts.
M5 Alerts system (battery, charger, temps, OSD for volumebrightnessmic).
M6 Quick Controls, Focus, Tasks & Reminders.
M-Layout: auto-sizing, spacing, readability and design system overhaul.
M-Transparency-Clock-Polish: BrimDeck-style transparency, Clock Studio, Collapsed Layout settings, System page density matching BrimDeck, and Neon/Cyberpunk polish.
M7 Calendar & Weather.
M8 Full Settings window with all pages, themes, hotkeys.
M9 Fullscreenmaximized behavior, polish pass, performance profiling, installer (Inno Setup or MSIX).

Begin with the plan and M0 now.