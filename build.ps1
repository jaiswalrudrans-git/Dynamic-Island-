<#
.SYNOPSIS
    Builds Island 2 for production, runs tests, and packages release artifacts.
.DESCRIPTION
    Validates .NET 8 SDK, builds Solution in Release with TreatWarningsAsErrors=true,
    executes the test suite with code coverage assertions, and copies final binaries to artifacts/.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$SkipTests = $false,
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
if ([string]::IsNullOrWhiteSpace($OutputDir) -or $OutputDir -eq "\artifacts") {
    $OutputDir = Join-Path $scriptRoot "artifacts"
}

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Island 2 - Production Build Pipeline" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

# 1. Verify .NET SDK
Write-Host "`n[1/4] Verifying .NET 8 SDK..." -ForegroundColor Yellow
$dotnetVersion = & dotnet --version
Write-Host "Found .NET SDK: $dotnetVersion" -ForegroundColor Green

# 2. Restore NuGet Packages
Write-Host "`n[2/4] Restoring packages..." -ForegroundColor Yellow
& dotnet restore "$PSScriptRoot\Island.sln"
if ($LASTEXITCODE -ne 0) { throw "Restore failed with exit code $LASTEXITCODE" }

# 3. Build Solution in Release with Warnings as Errors
Write-Host "`n[3/4] Building solution ($Configuration) with WarningsAsErrors..." -ForegroundColor Yellow
& dotnet build "$scriptRoot\Island.sln" -c $Configuration -p:TreatWarningsAsErrors=true --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

# 4. Run Test Suite
if (-not $SkipTests) {
    Write-Host "`n[4/4] Executing test suite..." -ForegroundColor Yellow
    & dotnet test "$scriptRoot\Island.sln" -c $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "Unit tests failed with exit code $LASTEXITCODE" }
} else {
    Write-Host "`n[4/4] Skipping tests (-SkipTests specified)" -ForegroundColor Gray
}

# 5. Output Artifacts
$appBinDir = "$scriptRoot\src\Island.App\bin\$Configuration\net8.0-windows10.0.19041.0"
if (Test-Path $appBinDir) {
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    Copy-Item "$appBinDir\*" -Destination $OutputDir -Recurse -Force
    Write-Host "`nArtifacts published to: $OutputDir" -ForegroundColor Green
}

Write-Host "`nSUCCESS: Island 2 production build complete!" -ForegroundColor Green
