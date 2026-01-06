# DGScope Profile Manager - Current Development Status

**Last Updated**: January 6, 2026
**Version**: v1.0.0 (In Development)
**Git Status**: Initial commit completed
**Build Status**: ✅ Compiles (with file lock warnings when app is running)

## What's Been Completed

### Core Features ✅
- [x] CRC profile import from `%LOCALAPPDATA%\CRC\ARTCCs\*.json`
- [x] TRACON/RAPCON/CERAP/RATCF filtering
- [x] Multi-area facility support
- [x] Area selection dialog for facilities with multiple radar positions
- [x] Custom profile naming (e.g., `ACY_main.xml`, `ACY_backup.xml`)
- [x] Automatic altimeter station configuration from `ssaAirports`
- [x] Automatic NEXRAD station selection based on proximity
- [x] Video map copying with human-readable filenames
- [x] Profile browsing and management UI

### Recent Changes (Latest Session)
1. **Area Selection Feature**:
   - Created `CrcArea` model to represent radar positions within a facility
   - Updated `CrcProfileReader` to parse `starsConfiguration.areas[]`
   - Each area has: id, name, lat/lon, and ssaAirports list
   - Created `AreaSelectionWindow.xaml` for user selection
   - Integrated into profile generation workflow

2. **Profile Naming Enhancement**:
   - Added textbox to `VideoMapSelectionWindow` for custom names
   - Profile name validation (no invalid filename characters)
   - Default name pre-filled with facility ID
   - Profiles saved as: `{FacilityID}_{CustomName}.xml`

3. **Altimeter Stations Fix**:
   - **Root Cause**: `ssaAirports` is in `areas[]`, not at `starsConfiguration` level
   - Updated parser to collect airports from all areas
   - Uses HashSet to avoid duplicates
   - When area selected: uses only that area's airports
   - When no area selected: aggregates all airports from all areas
   - Proper ICAO prefix logic: 'K' for CONUS, 'P' for ZAN and HCF

4. **Profile Generation Updates**:
   - `ProfileGeneratorService` now accepts `selectedArea` and `customProfileName` parameters
   - Location priority: selectedArea > selectedTracon > crcProfile
   - Altimeter priority: selectedArea > selectedTracon aggregate

### Documentation ✅
- [x] `README.md`: Comprehensive user documentation
- [x] `DEVELOPMENT.md`: Architecture and technical details
- [x] `TODO.md`: Future feature roadmap
- [x] Code comments and XML documentation

### Git Repository ✅
- [x] Initialized git repository
- [x] Created initial commit with all files
- [x] `.gitignore` configured for .NET projects

## Current File Structure

```
DGScope-profile-manager/
├── src/DGScopeProfileManager/
│   ├── Models/
│   │   ├── CrcProfile.cs          # CrcProfile, CrcTracon, CrcArea, VideoMapInfo
│   │   ├── DgScopeProfile.cs
│   │   ├── Facility.cs
│   │   ├── NexradStation.cs
│   │   └── AppSettings.cs
│   ├── Services/
│   │   ├── CrcProfileReader.cs     # Parses CRC JSON, extracts areas and ssaAirports
│   │   ├── ProfileGeneratorService.cs  # Generates profiles with area support
│   │   ├── NexradService.cs
│   │   ├── DgScopeProfileService.cs
│   │   ├── FacilityScanner.cs
│   │   ├── VideoMapService.cs
│   │   └── SettingsPersistenceService.cs
│   ├── Views/
│   │   ├── AreaSelectionWindow.xaml/cs      # NEW: Area selection
│   │   ├── VideoMapSelectionWindow.xaml/cs  # UPDATED: Added profile name textbox
│   │   ├── TraconSelectionWindow.xaml/cs
│   │   ├── ProfileConfigDialog.xaml/cs
│   │   ├── SettingsWindow.xaml/cs
│   │   ├── ProfileEditorWindow.xaml/cs
│   │   └── GenerateProfileDialog.xaml/cs
│   ├── Resources/
│   │   └── DefaultTemplate.xml      # Embedded default template (626 lines from PCT.xml)
│   ├── MainWindow.xaml/cs          # UPDATED: Integrated area selection workflow
│   └── App.xaml/cs
├── nexrad-stations.txt              # NEXRAD station database
├── README.md                        # User documentation
├── DEVELOPMENT.md                   # Development guide
├── TODO.md                          # Future features
├── CURRENT_STATUS.md                # This file
└── .gitignore

```

## Key Technical Details

### CRC JSON Structure (Important!)
```json
{
  "facility": {
    "childFacilities": [
      {
        "id": "PCT",
        "name": "Potomac TRACON",
        "type": "TRACON",
        "starsConfiguration": {
          "areas": [  // <-- ssaAirports is HERE, not at starsConfiguration level!
            {
              "id": "01GNAB2E7QW35BWQN8VN2ZESQN",
              "name": "Chesapeake",
              "visibilityCenter": {"lat": 39.452745, "lon": -74.591952},
              "ssaAirports": ["BWI", "DMW", "ESN", "FME", "GAI", "MRB", "MTN"]
            }
          ],
          "videoMapIds": ["01GFC38DNVH9H0K45ZMNT0AMDY"]
        }
      }
    ]
  }
}
```

