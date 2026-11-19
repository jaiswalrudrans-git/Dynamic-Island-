@echo off
setlocal
cd /d "%~dp0"
taskkill /f /im Island.App.exe >nul 2>&1
if exist "%~dp0.tools\dotnet\dotnet.exe" set "DOTNET_ROOT=%~dp0.tools\dotnet"
if not exist "src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe" (
    echo Building Island in Release mode...
    dotnet build -c Release
)
start "" "src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe"
echo ========================================================
echo Island is now running!
echo Look at the very TOP-CENTER edge of your main display.
echo You will see the collapsed pill showing the time.
echo Hover your mouse over it to expand the island.
echo Right-click the island or check the system tray to Pin or open Settings.
echo ========================================================
