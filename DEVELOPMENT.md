# DGScope Profile Manager - Development Documentation

## Project Architecture

### Technology Stack

- **Language**: C# 12
- **Framework**: .NET 10.0 (Preview)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **JSON Parsing**: System.Text.Json
- **XML Manipulation**: System.Xml.Linq

### Project Structure

```
DGScope-profile-manager/
├── src/
│   └── DGScopeProfileManager/
│       ├── Models/                          # Data models
│       │   ├── AppSettings.cs              # Application settings
│       │   ├── CrcProfile.cs               # CRC profile models (CrcProfile, CrcTracon, CrcArea, VideoMapInfo)
│       │   ├── DgScopeProfile.cs           # DGScope profile model
│       │   ├── Facility.cs                 # Hierarchical folder structure
│       │   └── NexradStation.cs            # NEXRAD weather station data
│       ├── Services/                        # Business logic layer
│       │   ├── AppSettingsService.cs       # Settings persistence
│       │   ├── CrcProfileReader.cs         # Parse CRC JSON files
│       │   ├── DgScopeProfileService.cs    # Load/save DGScope XML
│       │   ├── FacilityScanner.cs          # Scan profile folders
│       │   ├── NexradService.cs            # NEXRAD station selection
│       │   ├── ProfileGeneratorService.cs  # Generate profiles from CRC
│       │   └── VideoMapService.cs          # Copy/manage video maps
│       ├── Views/                           # WPF windows and dialogs
│       │   ├── AreaSelectionWindow.xaml    # Area selection for multi-area facilities
│       │   ├── ProfileConfigDialog.xaml    # Configuration confirmation
│       │   ├── SettingsWindow.xaml         # Application settings
│       │   ├── TraconSelectionWindow.xaml  # TRACON selection
│       │   └── VideoMapSelectionWindow.xaml # Video map and profile name selection
│       ├── Resources/                       # Embedded resources
│       │   └── DefaultTemplate.xml         # Default profile template (626 lines from PCT.xml)
│       ├── MainWindow.xaml                 # Main application window
│       └── App.xaml                        # Application entry point
├── nexrad-stations.txt                     # NEXRAD station database
├── README.md                               # User documentation
├── DEVELOPMENT.md                          # This file
└── TODO.md                                 # Future feature roadmap
```

## Data Models

### CrcProfile (Models/CrcProfile.cs)

Represents a CRC ARTCC profile loaded from `%LOCALAPPDATA%\CRC\ARTCCs\{ARTCC}.json`.

```csharp
public class CrcProfile
{
    public string Name { get; set; }                    // Filename without extension
    public string Path { get; set; }                    // Full file path
    public string ArtccCode { get; set; }               // ARTCC identifier (e.g., ZDC)
    public List<VideoMapInfo> VideoMaps { get; set; }   // Available video maps
    public List<CrcTracon> Tracons { get; set; }        // Child facilities
    public double? HomeLatitude { get; set; }           // ARTCC center latitude
    public double? HomeLongitude { get; set; }          // ARTCC center longitude
}
```

### CrcTracon (Models/CrcProfile.cs)

Represents a TRACON/RAPCON/CERAP/RATCF facility within an ARTCC.

```csharp
public class CrcTracon
{
    public string Id { get; set; }                              // Facility identifier (e.g., PCT)
    public string Name { get; set; }                            // Full name
    public string Type { get; set; }                            // Facility type
    public double? Latitude { get; set; }                       // First area latitude
    public double? Longitude { get; set; }                      // First area longitude
    public List<VideoMapInfo> AvailableVideoMaps { get; set; } // Video maps for this facility
    public List<string> SsaAirports { get; set; }              // Aggregate SSA airports from all areas
    public List<CrcArea> Areas { get; set; }                   // Radar positions/areas

    public bool IsControlledFacility()                         // Check if TRACON/RAPCON/CERAP/RATCF
    public List<string> GetAltimeterStations()                 // Convert SSA airports to ICAO format
}
```

### CrcArea (Models/CrcProfile.cs)

Represents a radar area/position within a facility.