### Profile Generation Flow
```
User selects CRC profile
  ↓
TraconSelectionWindow (select TRACON)
  ↓
AreaSelectionWindow (if multiple areas exist)
  ↓
VideoMapSelectionWindow (enter profile name, select video map)
  ↓
ProfileGeneratorService.GenerateFromCrc(
    crcProfile,
    outputDir,
    selectedTracon,
    selectedVideoMap,
    crcVideoMapFolder,
    selectedArea,        // <-- NEW parameter
    customProfileName    // <-- NEW parameter
)
  ↓
Profile saved as: {DGScope}\profiles\{ARTCC}\{FacilityID}_{ProfileName}.xml
```

### Build Commands

```bash
# Build (close app first if running!)
cd "c:\Users\yanjz\Documents\VSCode Projects\DGScope-profile-manager"
dotnet build

# Run
dotnet run --project src/DGScopeProfileManager

# Publish executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `src/DGScopeProfileManager/bin/Release/net10.0-windows/win-x64/publish/DGScopeProfileManager.exe`

## Known Issues

1. **Build Lock Error**:
   - **Symptom**: `MSB3027: Could not copy... file is locked`
   - **Solution**: Close running application before building

2. **XAML Intellisense Errors**:
   - `ProfileNameBox` may show as not existing in code-behind
   - **Solution**: Build project to generate XAML code-behind classes

## Next Steps

### Immediate (This Session)
- [x] Create GitHub repository
- [ ] Push code to GitHub
- [ ] Test the application with real CRC data
- [ ] Verify area selection works correctly
- [ ] Test profile naming with special characters

### Short-term (Next Session)
- [ ] Handle single-area facilities gracefully (auto-select without dialog)
- [ ] Add better error messages for invalid profile names
- [ ] Test NEXRAD selection with various facility locations
- [ ] Verify altimeter station generation for different areas

### Future Enhancements (See TODO.md)
- Default settings template management
- Profile backfill from existing configurations
- Bulk profile generation
- Profile comparison and merge tools

## GitHub Repository Setup

### Creating the Repository

1. Go to GitHub.com and create a new repository
2. Repository name: `DGScope-profile-manager`
3. Description: "WPF application for managing and generating DGScope radar profiles from CRC configuration data"
4. Set as Public or Private (your choice)
5. **Do NOT** initialize with README (we already have one)

### Pushing to GitHub

```bash
cd "c:\Users\yanjz\Documents\VSCode Projects\DGScope-profile-manager"

# Add remote repository (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/DGScope-profile-manager.git

# Push code
git branch -M main
git push -u origin main
```

### Repository Topics (Suggested)
- `dgscope`
- `vatsim`
- `radar`
- `crc`
- `profile-manager`
- `wpf`
- `csharp`
- `dotnet`
- `air-traffic-control`

## Testing Checklist

When testing the application:

- [ ] Settings window saves paths correctly
- [ ] CRC profiles load from configured folder
- [ ] TRACON filtering works (only shows TRACON/RAPCON/CERAP/RATCF)
- [ ] Area selection dialog appears when facility has multiple areas
- [ ] Area selection shows correct airport lists
- [ ] Profile name textbox accepts valid names
- [ ] Profile name validation rejects invalid characters
- [ ] Generated profile has correct filename: `{FacilityID}_{CustomName}.xml`
- [ ] Altimeter stations populated correctly
  - Check if using selected area's airports OR aggregate
- [ ] NEXRAD station selected based on facility location
- [ ] Video map copied with human-readable name
- [ ] Profile loads correctly in DGScope

## Debugging Tips

### Enable Debug Output
Debug messages are written to Output window (Visual Studio) or debug console.

**Key debug messages to look for**:
```
Found X areas and Y unique ssaAirports for PCT
Using ssaAirports from selected area 'Chesapeake': BWI, DMW, ...
✓ Added 7 altimeter stations: KBWI, KDMW, ...
✓ Selected NEXRAD station: KLWX (Sterling) - 25.3 NM away
✓ Copied video map: C:\...\CRC\VideoMaps\ZDC\01GFC38.geojson -> ...
```

### Common Issues

**No altimeter stations**:
- Check debug output for "Found X areas"
- Verify selected area has ssaAirports in CRC JSON
- Use Python to inspect JSON:
  ```python
  import json
  data = json.load(open('ZDC.json'))
  facility = [f for f in data['facility']['childFacilities'] if f['id'] == 'PCT'][0]
  area = facility['starsConfiguration']['areas'][0]
  print(area['ssaAirports'])
  ```

**Video map not found**:
- Verify CRC Video Map folder path in settings
- Check video map ID exists in CRC\VideoMaps\{ARTCC}\{ID}.geojson
- Look for debug message showing attempted path

## Project Context for AI Assistants

This is a Windows desktop application built with WPF targeting .NET 10.0. The primary goal is to automate DGScope profile creation from CRC (vERAM/vSTARS) configuration files.

**Key Design Decisions**:
1. **Automatic Configuration**: Minimize user input by extracting settings from CRC
2. **Multi-Area Support**: Allow users to select specific radar positions
3. **Custom Naming**: Enable multiple profiles per facility
4. **Template Preservation**: Use existing profiles as templates when available

**Code Patterns**:
- Services layer for business logic
- Models for data representation
- XAML for UI with code-behind (not full MVVM)
- System.Text.Json for JSON parsing
- System.Xml.Linq for XML manipulation

**When continuing work**:
1. Read this file for current state
2. Check TODO.md for future features
3. See DEVELOPMENT.md for architecture details
4. Review git log for recent changes

---

**Ready to Continue**: Yes, all features implemented and documented. Ready for GitHub push and testing.
