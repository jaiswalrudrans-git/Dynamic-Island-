# Milestone progress

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