```csharp
public class CrcArea
{
    public string Id { get; set; }                      // Unique area ID
    public string Name { get; set; }                    // Area name (e.g., "Chesapeake")
    public double? Latitude { get; set; }               // Area center latitude
    public double? Longitude { get; set; }              // Area center longitude
    public List<string> SsaAirports { get; set; }      // SSA airports for this area
    public string AirportsDisplay { get; }              // Display string for UI
}
```

### VideoMapInfo (Models/CrcProfile.cs)

Represents a video map reference from CRC.

```csharp
public class VideoMapInfo
{
    public string SourceFileName { get; set; }          // Human-readable filename
    public string Id { get; set; }                      // Hash-based ID for file lookup
    public List<string> Tags { get; set; }              // Map tags
}
```

### NexradStation (Models/NexradStation.cs)

Represents a NEXRAD weather radar station.

```csharp
public class NexradStation
{
    public string Icao { get; set; }                    // Station ICAO code
    public string Name { get; set; }                    // Station name
    public string StationType { get; set; }             // WSR-88D or TDWR
    public double Latitude { get; set; }                // Station latitude
    public double Longitude { get; set; }               // Station longitude
    public int Elevation { get; set; }                  // Elevation in feet

    public double DistanceToNauticalMiles(lat, lon)    // Haversine distance calculation
}
```

## Services

### CrcProfileReader

**Purpose**: Parse CRC JSON files and extract facility information.

**Key Methods**:
- `GetAllProfiles()`: Scan CRC directory for JSON files
- `LoadProfile(string filePath)`: Parse a single CRC JSON file

**Data Extraction**:
1. Extract ARTCC code from filename
2. Parse `videoMaps` array for map metadata
3. Extract `visibilityCenters` for ARTCC home location
4. Parse `facility.childFacilities` for TRACONs
5. For each TRACON:
   - Extract basic info (id, name, type)
   - Parse `starsConfiguration.areas[]` for radar positions
   - Collect `ssaAirports` from each area
   - Extract `visibilityCenter` from first area
   - Map `videoMapIds` to `VideoMapInfo` objects

**CRC JSON Path Structure**:
```
facility.childFacilities[].starsConfiguration.areas[].ssaAirports[]
facility.childFacilities[].starsConfiguration.areas[].visibilityCenter
facility.childFacilities[].starsConfiguration.videoMapIds[]
```

### ProfileGeneratorService

**Purpose**: Generate DGScope XML profiles from CRC data.

**Generation Process**:
1. **Template Loading**:
   - Search for similar existing profile in output directory
   - If found, use it as template (preserves custom settings)
   - Otherwise, load embedded `DefaultTemplate.xml` resource

2. **Video Map Handling**:
   - Copy GeoJSON from `CRC\VideoMaps\{ARTCC}\{MapID}.geojson`
   - Rename to human-readable format: `{FacilityID}_{SourceFileName}`
   - Update XML `<VideoMapFilename>` element

3. **Location Configuration**:
   - Priority: selectedArea > selectedTracon > crcProfile
   - Update three XML elements:
     - `<HomeLocation>` (Latitude/Longitude)
     - `<CurrentPrefSet><ScreenCenterPoint>`
     - `<CurrentPrefSet><RangeRingLocation>`

4. **Altimeter Stations**:
   - Use selected area's ssaAirports if area specified
   - Otherwise aggregate all airports from all areas
   - Convert to ICAO format:
     - Add 'K' prefix for CONUS (e.g., ACY → KACY)
     - Add 'P' prefix for Pacific (ZAN → PZAN, HCF → PHCF)

5. **Receiver Configuration**:
   - Calculate receiver positions relative to facility location
   - Maintain existing receiver structure from template

6. **NEXRAD Selection**:
   - Use `NexradService` to find closest station
   - Update `<NexradSensorId>` and `<NexradDownloadInterval>`

7. **Profile Naming**:
   - Format: `{FacilityID}_{CustomName}.xml` (if custom name provided)
   - Format: `{FacilityID}.xml` (default)

### NexradService

**Purpose**: Automatically select appropriate NEXRAD weather radar station.

**Selection Algorithm**:
1. Load stations from `nexrad-stations.txt` (fixed-width format)
2. Calculate great-circle distance (Haversine formula) to each station
3. Prefer WSR-88D over TDWR if within 20% distance
4. Return closest station

