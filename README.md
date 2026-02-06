# DGScope Profile Manager

A WPF application for managing and generating DGScope radar simulation profiles from CRC STARS configuration data.

## Overview

DGScope Profile Manager automates the creation of DGScope XML profiles by extracting facility information from CRC JSON files. It automatically configures:

- **Altimeter stations** from CRC facility areas (ssaAirports)
- **NEXRAD weather radar** based on proximity to the facility
- **Home location and coordinates** from facility areas
- **Video maps** with human-readable filenames
- **Receiver configuration** based on facility location

## Features

### Core Features
- **CRC Profile Import**: Reads ARTCC profiles from `AppData\Local\CRC\ARTCCs`
- **Hierarchical Facility Browser**: Browse ARTCCs → TRACONs → Areas → PrefSets in a tree view
- **Automatic Configuration**: All facility-specific settings configured automatically from CRC data
- **Profile Management**: Browse, view, and manage existing DGScope profiles
- **Direct DGScope Launch**: Launch DGScope directly with selected profile

### Batch Generation (v1.3.0)
- **Multi-Select**: Check multiple facilities or areas using checkboxes in the tree view
- **Select All**: Quickly select all facilities in an ARTCC or entire regions
- **Batch Generate**: Generate profiles for all selected items at once with progress tracking
- **Inline Configuration**: All options visible in a side panel - no dialog windows needed

### Profile Configuration
- **PrefSet Support**: Apply CRC PrefSet configurations (brightness, range, leader direction, etc.)
- **Video Maps**: Multi-map support with DCB button assignments
- **Apply-to-All Defaults**: Set default settings once and apply to all generated profiles
- **Profile Editor**: Edit existing profiles with live preview

### Quality of Life
- **Auto-Update**: Automatically checks for updates and offers one-click installation
- **Locale Support**: Correctly handles decimal separators across different system locales
- **Auto-Close DGScope**: Option to automatically close DGScope before installing updates

## Installation

### Prerequisites

- **Windows 10/11** (WPF application)
- **CRC** installed with ARTCC profiles

> **Note**: The release bundle is self-contained and includes the .NET 8.0 runtime - no separate installation required.

### Setup

1. Download the latest release
2. Extract to a folder
3. Run `DGScopeProfileManager.exe`
4. Configure paths in Settings:
   - **CRC Folder**: Path to CRC installation (e.g., `C:\Users\{username}\AppData\Local\CRC`)
   - **DGScope Folder**: Path to DGScope profiles root (contains ARTCC folders)
   - **DGScope Executable**: Path to DGScope.exe to enable "Launch DGScope" feature

## Usage

### Single Profile Generation

1. **Browse CRC Tree**: Expand the left panel to see ARTCCs → Facilities → Areas → PrefSets
2. **Select Item**: Check the checkbox next to a single area or facility
3. **Configure Options**: The center panel shows generation options:
   - Facility ID override
   - Auto-select video maps checkbox
   - PrefSet selection (optional)
   - Profile name
4. **Generate**: Click "Generate Profile" - profile is created at `{DGScope}\profiles\{ARTCC}\{FacilityID}_{ProfileName}.xml`

### Batch Generation

1. **Multi-Select**: Check multiple items in the CRC tree
   - Check an ARTCC to select all its facilities
   - Check a facility to select all its areas
   - Or check individual areas
2. **Use Batch Menu**: `Batch` menu provides quick selection:
   - "Select All in Current ARTCC" - selects all facilities in the expanded ARTCC
   - "Select All Facilities" - selects everything
   - "Clear All Selections" - deselects everything
3. **Generate All**: Click "Generate N Profiles" button
4. **Progress Tracking**: Progress bar shows current item and overall progress
5. **Cancel**: Click Cancel to stop batch generation (partial results are kept)

### Launching DGScope

1. **Select Profile**: Click on any profile in the right panel
2. **Launch**: Click the green "Launch DGScope" button
3. DGScope opens directly with the selected profile

### Using Apply-to-All Defaults

1. Open `Tools` → `DGScope Default Settings`
2. Configure your preferred default settings (brightness, range, leader direction, etc.)
3. Click "Save" to set as template for all new profiles
4. Generate new profiles - they will inherit these defaults

### Editing Profiles

1. Select an existing profile from the list
2. Click "Edit Profile" to modify settings
3. Changes are saved immediately
4. Live preview shows the updated XML

### Profile Structure

Generated profiles are organized as:
```
DGScope/
└── profiles/
    ├── ZNY/
    │   ├── N90_main.xml
    │   ├── N90_backup.xml
    │   └── VideoMaps/
    │       └── N90_JFK_Cab.geojson
    └── ZDC/
        ├── PCT_main.xml
        └── VideoMaps/
            └── PCT_DCA_Cab.geojson
```

## Configuration Details

### Altimeter Stations

Automatically extracted from CRC's `starsConfiguration.areas[].ssaAirports` with proper ICAO prefixes:
- **'K' prefix**: Standard CONUS airports (e.g., ACY → KACY)
- **'P' prefix**: Pacific stations (ZAN → PZAN, HCF → PHCF)

