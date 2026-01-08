# Session Summary - January 7, 2026

## Overview
This session focused on workflow automation, UI improvements, and bug fixes for the DGScope Profile Manager.

## Completed Work

### 1. GitHub Workflow Improvements
**Files Modified:**
- `.github/workflows/release-bundle.yml`
- `installer/DGScopeProfileManager-Bundle.nsi`

**Changes:**
- Added `permissions: contents: write` to workflow for release creation
- Changed to use `PAT_TOKEN` instead of `GITHUB_TOKEN` for cross-repo access
- Removed automatic creation of empty ARTCC folders (profiles folder now created empty)
- Profiles will be created by the application as needed

**Status:** Working - test builds running successfully with tag v1.0.0-test8

### 2. Settings UI Improvements
**Files Modified:**
- `src/DGScopeProfileManager/Views/SettingsWindow.xaml`
- `src/DGScopeProfileManager/Views/SettingsWindow.xaml.cs`
- `src/DGScopeProfileManager/Models/AppSettings.cs`

**Changes:**
- Removed yellow information box from Settings window
- Added tooltips to each setting label with folder structure information
- Renamed "DGScope Root Folder" to "DGScope Profiles Folder"
- Reduced window height from 500 to 250px
- Implemented auto-detection for all paths:
  - CRC folder: `%LocalAppData%\CRC`
  - DGScope profiles folder: `../profiles` relative to ProfileManager exe
  - DGScope executable: `../scope/scope.exe` relative to ProfileManager exe

### 3. Fix All Paths Button
**Files Modified:**
- `src/DGScopeProfileManager/Services/DgScopeProfileService.cs`
- `src/DGScopeProfileManager/MainWindow.xaml.cs`

**Changes:**
- Updated `FixFilePaths()` method to directly update `VideoMapFilename` element in XML
- Now saves changes to the XML file automatically
- Removed redundant `SaveProfile()` call from MainWindow

**Issue Fixed:** Video map paths are now properly updated in the XML file, not just in memory

### 4. Home Location Preservation
**Files Modified:**
- `src/DGScopeProfileManager/Services/DgScopeProfileService.cs`
- `src/DGScopeProfileManager/Models/DgScopeProfile.cs`

**Changes:**
- Modified `ApplyPrefSetSettings()` to preserve existing `ScreenCenterPoint` coordinates
- Added `HomeLocationLatitude` and `HomeLocationLongitude` properties to `DgScopeProfile`
- Updated `LoadProfile()` to parse `HomeLocation` from XML

**Issue Fixed:** Default settings no longer overwrite profile home locations

### 5. Reset to Home Location Feature
**Files Modified:**
- `src/DGScopeProfileManager/Views/UnifiedSettingsWindow.xaml`
- `src/DGScopeProfileManager/Views/UnifiedSettingsWindow.xaml.cs`

**Changes:**
- Added "Reset to Airport/Radar Center" button in Screen Position section
- Button only visible in profile edit mode (not in default settings mode)
- Resets screen center coordinates to the profile's HomeLocation
- Shows confirmation message with coordinates

## Project Structure

### Release Package Structure
```
DGScope-Profile-Manager/
├── ProfileManager/          # Built from this repo
│   └── DGScopeProfileManager.exe
├── scope/                   # Downloaded from yanjz124/scope releases
│   └── scope.exe
└── profiles/                # Empty folder for profile storage
    └── (ARTCC folders created by app as needed)
```

### Video Map Information in CRC
Each video map in CRC JSON has:
- `id`: Hash/unique identifier
- `sourceFileName`: Actual filename (e.g., "ACY_RVM.geojson")
- `tags`: Array of tags for categorization

The `VideoMapInfo.ToString()` displays `sourceFileName` in the UI.

## Build Process

### Triggering a Build
```bash
git tag v1.0.0-testX
git push origin v1.0.0-testX
```

### Build Outputs
- `DGScope-Profile-Manager-vX.X.X.zip` - Portable package
- `DGScope-Profile-Manager-vX.X.X-Setup.exe` - NSIS installer

### Required GitHub Secret
- `PAT_TOKEN`: Personal Access Token with `public_repo` scope for downloading from yanjz124/scope

## Known Issues / Future Work

### Potential Issues
1. DGScope launch error if settings were saved before auto-detection was implemented
   - Fix: Delete old settings and restart app to regenerate with auto-detection

### Future Enhancements
None identified - all requested features completed

## Testing Checklist

Before public release (v1.0.0):
- [ ] Test auto-detection on fresh install
- [ ] Verify Fix All Paths updates video map paths correctly
- [ ] Confirm default settings preserve home locations
- [ ] Test Reset to Home Location button
- [ ] Verify profiles folder creation works
- [ ] Test both ZIP and installer packages
- [ ] Verify DGScope launch works after install

## Git History
- `44dc935`: Improve Settings UI and auto-detection
- `1987ee8`: Fix multiple profile manager issues
- `1e0fd8f`: Use PAT_TOKEN for cross-repo release access
- `b477b62`: Use authenticated GitHub API to download DGScope release

## Next Steps
Ready for testing on another machine. All core functionality implemented and committed.
