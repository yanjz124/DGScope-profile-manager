@echo off
REM Build DGScope Profile Manager Installer
REM Requirements: Visual Studio 2022, NSIS 3.x

setlocal enabledelayedexpansion

echo.
echo ============================================
echo DGScope Profile Manager Installer Builder
echo ============================================
echo.

REM Check for .NET CLI
echo Checking for .NET CLI...
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: dotnet CLI not found. Please install .NET SDK.
    exit /b 1
)
echo OK: .NET CLI found.

REM Check for NSIS
set NSIS_PATH=C:\Program Files (x86)\NSIS\makensis.exe
echo Checking for NSIS...
if exist "%NSIS_PATH%" (
    echo OK: NSIS found.
) else (
    echo ERROR: NSIS ^(makensis.exe^) not found. Please install NSIS 3.x
    exit /b 1
)

REM Set paths
set PROJ_DIR=%~dp0..
set BUILD_OUTPUT=%PROJ_DIR%\src\DGScopeProfileManager\bin\Release\net10.0-windows
set INSTALLER_DIR=%~dp0
set INSTALLER_OUTPUT=%PROJ_DIR%\DGScopeProfileManager-Setup.exe

echo.
echo Step 1: Building Profile Manager (Release)...
echo.

cd /d "%PROJ_DIR%\src\DGScopeProfileManager"
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo ERROR: Profile Manager build failed.
    exit /b 1
)

echo.
echo Build output: %BUILD_OUTPUT%
echo.

REM Verify output files exist
if not exist "%BUILD_OUTPUT%\DGScopeProfileManager.exe" (
    echo ERROR: DGScopeProfileManager.exe not found in output directory.
    exit /b 1
)

echo OK: Profile Manager built successfully.
echo.

echo Step 2: Compiling NSIS Installer...
echo.

"%NSIS_PATH%" "%INSTALLER_DIR%DGScopeProfileManager.nsi"

if %errorlevel% neq 0 (
    echo ERROR: NSIS compilation failed.
    exit /b 1
)

echo.
if exist "%INSTALLER_OUTPUT%" (
    echo.
    echo ============================================
    echo SUCCESS!
    echo ============================================
    echo Installer created: %INSTALLER_OUTPUT%
    echo.
    echo To distribute:
    echo   1. Sign the EXE with a code signing certificate (optional)
    echo   2. Upload to GitHub Releases or your distribution site
    echo.
) else (
    echo ERROR: Installer file not created.
    exit /b 1
)

pause
