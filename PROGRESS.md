# Milestone progress

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