**Station Types**:
- **WSR-88D**: NEXRAD Doppler radar (preferred)
- **TDWR**: Terminal Doppler Weather Radar (local coverage)

### DgScopeProfileService

**Purpose**: Load and manipulate DGScope XML profiles.

**Capabilities**:
- Parse XML with proper encoding
- Extract settings as dictionary
- Update individual elements
- Save with proper formatting

### FacilityScanner

**Purpose**: Scan hierarchical DGScope profile folder structure.

**Folder Structure**:
```
DGScope/profiles/
└── {ARTCC}/           # e.g., ZDC
    ├── {Facility}/    # e.g., PCT
    │   └── *.xml      # Profile files
    └── VideoMaps/     # Shared video maps
```

### AppSettingsService

**Purpose**: Persist application settings.

**Settings Location**: `%APPDATA%\DGScopeProfileManager\settings.json`

**Settings**:
```json
{
  "CrcFolderPath": "C:\\Users\\{user}\\AppData\\Local\\CRC",
  "DgScopeFolderPath": "C:\\Path\\To\\DGScope",
  "CrcArtccFolderPath": "C:\\Users\\{user}\\AppData\\Local\\CRC\\ARTCCs",
  "CrcVideoMapFolderPath": "C:\\Users\\{user}\\AppData\\Local\\CRC\\VideoMaps"
}
```

## User Interface Flow

### Profile Generation Workflow

```
MainWindow
  ↓ (Select CRC Profile, Click "Generate Profile")
TraconSelectionWindow
  ↓ (Select TRACON/RAPCON)
AreaSelectionWindow (if facility has multiple areas)
  ↓ (Select specific area)
VideoMapSelectionWindow
  ↓ (Enter profile name, Select video map)
ProfileGeneratorService
  ↓ (Generate XML)
Success Message
```

### Window Details

#### Main Window
- **Left Panel**: CRC profiles list
- **Right Panel**: DGScope profiles tree (ARTCC → Facility → Profiles)
- **Action Buttons**:
  - Scan Folders
  - Generate Profile
  - Fix All Paths (convert absolute → relative)
  - Apply Batch Settings
  - Edit/Delete profile

#### TRACON Selection Window
- Displays filtered facilities (TRACON, RAPCON, CERAP, RATCF only)
- Shows facility name and type

#### Area Selection Window (New in v1.0)
- **Shown when**: Facility has multiple areas (multi-position facility)
- **Displays**: Area name and associated airports
- **Purpose**: User selects specific radar position

#### Video Map Selection Window
- **Profile Name Input**: Text box for custom naming
  - Pre-filled with facility ID
  - Validation for invalid filename characters
- **Video Map List**: Available maps with tags
- **Output**: Custom profile name + selected video map

## Key Algorithms

### Haversine Distance Calculation

Used for NEXRAD station selection. Calculates great-circle distance between two lat/lon points.

```csharp
public double DistanceToNauticalMiles(double targetLat, double targetLon)
{
    const double EarthRadiusKm = 6371.0;
    const double KmToNauticalMiles = 0.539957;

    // Convert degrees to radians
    var dLat = ToRadians(targetLat - Latitude);
    var dLon = ToRadians(targetLon - Longitude);

    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(targetLat)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    var distanceKm = EarthRadiusKm * c;

    return distanceKm * KmToNauticalMiles;
}
```

### SSA Airport to ICAO Conversion

Converts 3-letter airport codes to 4-letter ICAO altimeter station codes.

```csharp
public List<string> GetAltimeterStations()
{
    return SsaAirports.Select(airport =>
    {
        var upper = airport.ToUpper();
        // Pacific stations need 'P' prefix
        if (upper == "ZAN" || upper == "HCF")
            return "P" + upper;
        // All others get 'K' prefix
        return "K" + upper;
    }).ToList();
}
```

## Build Configuration

### Project File (DGScopeProfileManager.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <!-- Embed default profile template -->
    <EmbeddedResource Include="Resources\DefaultTemplate.xml" />

    <!-- Copy NEXRAD stations to output -->
    <None Include="..\..\nexrad-stations.txt">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

