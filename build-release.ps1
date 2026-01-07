# Build Release Package
# This script creates a complete release package with DGScope, Profiles, and Profile Manager

param(
    [string]$ReleaseVersion = "1.0.0",
    [string]$ScopeRelease = "latest"  # GitHub release tag or 'latest'
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$ReleaseDir = ".\release"
$PublishDir = "src\DGScopeProfileManager\bin\Release\net10.0-windows\win-x64\publish"

Write-Host "Building DGScope Profile Manager Release v$ReleaseVersion" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Build the application
Write-Host "`n[1/4] Building application..." -ForegroundColor Cyan
dotnet publish src/DGScopeProfileManager/DGScopeProfileManager.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o $PublishDir | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "✓ Application built" -ForegroundColor Green

# Step 2: Create release directory structure
Write-Host "`n[2/4] Creating release directory..." -ForegroundColor Cyan
if (Test-Path $ReleaseDir) { Remove-Item $ReleaseDir -Recurse -Force }
$null = mkdir "$ReleaseDir\DGScope-Profile-Manager"
$ReleaseRoot = "$ReleaseDir\DGScope-Profile-Manager"

Write-Host "✓ Release directory created at $ReleaseRoot" -ForegroundColor Green

# Step 3: Copy Profile Manager
Write-Host "`n[3/4] Copying Profile Manager..." -ForegroundColor Cyan
$null = mkdir "$ReleaseRoot\ProfileManager"
Copy-Item "$PublishDir\*" "$ReleaseRoot\ProfileManager\" -Recurse
Write-Host "✓ Profile Manager copied" -ForegroundColor Green

# Step 4: Download DGScope
Write-Host "`n[4/4] Downloading DGScope from GitHub..." -ForegroundColor Cyan

$Owner = "yanjz124"
$Repo = "scope"

# Get the latest release
$ReleasesUrl = "https://api.github.com/repos/$Owner/$Repo/releases"
$Release = if ($ScopeRelease -eq "latest") {
    (Invoke-WebRequest -Uri $ReleasesUrl | ConvertFrom-Json)[0]
} else {
    Invoke-WebRequest -Uri "$ReleasesUrl/tags/$ScopeRelease" | ConvertFrom-Json
}

if (-not $Release) {
    throw "Could not find GitHub release"
}

$ZipAsset = $Release.assets | Where-Object { $_.name -match "\.zip$" } | Select-Object -First 1
if (-not $ZipAsset) {
    throw "No ZIP asset found in release: $($Release.name)"
}

$DownloadUrl = $ZipAsset.browser_download_url
$ZipFile = Join-Path $ReleaseDir "scope.zip"

Write-Host "Downloading $($ZipAsset.name) ($([math]::Round($ZipAsset.size / 1MB, 2)) MB)..."
Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipFile
Write-Host "✓ Downloaded: $($ZipAsset.name)" -ForegroundColor Green

# Extract and organize
Write-Host "Extracting and organizing DGScope files..."
$ExtractDir = Join-Path $ReleaseDir "scope-extract"
$null = mkdir $ExtractDir
Expand-Archive -Path $ZipFile -DestinationPath $ExtractDir

# Find the scope folder (it might be nested in a release folder)
$ScopeFolder = Get-ChildItem $ExtractDir -Directory -Recurse -Filter "scope" | Select-Object -First 1
if (-not $ScopeFolder) {
    # Try to find scope.exe
    $ScopeExe = Get-ChildItem $ExtractDir -File -Recurse -Filter "scope.exe" | Select-Object -First 1
    if ($ScopeExe) {
        $ScopeFolder = $ScopeExe.Directory
    } else {
        throw "Could not find scope.exe or scope folder in release"
    }
}

# Copy entire scope folder to release
Copy-Item $ScopeFolder.FullName "$ReleaseRoot\scope" -Recurse
Write-Host "✓ DGScope extracted and copied" -ForegroundColor Green

# Verify scope.exe exists
$ScopeExePath = "$ReleaseRoot\scope\scope.exe"
if (-not (Test-Path $ScopeExePath)) {
    throw "scope.exe not found at expected location: $ScopeExePath"
}
Write-Host "✓ Verified scope.exe exists" -ForegroundColor Green

# Step 5: Create profiles folder structure
Write-Host "`nCreating profiles folder structure..." -ForegroundColor Cyan
$null = mkdir "$ReleaseRoot\profiles"
@("ZAN", "ZAK", "ZDV", "ZDC", "ZID", "ZIN", "ZJX", "ZKC", "ZLA", "ZLC", "ZMA", "ZME", "ZMP", "ZNY", "ZOA", "ZOB", "ZSE", "ZTL", "ZUA") | ForEach-Object {
    $null = mkdir "$ReleaseRoot\profiles\$_"
}
Write-Host "✓ Profiles folder structure created" -ForegroundColor Green

# Step 6: Create package files
Write-Host "`nCreating package files..." -ForegroundColor Cyan

# Create ZIP for distribution
$ZipOutput = "DGScope-Profile-Manager-v$ReleaseVersion.zip"
Compress-Archive -Path $ReleaseRoot -DestinationPath $ZipOutput -Force
Write-Host "✓ Created ZIP: $ZipOutput" -ForegroundColor Green

# Create README
$ReadmeContent = @"
# DGScope Profile Manager v$ReleaseVersion

Complete bundle with DGScope and Profile Manager

## Contents

- **ProfileManager/**: DGScope Profile Manager application
- **scope/**: DGScope radar simulation (prebuilt, ready to run)
- **profiles/**: Empty ARTCC profile folders (auto-detected by Profile Manager)

## Quick Start

1. Extract this folder anywhere on your computer
2. Run `ProfileManager\DGScopeProfileManager.exe`
3. (Optional) In Settings, configure your CRC root folder path
4. The app will auto-detect the bundled DGScope
5. Generate profiles or select existing ones
6. Click "Launch DGScope" to open profiles

## Folder Structure

```
DGScope-Profile-Manager/
├── ProfileManager/          # Profile Manager executable and dependencies
├── scope/                   # DGScope radar simulation
│   ├── scope.exe           # Main application
│   ├── dependencies/       # .NET and runtime files
│   └── ...                 # Other DGScope files
├── profiles/               # DGScope profile storage
│   ├── ZAN/
│   ├── ZAK/
│   ├── ZDV/
│   ├── ... (all ARTCCs)
│   └── ZTL/
└── README.md              # This file
```

## Auto-Detection

The Profile Manager automatically detects `scope/scope.exe` in the same directory.
No manual configuration needed!

## Manual Configuration

If you want to use a different DGScope installation:
1. Open Settings in Profile Manager (gear icon)
2. Browse for DGScope Executable
3. Select the desired scope.exe location
4. Click OK to save

## Requirements

- Windows 10/11
- .NET 10.0 Runtime (included in scope folder)
- Optional: CRC (vERAM/vSTARS) for importing profiles

## Documentation

For detailed usage and features, see:
https://github.com/yanjz124/DGScope-profile-manager

## Support

Issues and feature requests:
https://github.com/yanjz124/DGScope-profile-manager/issues
"@

$ReadmeContent | Out-File "$ReleaseRoot\README.md" -Encoding UTF8
Write-Host "✓ Created README.md" -ForegroundColor Green

# Cleanup temporary files
Write-Host "`nCleaning up temporary files..." -ForegroundColor Cyan
Remove-Item $ZipFile -Force
Remove-Item $ExtractDir -Recurse -Force
Write-Host "✓ Cleanup complete" -ForegroundColor Green

# Summary
Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Release Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nPackage: $ZipOutput"
Write-Host "Size: $('{0:N2}' -f ((Get-Item $ZipOutput).Length / 1MB)) MB"
Write-Host "DGScope Version: $($Release.tag_name)"
Write-Host "Profile Manager Version: $ReleaseVersion"
Write-Host "`nReadiness: Ready for distribution!" -ForegroundColor Green
