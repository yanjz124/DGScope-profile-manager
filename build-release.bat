@echo off
REM Build release package with DGScope from GitHub

setlocal enabledelayedexpansion

echo.
echo ========================================
echo DGScope Profile Manager Release Builder
echo ========================================
echo.

REM Check if PowerShell is available
powershell -Command "Write-Host 'PowerShell available'" >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: PowerShell is required but not found
    exit /b 1
)

REM Run the PowerShell script
echo Building release package...
echo.

powershell -ExecutionPolicy Bypass -File "build-release.ps1" -ReleaseVersion "1.0.0"

if %errorlevel% neq 0 (
    echo.
    echo Error: Release build failed
    pause
    exit /b 1
)

echo.
echo Release build complete! Check the release folder for the ZIP file.
pause