When an area is selected, only that area's airports are used. Otherwise, all airports from all areas are aggregated.

### NEXRAD Station

Automatically selected based on facility location using great-circle distance:
- Prefers **WSR-88D** (NEXRAD) over TDWR if within 20% distance
- Default download interval: 300 seconds

### Location Configuration

Priority order for facility location:
1. Selected area's visibility center
2. TRACON's first area visibility center
3. ARTCC profile's visibility center

Updates the following profile elements:
- `HomeLocation`
- `CurrentPrefSet.ScreenCenterPoint`
- `CurrentPrefSet.RangeRingLocation`

## File Locations

- **CRC Profiles**: `%LOCALAPPDATA%\CRC\ARTCCs\{ARTCC}.json`
- **CRC Video Maps**: `%LOCALAPPDATA%\CRC\VideoMaps\{ARTCC}\{MapID}.geojson`
- **DGScope Profiles**: `{DGScope}\profiles\{ARTCC}\{FacilityID}_{Name}.xml`
- **Video Maps**: `{DGScope}\profiles\{ARTCC}\VideoMaps\{FacilityID}_{MapName}.geojson`
- **App Settings**: `%APPDATA%\DGScopeProfileManager\settings.json`
- **NEXRAD Stations**: `nexrad-stations.txt` (bundled with application)

## Technical Details

### CRC JSON Structure

The application parses the following structure from CRC JSON files:

```json
{
  "facility": {
    "childFacilities": [
      {
        "id": "PCT",
        "name": "Potomac TRACON",
        "type": "TRACON",
        "starsConfiguration": {
          "areas": [
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
  },
  "videoMaps": [
    {
      "id": "01GFC38DNVH9H0K45ZMNT0AMDY",
      "sourceFileName": "IAD Cab.geojson",
      "tags": ["IAD", "Cab"]
    }
  ]
}
```

### Dependencies

- **System.Text.Json**: JSON parsing for CRC profiles
- **System.Xml.Linq**: XML manipulation for DGScope profiles
- **.NET 10.0 WPF**: UI framework

## Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/DGScope-profile-manager.git
cd DGScope-profile-manager

# Build
dotnet build

# Run
dotnet run --project src/DGScopeProfileManager

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `src/DGScopeProfileManager/bin/Release/net10.0-windows/win-x64/publish/DGScopeProfileManager.exe`

## Release Bundles

Complete bundles (Profile Manager + DGScope + profiles) are automatically built by GitHub Actions and available at:
https://github.com/yanjz124/DGScope-profile-manager/releases

**Bundle Contents:**
- ProfileManager/ - Ready-to-run Profile Manager executable
- scope/ - Prebuilt DGScope from [yanjz124/scope](https://github.com/yanjz124/scope)
- profiles/ - Empty ARTCC profile directories
- README.md - Quick start guide

**Auto-Detection:** Profile Manager automatically detects bundled scope.exe - no configuration needed!

For building releases locally, see [RELEASE_PROCESS.md](RELEASE_PROCESS.md)

## Future Enhancements

See [TODO.md](TODO.md) for planned features:

- Profile comparison and merge tools
- Color picker for visual color selection
- Profile import/export for backup

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for:
- Architecture overview
- Code structure
- Data flow diagrams
- Contributing guidelines

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Credits

Developed for the VATSIM community to streamline DGScope profile management.

## Support

For issues, feature requests, or questions:
- GitHub Issues: [Create an issue](https://github.com/yourusername/DGScope-profile-manager/issues)

## Version History

### v1.3.0
- **UI Overhaul**: Replaced dialog-based workflow with inline configuration panel
- **Batch Generation**: Select multiple facilities/areas with checkboxes and generate all at once
- **Multi-Select TreeView**: Hierarchical selection with cascading (select ARTCC → all children selected)
- **Progress Tracking**: Real-time progress bar during batch generation with cancel support
- **Batch Menu**: Quick actions for "Select All in ARTCC", "Select All Facilities", "Clear All"
- **Locale Fix**: Properly handles decimal separators on European systems (comma vs period)

### v1.2.0
- **Auto-Update**: Automatic update checking with one-click installation
- **PrefSets in Tree**: Browse and select PrefSets directly in the facility tree
- **Auto-Close DGScope**: Option to close DGScope before installing updates
- **Smart Dialog Skipping**: Skip redundant selection dialogs when only one option exists

### v1.1.0
- **PrefSet Support**: Apply CRC PrefSet configurations to profiles
- **Multi-Map Support**: Handle multiple video maps with DCB button assignments
- **Improved Coordinates**: Better handling of area visibility centers

### v1.0.0
- Initial release
- CRC profile import with TRACON/RAPCON/CERAP/RATCF filtering
- Automatic configuration from CRC data
- Area selection for multi-area facilities
- Custom profile naming (e.g., `ACY_main.xml`)
- NEXRAD auto-selection based on proximity
- Altimeter station auto-configuration with ICAO prefixes
- Video map copying with human-readable filenames
- Profile browsing and management
- Direct DGScope launch integration
- Apply-to-All defaults template
- Unified profile editor with live XML preview
