# DGScope Profile Manager

A WPF application for managing and generating DGScope radar simulation profiles from CRC (vERAM/vSTARS) configuration data.

## Overview

DGScope Profile Manager automates the creation of DGScope XML profiles by extracting facility information from CRC JSON files. It automatically configures:

- **Altimeter stations** from CRC facility areas (ssaAirports)
- **NEXRAD weather radar** based on proximity to the facility
- **Home location and coordinates** from facility areas
- **Video maps** with human-readable filenames
- **Receiver configuration** based on facility location

## Features

- **CRC Profile Import**: Reads ARTCC profiles from `AppData\Local\CRC\ARTCCs`
- **TRACON Selection**: Choose from available TRACONs, RAPCONs, CERAPs, and RATCFs
- **Area Selection**: Select specific areas when facilities have multiple radar positions
- **Custom Profile Names**: Create multiple profiles per facility (e.g., `ACY_main.xml`, `ACY_backup.xml`)
- **Automatic Configuration**: All facility-specific settings configured automatically from CRC data
- **Profile Management**: Browse, view, and manage existing DGScope profiles
- **Direct DGScope Launch**: Launch DGScope directly with selected profile, bypassing file selection dialog
- **Apply-to-All Defaults**: Set default settings once and apply to all generated profiles
- **Profile Editor**: Edit existing profiles with live preview

## Installation

### Prerequisites

- **.NET 10.0 Runtime** or later
- **Windows 10/11** (WPF application)
- **CRC (vERAM/vSTARS)** installed with ARTCC profiles

### Setup

1. Download the latest release
2. Extract to a folder
3. Run `DGScopeProfileManager.exe`
4. Configure paths in Settings:
   - **CRC Folder**: Path to CRC installation (e.g., `C:\Users\{username}\AppData\Local\CRC`)
   - **DGScope Folder**: Path to DGScope profiles root (contains ARTCC folders)
   - **DGScope Executable**: Path to DGScope.exe to enable "Launch DGScope" feature

## Usage

### Generating a New Profile

1. **Select ARTCC Profile**: Choose from the available CRC ARTCC profiles (e.g., ZNY, ZDC)
2. **Select TRACON**: Pick a TRACON/RAPCON facility from the list
3. **Select Area** (if applicable): If the facility has multiple areas, choose one
4. **Set Profile Name**: Enter a custom name or use the default facility ID
5. **Select Video Map**: Choose the video map to use
6. **Generate**: Profile is created at `{DGScope}\profiles\{ARTCC}\{FacilityID}_{ProfileName}.xml`

### Using Apply-to-All Defaults

1. Open Settings and configure your preferred default settings
2. Click "Apply to All" to set as template for all new profiles
3. Generate new profiles - they will inherit these defaults
4. Individual profiles can still be edited after creation

### Launching DGScope

1. **Configure**: Set DGScope.exe path in Settings (one-time setup)
2. **Select Profile**: Click on any profile in the profile list
3. **Launch**: Click the green "Launch DGScope" button
4. DGScope opens directly with the selected profile, skipping file selection

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

## Future Enhancements

See [TODO.md](TODO.md) for planned features:

- Default settings template management
- Profile backfill from existing configurations
- Bulk profile generation
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

[To be determined]

## Credits

Developed for the VATSIM community to streamline DGScope profile management.

## Support

For issues, feature requests, or questions:
- GitHub Issues: [Create an issue](https://github.com/yourusername/DGScope-profile-manager/issues)

## Version History

### v1.0.0 (In Development)
- Initial release
- CRC profile import with TRACON/RAPCON/CERAP/RATCF filtering
- Automatic configuration from CRC data
- Area selection for multi-area facilities
- Custom profile naming (e.g., `ACY_main.xml`)
- NEXRAD auto-selection based on proximity
- Altimeter station auto-configuration with ICAO prefixes
- Video map copying with human-readable filenames
- Profile browsing and management
- **NEW**: Direct DGScope launch integration - open profiles with one click
- **NEW**: Apply-to-All defaults - set template settings for all new profiles
- **NEW**: Unified profile editor with live XML preview
- **NEW**: Headless testing mode for automated testing
- Fixed null reference warnings in default settings
- Improved loading indicators with spinners
