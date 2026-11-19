# Milestone progress

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