### Build Commands

```bash
# Development build
dotnet build

# Run from source
dotnet run --project src/DGScopeProfileManager

# Release build (single-file executable)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Testing Strategy

### Unit Testing Considerations

1. **CrcProfileReader**:
   - Test JSON parsing with various ARTCC structures
   - Verify area extraction and ssaAirports parsing
   - Test with malformed JSON

2. **ProfileGeneratorService**:
   - Mock file I/O operations
   - Verify XML element updates
   - Test area vs. TRACON priority logic
   - Validate profile naming logic

3. **NexradService**:
   - Test distance calculations with known coordinates
   - Verify WSR-88D preference logic
   - Test with empty station list

### Integration Testing

1. Generate profile from real CRC JSON
2. Verify video map copying
3. Validate generated XML structure
4. Test with multi-area facilities

## Common Issues & Solutions

### Issue: Build Fails Due to Running Application

**Symptom**: `MSB3027: Could not copy... file is locked`

**Solution**: Close the running application before building

### Issue: VideoMapFilename Not Found

**Symptom**: Video map path in XML doesn't resolve

**Solution**:
- Check CRC Video Map folder setting
- Verify video map ID exists in CRC JSON
- Use "Fix All Paths" to convert to relative paths

### Issue: ssaAirports Not Found

**Symptom**: No altimeter stations in generated profile

**Solution**:
- Verify CRC JSON has `starsConfiguration.areas[].ssaAirports`
- Check area selection (may have no airports)
- Enable debug output to see parsing logs

## Contributing Guidelines

### Code Style

- Follow C# naming conventions
- Use XML documentation comments for public APIs
- Keep methods focused and single-purpose
- Prefer explicit types over `var` for clarity

### Adding New Features

1. Create models in `Models/` if new data structures needed
2. Implement business logic in `Services/`
3. Create UI in `Views/` with XAML + code-behind
4. Update `MainWindow.xaml.cs` to integrate workflow
5. Document in README.md and TODO.md

### Pull Request Checklist

- [ ] Code compiles without warnings
- [ ] Added XML documentation to public methods
- [ ] Tested with real CRC/DGScope data
- [ ] Updated README.md if user-facing changes
- [ ] Updated DEVELOPMENT.md if architectural changes

## Debugging Tips

### Enable Debug Output

Debug output is written using `System.Diagnostics.Debug.WriteLine()`. View in Visual Studio Output window or attach a debugger.

**Key Debug Messages**:
- CrcProfileReader: `Found {count} areas and {count} ssaAirports`
- ProfileGeneratorService: `✓ Added {count} altimeter stations`
- NexradService: `✓ Selected NEXRAD station: {code} - {distance} NM away`

### Common Debugging Scenarios

**Altimeter stations not appearing**:
1. Check debug output for "Found X ssaAirports"
2. Verify area has ssaAirports in CRC JSON
3. Use Python script to inspect JSON structure:
   ```python
   import json
   with open('ZDC.json') as f:
       data = json.load(f)
       facility = data['facility']['childFacilities'][0]
       print(facility['starsConfiguration']['areas'][0]['ssaAirports'])
   ```

**Video map not copying**:
1. Check debug output for "✓ Copied video map"
2. Verify source path exists
3. Check CRC Video Map folder setting
4. Confirm video map ID matches file in CRC folder

## Version History

### v1.0.0 (In Development - January 2026)
- Initial release
- CRC profile import with full TRACON parsing
- Area selection for multi-position facilities
- Custom profile naming
- Automatic NEXRAD and altimeter configuration
- Video map management with human-readable names
- Profile browsing and management UI

## Future Architecture Considerations

See TODO.md for planned features that may require architectural changes:

1. **Template Management System**:
   - Store user-defined default templates
   - Template versioning
   - Import/export templates

2. **Profile Backfill**:
   - Detect changes in DGScope profiles
   - Merge back into template
   - Track non-facility-specific settings

3. **Batch Generation**:
   - Generate multiple profiles at once
   - Progress tracking UI
   - Error handling for partial failures

4. **Profile Comparison**:
   - Diff two profiles
   - Merge tool for settings
   - Visual diff display
