# DGScope Setup Helper Script
# This script guides users through:
# 1. Installing Git for Windows (if needed)
# 2. Installing .NET 7.0.405 SDK (if needed)
# 3. Cloning the DGScope repo
# 4. Building DGScope

param(
    [switch]$NoPrompt = $false
)

$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Message)
    Write-Host -ForegroundColor Cyan "[DGScope Setup]" $Message
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host -ForegroundColor Red "[ERROR]" $Message
}

function Write-Success {
    param([string]$Message)
    Write-Host -ForegroundColor Green "[SUCCESS]" $Message
}

function Test-CommandExists {
    param([string]$Command)
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

function Install-GitForWindows {
    Write-Status "Git for Windows not found. Downloading and installing..."
    
    $gitUrl = "https://github.com/git-for-windows/git/releases/download/v2.42.0.windows.2/Git-2.42.0.2-64-bit.exe"
    $gitInstaller = "$env:TEMP\Git-Installer.exe"
    
    try {
        Invoke-WebRequest -Uri $gitUrl -OutFile $gitInstaller -UseBasicParsing
        & $gitInstaller /SILENT /INSTALL=C:\Program Files\Git
        Write-Success "Git installed successfully."
        return $true
    }
    catch {
        Write-Error-Custom "Failed to install Git: $_"
        Write-Status "Please install Git manually from https://git-scm.com/download/win"
        return $false
    }
}

function Install-DotNET7SDK {
    Write-Status ".NET 7.0.405 SDK not found. Downloading and installing..."
    
    # Check if running x64 or x86
    if ([Environment]::Is64BitOperatingSystem) {
        $dotnetUrl = "https://dot.net/v1/dotnet-install.ps1"
    }
    else {
        $dotnetUrl = "https://dot.net/v1/dotnet-install.ps1"
    }
    
    try {
        $dotnetScript = "$env:TEMP\dotnet-install.ps1"
        Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetScript -UseBasicParsing
        
        # Run installer for .NET 7.0.405
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $dotnetScript -Version 7.0.405
        
        Write-Success ".NET 7.0.405 SDK installed successfully."
        
        # Refresh PATH for current session
        $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
        
        return $true
    }
    catch {
        Write-Error-Custom "Failed to install .NET SDK: $_"
        Write-Status "Please install .NET 7.0.405 SDK manually from https://dotnet.microsoft.com/en-us/download/dotnet/7.0"
        return $false
    }
}

function Clone-DGScopeRepo {
    $repoPath = "$env:USERPROFILE\Documents\DGScope"
    
    if (Test-Path $repoPath) {
        Write-Status "DGScope repo already exists at $repoPath"
        return $repoPath
    }
    
    Write-Status "Cloning DGScope repository..."
    
    try {
        # Use HTTPS clone (no SSH setup required)
        & git clone https://github.com/Oxillius/scope-profiles.git $repoPath
        Write-Success "Repository cloned to $repoPath"
        return $repoPath
    }
    catch {
        Write-Error-Custom "Failed to clone repository: $_"
        return $null
    }
}

function Build-DGScope {
    param([string]$RepoPath)
    
    if (-not (Test-Path $RepoPath)) {
        Write-Error-Custom "Repository path not found: $RepoPath"
        return $false
    }
    
    Write-Status "Building DGScope..."
    
    try {
        Push-Location $RepoPath
        
        # Run dotnet build
        & dotnet build --configuration Release
        
        Pop-Location
        Write-Success "DGScope built successfully."
        Write-Status "Output location: $RepoPath\scope\build\Release\"
        return $true
    }
    catch {
        Write-Error-Custom "Build failed: $_"
        Pop-Location
        return $false
    }
}

# Main execution
Clear-Host
Write-Status "DGScope Setup Wizard"
Write-Status "===================="
Write-Host ""

# Step 1: Check Git
Write-Status "Step 1: Checking Git..."
if (Test-CommandExists "git") {
    $gitVersion = & git --version
    Write-Success "Git found: $gitVersion"
}
else {
    Write-Status "Git not found."
    if (-not $NoPrompt) {
        $response = Read-Host "Install Git for Windows? (Y/n)"
        if ($response -ne "n") {
            if (-not (Install-GitForWindows)) {
                Write-Status "Exiting setup."
                exit 1
            }
        }
        else {
            Write-Status "Skipping Git installation."
        }
    }
}

Write-Host ""

# Step 2: Check .NET 7.0.405 SDK
Write-Status "Step 2: Checking .NET 7.0.405 SDK..."
if (Test-CommandExists "dotnet") {
    $dotnetVersion = & dotnet --version
    Write-Success "dotnet CLI found: $dotnetVersion"
    
    # Check if version is 7.0.405 or compatible
    if ($dotnetVersion -match "7\.0\.\d+") {
        Write-Success ".NET 7.x SDK is installed."
    }
    else {
        Write-Status "Note: Found .NET $dotnetVersion; recommend .NET 7.0.405 for DGScope."
    }
}
else {
    Write-Status ".NET 7.0.405 SDK not found."
    if (-not $NoPrompt) {
        $response = Read-Host "Install .NET 7.0.405 SDK? (Y/n)"
        if ($response -ne "n") {
            if (-not (Install-DotNET7SDK)) {
                Write-Status "Exiting setup."
                exit 1
            }
        }
        else {
            Write-Status "Skipping .NET installation."
        }
    }
}

Write-Host ""

# Step 3: Clone repo
Write-Status "Step 3: Cloning DGScope repository..."
$repoPath = Clone-DGScopeRepo
if (-not $repoPath) {
    Write-Error-Custom "Failed to clone repository."
    exit 1
}

Write-Host ""

# Step 4: Build
Write-Status "Step 4: Building DGScope..."
if (-not $NoPrompt) {
    $response = Read-Host "Build DGScope now? (Y/n)"
    if ($response -ne "n") {
        if (-not (Build-DGScope $repoPath)) {
            Write-Error-Custom "Build failed."
            Write-Status "You can retry manually by running: cd '$repoPath' && dotnet build --configuration Release"
            exit 1
        }
    }
    else {
        Write-Status "Skipping build. You can build manually later:"
        Write-Status "  cd '$repoPath'"
        Write-Status "  dotnet build --configuration Release"
    }
}
else {
    if (-not (Build-DGScope $repoPath)) {
        exit 1
    }
}

Write-Host ""
Write-Success "DGScope setup complete!"
Write-Status "DGScope is located at: $repoPath"
Write-Status "To launch DGScope after building:"
Write-Status "  $repoPath\scope\build\Release\DGScope.exe"
Write-Host ""
exit 0
