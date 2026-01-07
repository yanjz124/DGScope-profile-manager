# DGScope Profile Manager Bundle

Complete package with DGScope, Profile Manager, and empty profile directories.

## Contents

- **ProfileManager/**: DGScope Profile Manager application (ready to run)
- **scope/**: DGScope radar simulation (prebuilt executable)
- **profiles/**: Empty ARTCC profile folders (auto-detected and auto-populated)

## Quick Start

1. Extract or install this bundle
2. Run `ProfileManager\DGScopeProfileManager.exe`
3. (Optional) Configure your CRC root folder in Settings
4. The app will auto-detect the bundled DGScope
5. Generate or select profiles
6. Click "Launch DGScope" to open profiles directly

## Auto-Detection

The Profile Manager automatically detects `scope/scope.exe` relative to the application.
No manual configuration needed!

## Manual Configuration

To use a different DGScope installation:
1. Open Settings in Profile Manager (gear icon)
2. Browse for DGScope Executable
3. Select your scope.exe location
4. Click OK to save

## Features

- **Automatic Profile Generation**: Extract facility data from CRC
- **Direct Launch**: Open profiles in DGScope with one click
- **Profile Management**: Edit, delete, and organize profiles
- **Apply-to-All Defaults**: Set template defaults for new profiles
- **Auto-Detection**: Bundled DGScope found automatically

## Documentation

For usage instructions and feature details:
https://github.com/yanjz124/DGScope-profile-manager

## Support

Report issues or request features:
https://github.com/yanjz124/DGScope-profile-manager/issues

## License

See LICENSE file in repository
