# DGScope Profile Manager Installer

This directory contains the Windows installer and setup scripts for DGScope Profile Manager.

## Prerequisites to Build the Installer

- **NSIS 3.x** - Download from [NSIS Official](https://nsis.sourceforge.io/) or install via winget/chocolatey
- **Visual Studio 2022** or **VS Build Tools 2022** (for building Profile Manager .NET app)
- **PowerShell 5.0+** (included with Windows 10+)

### Quick Install (Windows)

```powershell
# Using winget
winget install NSIS.NSIS

# Using chocolatey
choco install nsis
```

## Files

- **DGScopeProfileManager.nsi** - NSIS installer script
  - Detects .NET Framework 4.7.2 on the system
  - Auto-downloads and installs if missing
  - Bundles Profile Manager executable
  - Creates Start Menu and Desktop shortcuts
  - Includes optional DGScope setup wizard

- **setup-dgscope.ps1** - PowerShell helper for DGScope setup
  - Detects and installs Git for Windows
  - Detects and installs .NET 7.0.405 SDK
  - Clones DGScope from Oxillius fork
  - Builds DGScope from source

## Building the Installer

### Step 1: Build Profile Manager (Release)

```bash
cd src/DGScopeProfileManager
dotnet build --configuration Release
```

Output: `bin/Release/net10.0-windows/DGScopeProfileManager.exe`

### Step 2: Compile NSIS Installer

Open NSIS and compile `DGScopeProfileManager.nsi`, or use command line:

```bash
"C:\Program Files (x86)\NSIS\makensis.exe" installer\DGScopeProfileManager.nsi
```

Output: `DGScopeProfileManager-Setup.exe` (in parent directory)

### Step 3: Test the Installer

Run the generated `.exe` file and follow the installer wizard.

## Features

### Profile Manager Installation
- ✅ .NET Framework 4.7.2 auto-detection and auto-install
- ✅ Program Files installation to `C:\Program Files\DGScopeProfileManager`
- ✅ Start Menu shortcuts
- ✅ Desktop shortcut
- ✅ Registry entry for Add/Remove Programs
- ✅ Uninstall support

### DGScope Setup (Optional)
- ✅ Git for Windows detection and install
- ✅ .NET 7.0.405 SDK detection and install
- ✅ Clones Oxillius/scope-profiles from GitHub
- ✅ Builds DGScope from source (`dotnet build`)
- ✅ Interactive prompts (can be bypassed with `-NoPrompt` flag)

## Installer Sections

The installer has two main sections:

1. **Install DGScope Profile Manager** (Required)
   - Checks for .NET Framework 4.7.2
   - Installs Profile Manager

2. **Setup DGScope** (Optional)
   - Runs the PowerShell setup wizard
   - Guides users through DGScope prerequisites and build

Users can choose to skip DGScope setup if they only want the Profile Manager.

## .NET Framework 4.7.2 Detection

The installer checks the Windows Registry for:
```
HKLM\Software\Microsoft\NET Framework Setup\NDP\v4\Full\Release >= 461808
```

If not found, it downloads the official Microsoft installer from:
```
https://download.microsoft.com/download/0/5/C/05C91A2B-8B22-40FF-B3A8-413ECF54DD57/NDP472-KB4054530-x86-x64exe.exe
```

And installs it with `/q /norestart` flags (silent mode).

## Troubleshooting

### NSIS Compilation Fails
- Ensure NSIS is installed and in PATH
- Check that DLL files are present in `src/DGScopeProfileManager/bin/Release/net10.0-windows/`

### .NET Framework 4.7.2 Download Fails
- Check internet connection
- Installer will prompt user to install manually from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet-framework/net472)

### DGScope Setup Fails
- If Git install fails, user is prompted to install manually from [git-scm.com](https://git-scm.com/download/win)
- If .NET SDK install fails, user is prompted to install manually from [dotnet.microsoft.com/7.0](https://dotnet.microsoft.com/download/dotnet/7.0)
- Users can always clone and build DGScope manually from [Oxillius/scope-profiles](https://github.com/Oxillius/scope-profiles)

## License

- **DGScope Profile Manager** - License as per source
- **DGScope** - GPLv3 (from upstream project)
- **Installer (NSIS)** - Public domain

## Support

For issues with:
- **Profile Manager**: Check [this repository's issues](https://github.com/your-repo/issues)
- **DGScope**: Check [Oxillius/scope-profiles issues](https://github.com/Oxillius/scope-profiles/issues)
