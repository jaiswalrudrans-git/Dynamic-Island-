Set-Location $PSScriptRoot
Stop-Process -Name "Island.App" -Force -ErrorAction SilentlyContinue
if (Test-Path "$PSScriptRoot\.tools\dotnet\dotnet.exe") {
    $env:DOTNET_ROOT = "$PSScriptRoot\.tools\dotnet"
}
$exe = "$PSScriptRoot\src\Island.App\bin\Release\net8.0-windows10.0.19041.0\Island.App.exe"
if (!(Test-Path $exe)) {
    Write-Host "Building Island in Release mode..." -ForegroundColor Cyan
    dotnet build -c Release
}
Start-Process -FilePath $exe
Write-Host "========================================================" -ForegroundColor Green
Write-Host "Island is now running!" -ForegroundColor Green
Write-Host "Look at the very TOP-CENTER edge of your main screen." -ForegroundColor Yellow
Write-Host "You will see the collapsed notch showing the clock." -ForegroundColor Yellow
Write-Host "Hover your mouse over it to expand the island." -ForegroundColor Cyan
Write-Host "Right-click or check the system tray icon to Pin or open Settings." -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Green
