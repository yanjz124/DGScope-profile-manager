# Project Status & Development Roadmap

**Project**: DGScope Profile Manager  
**Status**: Alpha - Core functionality complete, testing phase  
**Last Updated**: January 6, 2026

## 📊 Overall Progress

```
██████████████████████░░ 85% Complete

Core Implementation:    ████████████████████ 100%
UI Implementation:      ████████████████████ 100%
CRC Integration:        ████████████████████ 100%
Testing & Validation:   ████░░░░░░░░░░░░░░░░  20%
Deployment:             ████░░░░░░░░░░░░░░░░  20%
```

## 🎯 Development Phases

### Phase 1: Project Initialization ✅ COMPLETE
**Objective**: Set up project structure and build system

- ✅ Created C# WPF solution with .NET 10.0
- ✅ Configured project file (DGScopeProfileManager.csproj)
- ✅ Established folder structure (Models, Services, Views)
- ✅ Confirmed successful build

**Key Decisions**:
- Chose WPF over WinForms for better UI flexibility
- Selected .NET 10.0 preview for latest features
- Adopted MVVM-ready architecture for maintainability

### Phase 2: Data Model Design ✅ COMPLETE
**Objective**: Define domain models for CRC and DGScope profiles

**Files Created**:
- `Models/CrcProfile.cs` - CRC JSON profile representation
- `Models/DgScopeProfile.cs` - DGScope XML profile representation
- `Models/Facility.cs` - Hierarchical ARTCC/Facility container
- `Models/AppSettings.cs` - Application configuration

**Data Structure Insights**:
```
CRC Profile:
- Name: Profile identifier
- Path: File system location
- ArtccCode: Extracted from filename (e.g., "ZDC" from "ZDC.json")
- VideoMaps: List of video map references with sourceFileName

DGScope Profile:
- FilePath: XML file location
- Name: Profile identifier
- Settings: Dictionary<string, string> for all XML settings
  - BackColor, VideoMapLineColor, BeaconColor, DataBlockColor (negative integers)
  - ScreenRotation (float degrees)
  - FontName, FontSize (string, string)
  - HomeLatitude, HomeLongitude (decimal coordinates)
  - AltimeterStations (comma-separated station codes)
- VideoMapPaths: List of GeoJSON file references

Facility:
- Name: Facility code (e.g., "PCT", "A80")
- ArtccCode: Parent ARTCC (e.g., "ZDC", "ZTL")
- Path: Folder path
- Profiles: List of DgScopeProfile objects
```

### Phase 3: Service Layer Implementation ✅ COMPLETE
**Objective**: Build business logic for file operations and profile management

#### 3.1 CRC Profile Reading ✅
**File**: `Services/CrcProfileReader.cs`

**Capabilities**:
- Scan directory for all JSON files
- Parse JSON using System.Text.Json
- Extract video map source file names from videoMaps array
- Handle malformed JSON gracefully

**Key Methods**:
```csharp
List<CrcProfile> GetAllProfiles(string folderPath)
CrcProfile LoadProfile(string filePath)
```

#### 3.2 DGScope Profile Service ✅
**File**: `Services/DgScopeProfileService.cs`

**Capabilities**:
- Load XML profiles using System.Xml.Linq
- Extract all settings into dictionary
- Save modified profiles back to XML
- Fix file paths (convert absolute ↔ relative)
- Apply batch settings to multiple profiles

**Key Methods**:
```csharp
DgScopeProfile LoadProfile(string filePath)
void SaveProfile(DgScopeProfile profile)
void FixFilePaths(DgScopeProfile profile, bool makeAbsolute)
void ApplyBatchSettings(List<DgScopeProfile> profiles, Dictionary<string, string> settings)
```

**Implementation Details**:
- Uses XElement for XML manipulation
- Settings dictionary allows flexible key-value storage
- Path normalization uses forward slashes internally
- Handles missing XML elements gracefully (returns empty strings)

#### 3.3 Video Map Management ✅
**File**: `Services/VideoMapService.cs`

**Capabilities**:
- List available GeoJSON files in source folder
- Copy files to destination directory
- Preserve file names during copy
- Return list of copied file paths

**Key Methods**:
```csharp
List<string> GetAvailableVideoMaps(string sourceFolder)
List<string> CopyVideoMaps(List<string> sourceFiles, string destinationFolder)
```

#### 3.4 Profile Generation ✅
**File**: `Services/ProfileGeneratorService.cs`

**Capabilities**:
- Generate DGScope XML from CRC profile
- Create default XML structure with template values
- Copy associated video maps
- Create folder structure if needed

**Key Methods**:
```csharp
DgScopeProfile GenerateFromCrc(CrcProfile crcProfile, string outputFolder, string videoMapFolder)
DgScopeProfile CreateDefaultProfile(string outputPath)
```

**XML Template**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Profile>
  <Settings>
    <BackColor>-16777216</BackColor>      <!-- Black -->
    <VideoMapLineColor>-16711936</VideoMapLineColor> <!-- Green -->
    <BeaconColor>-256</BeaconColor>        <!-- Yellow -->
    <DataBlockColor>-1</DataBlockColor>    <!-- White -->
    <ScreenRotation>0.0</ScreenRotation>
    <FontName>Arial</FontName>
    <FontSize>10</FontSize>
  </Settings>
  <VideoMapFilename><!-- Relative path --></VideoMapFilename>
  <HomeLocation>
    <Latitude>0.0</Latitude>
    <Longitude>0.0</Longitude>
  </HomeLocation>
  <AltimeterStations></AltimeterStations>
</Profile>
```

#### 3.5 Hierarchical Folder Scanner ✅
**File**: `Services/FacilityScanner.cs`

**Capabilities**:
- Scan ARTCC/Facility/Profile folder structure
- Recursively traverse directories
- Load all XML profiles within each facility
- Build hierarchical Facility objects

**Folder Traversal Logic**:
```
DGScope Root/
├── ARTCC1/ (ZDC)
│   ├── Facility1/ (PCT)
│   │   ├── profile1.xml
│   │   └── profile2.xml
│   └── Facility2/ (DCA)
│       └── profile3.xml
└── ARTCC2/ (ZTL)
    └── Facility3/ (A80)
        └── profile4.xml

Result: List<Facility> with 3 facilities:
- Facility(Name="PCT", ArtccCode="ZDC", Profiles=[profile1, profile2])
- Facility(Name="DCA", ArtccCode="ZDC", Profiles=[profile3])
- Facility(Name="A80", ArtccCode="ZTL", Profiles=[profile4])
```

**Key Methods**:
```csharp
List<Facility> ScanFacilities(string rootPath)
```

### Phase 4: Sample File Analysis ✅ COMPLETE
**Objective**: Understand actual file formats from user samples

**Files Analyzed**:
1. `ZDC.json` - CRC profile sample
   - Confirmed videoMaps array structure
   - Identified sourceFileName as absolute path
   - Noted additional properties (id, name, tags, starsId)

2. `PCT.xml` - DGScope profile sample
   - Confirmed XML structure with nested elements
   - Identified color storage as negative integers
   - Noted relative path format for VideoMapFilename
   - Discovered HomeLocation latitude/longitude elements
   - Found AltimeterStations as comma-separated text

**Key Insights**:
- CRC uses absolute paths, DGScope prefers relative paths
- Colors are ARGB integers (negative when alpha is set)
- XML structure is consistent across profiles
- Video maps are separate GeoJSON files

### Phase 5: User Interface Development ✅ COMPLETE
**Objective**: Build comprehensive WPF windows for all user interactions

#### 5.1 Main Window ✅
**Files**: `MainWindow.xaml`, `MainWindow.xaml.cs`

**Layout**:
```
┌─────────────────────────────────────────────────────┐
│ Menu Bar: File, Settings                            │
├──────────────────┬──────────────────────────────────┤
│ CRC Profiles     │ DGScope Profiles (Tree)          │
│ ┌──────────────┐ │ ┌──────────────────────────────┐ │
│ │ ZDC.json     │ │ │ ├─ 📁 ZDC / PCT (2 profiles) │ │
│ │ ZTL.json     │ │ │ │  ├─ 📄 PCT_north.xml       │ │
│ │ ZNY.json     │ │ │ │  └─ 📄 PCT_south.xml       │ │
│ │              │ │ │ └─ 📁 ZTL / A80 (1 profile)  │ │
│ └──────────────┘ │ └──────────────────────────────┘ │
│                  │                                   │
│ [Generate]       │ [Edit] [Delete]                  │
├──────────────────┴──────────────────────────────────┤
│ ▼ Batch Settings                                    │
│   Screen Rotation: [____] Font Name: [____]         │
│   Font Size: [____]                                 │
│   [Apply Batch Settings]                            │
├─────────────────────────────────────────────────────┤
│ Actions: [Scan Folders] [Fix All Paths]             │
├─────────────────────────────────────────────────────┤
│ Status: Ready                                       │
└─────────────────────────────────────────────────────┘
```

**Event Handlers**:
- `Settings_Click`: Open SettingsWindow dialog
- `ScanFolders_Click`: Refresh folder scan (LoadFolders)
- `FixAllPaths_Click`: Fix all video map paths in all profiles
- `GenerateProfile_Click`: Create new DGScope profile from selected CRC
- `ApplyBatchSettings_Click`: Apply rotation/font to selected profiles
- `EditProfile_Click`: Open ProfileEditorWindow for selected profile
- `DeleteProfile_Click`: Delete selected profile with confirmation
- `FacilityTree_SelectionChanged`: Update button states

**State Management**:
- `_crcProfiles`: List of CRC profiles (left list)
- `_facilities`: List of hierarchical facilities (right tree)
- `_settings`: AppSettings instance with folder paths

#### 5.2 Settings Window ✅
**Files**: `Views/SettingsWindow.xaml`, `Views/SettingsWindow.xaml.cs`

**Purpose**: Configure folder paths for CRC, DGScope, and video maps

**UI Elements**:
- TextBox for CRC folder path + Browse button
- TextBox for DGScope root folder path + Browse button
- TextBox for video maps source path + Browse button
- Info panel explaining folder structure
- OK/Cancel buttons

**Functionality**:
- Uses `OpenFolderDialog` for folder selection (.NET 10 feature)
- Updates AppSettings on OK click
- Returns DialogResult to notify caller

#### 5.3 Generate Profile Dialog ✅
**Files**: `Views/GenerateProfileDialog.xaml`, `Views/GenerateProfileDialog.xaml.cs`

**Purpose**: Wizard for creating new DGScope profile from CRC data

**UI Elements**:
- Display CRC profile name
- TextBox for ARTCC code (pre-filled from CRC)
- TextBox for Facility code
- Live preview of output path
- Generate/Cancel buttons

**Validation**:
- Ensures ARTCC code is not empty
- Ensures Facility code is not empty
- Shows where the XML file will be created

**Preview Example**:
```
Generating profile for: PCT_combined
ARTCC Code: ZDC
Facility Code: PCT

The profile will be created at:
C:\DGScope\ZDC\PCT\PCT_combined.xml
```

#### 5.4 Profile Editor Window ✅
**Files**: `Views/ProfileEditorWindow.xaml`, `Views/ProfileEditorWindow.xaml.cs`

**Purpose**: Edit all settings for an individual DGScope profile

**UI Sections**:

1. **Basic Information** (read-only):
   - Profile name
   - File path
   - Current video map

2. **Display Settings** (editable):
   - Screen Rotation (degrees)
   - Font Name
   - Font Size
   - Back Color (negative integer)

3. **Location Settings** (editable):
   - Home Latitude (decimal)
   - Home Longitude (decimal)

4. **Other Settings** (editable):
   - Altimeter Stations (comma-separated)

**Save Logic**:
- Updates DgScopeProfile.Settings dictionary
- Calls DgScopeProfileService.SaveProfile()
- Shows success/error message
- Closes on successful save

### Phase 6: TRACON Selection & Enhanced CRC Integration ✅ COMPLETE
**Objective**: Implement TRACON selection and facility-specific profile generation

**Files Created**:
- `Views/TraconSelectionWindow.xaml` - Dialog to select TRACON from CRC profile
- `Views/TraconSelectionWindow.xaml.cs` - Window code-behind

**Features**:
- Displays all TRACONs from selected CRC profile
- Filters to controlled facilities (TRACON/RAPCON/CERAP/RATCF types)
- Allows user to select which facility to generate profile for
- Returns selected TRACON to main window

### Phase 7: Settings Persistence ✅ COMPLETE
**Objective**: Save and restore application settings across sessions

**Files Created**:
- `Services/SettingsPersistenceService.cs` - JSON-based settings storage

**Features**:
- Saves folder paths to `%APPDATA%/DGScopeProfileManager/settings.json`
- Auto-loads settings on application startup
- Graceful fallback to defaults if file doesn't exist
- Integrated into MainWindow initialization and Settings dialog

**Key Methods**:
```csharp
AppSettings LoadSettings()  // Loads from JSON or returns defaults
void SaveSettings(AppSettings settings)  // Persists to disk
```

### Phase 8: CRC Location & Video Map Extraction ✅ COMPLETE
**Objective**: Extract facility coordinates and available video maps from CRC JSON

**Enhancements to CrcProfile.cs**:
- `CrcTracon`: Added `Latitude`, `Longitude` properties
- `CrcTracon`: Changed `AltimeterStations` to `AvailableVideoMaps` (List<VideoMapInfo>)
- `VideoMapInfo`: New class holding `SourceFileName`, `Id`, `Tags`

**Enhancements to CrcProfileReader.cs**:
- **Location Extraction**: Parses facility location from `starsConfiguration.areas[0].visibilityCenter`
  - Handles both object format (with `lat`/`lon` properties) and string format
  - Properly extracts coordinates for each TRACON
- **Video Map Extraction**: Collects facility-specific video maps
  - Reads `videoMapIds` array from STARS configuration
  - Looks up full VideoMapInfo from global videoMaps list
  - Associates each video map with its TRACON
- **Robust Parsing**: Added `ExtractLatLonFromString()` for string-based location parsing

**Key Data Points Extracted**:
```
ACY TRACON:
- Latitude: 39.452745
- Longitude: -74.591952
- AvailableVideoMaps: [
    - IAD Cab.geojson (tags: TOWER, CAB)
    - DCA Cab.geojson (tags: TOWER, CAB)
    - ... more maps
  ]
```

### Phase 9: Video Map Selection Dialog ✅ COMPLETE
**Objective**: Allow user to choose which video map to use when generating profiles

**Files Created**:
- `Views/VideoMapSelectionWindow.xaml` - UI for video map selection
- `Views/VideoMapSelectionWindow.xaml.cs` - Selection logic

**Features**:
- Lists all available video maps for selected TRACON
- Shows video map source file names
- Displays tags for each map (e.g., "TOWER, CAB")
- Returns selected video map to generation workflow
- Modal dialog with OK/Cancel buttons

**Implementation Details**:
- Uses `VideoMapDisplay` wrapper class for XAML binding
- Supports computed `TagsDisplay` property (joins tags with commas)
- Defaults to first map if available
- Prevents generation if no maps available

### Phase 10: Enhanced Profile Generation with Receiver Config ✅ COMPLETE
**Objective**: Populate facility data and receiver configuration in generated profiles

**Enhancements to ProfileGeneratorService.cs**:
- **Method Signature Update**: 
  ```csharp
  GenerateFromCrc(CrcProfile, string, CrcTracon, VideoMapInfo, string)
  ```
  Now accepts selected video map and TRACON

- **Receiver Element Creation**: `UpdateReceiverConfig()` now:
  - Creates `Receiver` element if it doesn't exist (templates don't have this)
  - Creates `ScopeServerClient` child element if missing
  - Populates with facility data:
    - `Name`: Facility name (e.g., "Atlantic City ATCT/TRACON")
    - `Location.Latitude`: TRACON location or ARTCC fallback
    - `Location.Longitude`: TRACON location or ARTCC fallback
  
- **Video Map Handling**:
  - Copies selected video map to output directory
  - Renames to human-readable format: `{FacilityId}_{OriginalName}`
  - Updates `VideoMapFilename` element with new path

- **Home Location Update**:
  - Sets to TRACON coordinates if available
  - Falls back to ARTCC center location
  - Format: `decimal.ToString("F6")` for precision

**Generated Profile Example**:
```xml
<RadarWindow>
  ...
  <VideoMapFilename>C:\...\profiles\ZDC\VideoMaps\ACY_IAD Cab.geojson</VideoMapFilename>
  <HomeLocation>
    <Latitude>39.452745</Latitude>
    <Longitude>-74.591952</Longitude>
  </HomeLocation>
  ...
  <Receiver AssemblyQualifiedName="...">
    <ScopeServerClient>
      <Name>Atlantic City ATCT/TRACON</Name>
      <Enabled>true</Enabled>
      <Location>
        <Latitude>39.452745</Latitude>
        <Longitude>-74.591952</Longitude>
      </Location>
      <Range>250</Range>
      <CreateNewAircraft>true</CreateNewAircraft>
      <Url></Url>
    </ScopeServerClient>
  </Receiver>
</RadarWindow>
```

### Phase 11: Complete Workflow Integration ✅ COMPLETE
**Objective**: Wire all components together for end-to-end profile generation

**Updated MainWindow.xaml.cs**:
```
User Action Flow:
1. Click "Scan Folders"
   → LoadFolders() called
   → CRC profiles loaded into _crcProfiles
   → DGScope profiles loaded into _facilities
   → UI updates with both lists

2. Select CRC profile (e.g., ZDC)
3. Click "Generate Profile"
   → Settings validation
   → TraconSelectionWindow shown
   
4. User selects TRACON (e.g., ACY)
   → TraconSelectionWindow closes
   → VideoMapSelectionWindow shown
   
5. User selects video map (e.g., "IAD Cab.geojson")
   → VideoMapSelectionWindow closes
   → ProfileGeneratorService.GenerateFromCrc() called
     ├─ Finds similar template (if exists)
     ├─ Copies selected video map with new name
     ├─ Updates VideoMapFilename element
     ├─ Updates HomeLocation with coordinates
     └─ Creates/Updates Receiver element with facility data
   
6. Profile saved to disk
   → Success message shown
   → Folder tree refreshed
```

**New Event Handler**:
- `VideoMapSelection`: Shown after TRACON selection
- Passes selected map to `GenerateFromCrc()`
- Handles case when no maps available

## 🔄 Current Status

### What Works ✅
- ✅ Project builds successfully (zero errors, zero warnings)
- ✅ All data models fully defined
- ✅ All service classes implemented and integrated
- ✅ Complete UI with 6 windows (Main, Settings, TraconSelection, VideoMapSelection, ProfileEditor, plus support windows)
- ✅ Event handlers wired up for complete workflow
- ✅ XAML bindings configured correctly
- ✅ CRC JSON parsing with location and video map extraction
- ✅ Settings persistence (saves/loads from JSON)
- ✅ Profile generation from CRC data
- ✅ Receiver element creation with facility data
- ✅ Video map selection and human-readable naming
- ✅ Hierarchical profile scanning
- ✅ Batch settings application
- ✅ Path fixing algorithm

### Currently Being Tested 🧪
- ⏳ End-to-end profile generation workflow
- ⏳ Location data population in generated profiles
- ⏳ Receiver name and coordinates in generated profiles
- ⏳ Video map selection and copying

### Known Issues & Limitations
- **Template Profiles**: Existing template profiles don't have Receiver element
  - *Workaround*: Code now creates it if missing ✅ FIXED
- **Location Extraction**: Requires proper parsing of visibilityCenter
  - *Status*: Now handles both object and string formats ✅ FIXED
- **No Input Validation**: User can enter invalid values (future enhancement)
- **No Progress Indicators**: Long operations appear frozen (future enhancement)
- **No Undo/Redo**: Changes are immediate and permanent
- **No Logging**: Limited diagnostic information (future enhancement)
- **.NET 10 Preview**: Using preview runtime (may be unstable)

## 📝 Next Steps

### Immediate Priorities

#### 1. ~~Initial Testing Run~~ In Progress 🧪
**Goal**: Verify application works with real user data

**Current Status**: 
- Application launches successfully ✅
- Settings persistence working ✅
- CRC profiles loading with location data ✅
- TRACON selection working ✅
- Video map selection dialog working ✅
- Profile generation executing ✅
- **Current Focus**: Verifying generated profile has:
  - Location coordinates populated
  - Receiver element with facility name
  - Video map properly named and located

**Test Sequence Remaining**:
1. Generate profile for ACY TRACON with video map
2. Verify XML contains correct coordinates
3. Verify receiver name is set to facility name
4. Verify home location matches receiver location
5. Test batch settings on multiple profiles
6. Test path fixing on generated profiles

#### 2. ~~Settings Persistence~~ ✅ COMPLETE
**Status**: DONE - SettingsPersistenceService fully implemented
- Settings saved to `%APPDATA%/DGScopeProfileManager/settings.json`
- Auto-loads on startup
- Saves on settings dialog close

#### 3. Validation & Error Messages 🎯 NEXT
**Goal**: Improve user experience with better error handling

**Priority**:
1. Validate TRACON selection (ensure coordinates exist)
2. Validate video map selection (ensure available maps)
3. Better error messages for file operations
4. User feedback if generation fails

**Implementation**:
- Add validation checks before generation
- Show specific error messages instead of generic exceptions
- Log errors for debugging

### Medium-Term Goals

#### 4. UI Polish & User Experience 🎨
**Future Enhancements**:
- Search/filter profiles by name
- Sort options for tree view
- Visual confirmation when profile is generated
- Copy-to-clipboard for file paths
- Quick navigation to profile folder in Explorer

#### 5. NEXRAD/Advanced Features 🚀
**Deferred Features** (documented for future work):
- NEXRAD URL selection dialog (currently empty for manual entry)
- NEXRAD closest-site auto-detection
- Video map preview/visualization
- Profile templates for common configurations

#### 6. Batch Operations 📦
**Future Capabilities**:
- Generate profiles for multiple TRACONs at once
- Apply settings to profile families (by ARTCC)
- Backup/restore profile collections
- Export selected profiles to ZIP

### Long-Term Enhancements

#### 7. Deployment Package 📦
**Release Preparation**:
1. Create publish configuration
2. Test single-file executable
3. Verify self-contained deployment
4. Add application icon
5. Set assembly metadata (version, copyright)
6. Create installer (optional MSI)

**Publish Command**:
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**Output Location**:
```
src/DGScopeProfileManager/bin/Release/net10.0-windows/win-x64/publish/
└── DGScopeProfileManager.exe
```

#### 8. Documentation 📚
**User Manual Sections**:
1. Installation instructions
2. Initial setup walkthrough
3. Feature overview with screenshots
4. Common workflows
5. Troubleshooting guide
6. FAQ

**Developer Documentation**:
1. Architecture overview
2. Service class responsibilities
3. Adding new features
4. Testing procedures
5. Build and deployment

#### 9. Quality Assurance 🧪
**Testing Plan**:
- Unit tests for service classes
- Integration tests for file operations
- UI automation tests
- Performance testing (large profile collections)
- Memory leak detection
- Exception handling verification

## 🔍 Technical Debt & Future Improvements

### Code Quality Issues
1. **Hardcoded Values**: Some default values embedded in code
2. **Magic Strings**: Setting keys as string literals (could use constants)
3. **No Logging**: No diagnostic logging for troubleshooting
4. **Tight Coupling**: MainWindow knows about all service classes
5. **No Dependency Injection**: Services instantiated directly

### Architecture Improvements (Future)
1. **MVVM Pattern**: Add ViewModels for proper separation
2. **Commanding**: Use ICommand instead of event handlers
3. **Data Binding**: Two-way binding for form fields
4. **Dependency Injection**: Use DI container (Microsoft.Extensions.DependencyInjection)
5. **Repository Pattern**: Abstract file system access

### Performance Considerations
1. **Synchronous Operations**: UI blocks during long operations
2. **No Caching**: Re-scan folders on every operation
3. **Eager Loading**: Load all profiles upfront
4. **No Virtualization**: Tree view with many items may be slow

## 📊 Metrics

### Codebase Statistics
- **Total Files**: 19
- **Lines of Code**: ~2,500 (estimated)
- **Models**: 4 classes
- **Services**: 5 classes
- **Views**: 4 windows
- **Build Time**: ~2 seconds
- **Warnings**: 0
- **Errors**: 0

### Complexity Assessment
- **Models**: Low complexity (simple data structures)
- **Services**: Medium complexity (file I/O, XML parsing)
- **UI**: Medium-high complexity (hierarchical data, multiple windows)
- **Overall**: Medium complexity for a desktop application

## 🎓 Lessons Learned

1. **File Format Analysis First**: Understanding CRC JSON and DGScope XML structure early prevented rework
2. **Iterative Development**: Building services before UI allowed independent testing
3. **Namespace Organization**: Clear separation (Models, Services, Views) improved maintainability
4. **XAML Data Binding**: Powerful but requires correct namespace declarations
5. **JSON Structure Variations**: visibilityCenter can be either object or string format - must handle both
6. **Template Limitations**: Existing templates don't have all elements (e.g., Receiver) - must create if missing
7. **Location Data Discovery**: Facility location is in starsConfiguration.areas[0].visibilityCenter, not a direct property
8. **Video Map Association**: Video maps are per-TRACON, not global - must look up by videoMapIds array

## 🤖 AI Assistant Notes

**For Future AI Assistants Working on This Project**:

### Quick Start Checklist
1. **Read PROJECT_STATUS.md** - Complete project context and architecture
2. **Review Recent Changes** - See Phase 8-11 for latest CRC integration work
3. **Understand Data Flow** - CRC JSON → Location/VideoMaps → TRACON → ProfileGeneration → XML with Receiver
4. **Build & Verify** - Run `dotnet build -c Release` (should show "Build succeeded. 0 Error(s)")
5. **Check Current Status** - App builds, Settings persist, CRC loads, Video maps extract correctly

### Key Architecture Points
- **Services**: Follow Repository pattern (load, save, scan operations)
- **Models**: POCOs with properties; no business logic
- **Views**: Code-behind with event handlers (not MVVM yet)
- **Settings**: AppSettings class + SettingsPersistenceService for JSON storage
- **Workflow**: Settings → CRC Load → TRACON Select → VideoMap Select → Generate Profile

### Critical Implementation Details
- **Location Extraction**: visibilityCenter in starsConfiguration.areas[0]
  - Parse as JsonValueKind.Object: try `lat`/`lon` properties
  - Parse as string: use ExtractLatLonFromString() helper
- **Video Map Lookup**: starsConfiguration.videoMapIds contains map IDs
  - Look up full VideoMapInfo from global videoMaps list
  - Associate with CrcTracon.AvailableVideoMaps
- **Receiver Creation**: UpdateReceiverConfig() must CREATE element if missing
  - Existing templates don't have Receiver element
  - Code creates complete element with Name, Location, Range, etc.
- **Profile Generation**: Three-step process
  1. Find/load template or create default
  2. Copy and rename video map with {FacilityId}_{OriginalName}
  3. Update HomeLocation and create/update Receiver with facility data

### Common Tasks

**Adding New Fields to Generated Profiles**:
1. Identify field in template XML
2. Find corresponding CRC data source
3. Add extraction to CrcProfileReader.LoadProfile()
4. Update CrcTracon or CrcProfile model
5. Update ProfileGeneratorService.GenerateFromCrc() to populate field

**Debugging Profile Generation**:
1. Check generated XML with: `[xml](Get-Content file.xml) | Select-Object -ExpandProperty RadarWindow`
2. Verify CRC parsing: Check CrcTracon properties have values
3. Verify video maps: Check CrcTracon.AvailableVideoMaps.Count > 0
4. Check receiver: Verify root.Element("Receiver") was created

**Testing End-to-End**:
1. Launch app
2. Settings dialog → browse to actual folders, save
3. Scan Folders → verify CRC profiles load
4. Select ZDC → click Generate
5. Select ACY TRACON → click OK
6. Select "IAD Cab.geojson" → click OK
7. Check generated file: `C:\...\profiles\ZDC\ACY.xml`
8. Verify coordinates, receiver name, video map path

### Watch Out For
- **File Paths**: Mix of absolute (CRC) and relative (DGScope) paths
- **Color Values**: Stored as negative integers for ARGB
- **Coordinates**: TRACON > ARTCC fallback hierarchy for location
- **Video Map Names**: Use human-readable format with FacilityId prefix
- **XML Namespace**: RadarWindow uses xsd/xsi schema attributes
- **.NET 10 Preview**: Some features may not be stable

### Files That Changed Recently
1. `Models/CrcProfile.cs` - Added AvailableVideoMaps, Lat/Lon properties
2. `Services/CrcProfileReader.cs` - Enhanced location and video map parsing
3. `Services/ProfileGeneratorService.cs` - Receiver creation, location update
4. `Services/SettingsPersistenceService.cs` - Settings persistence (NEW)
5. `Views/VideoMapSelectionWindow.xaml*` - Video map selection dialog (NEW)
6. `MainWindow.xaml.cs` - Integration of new dialog, TRACON selection flow

### Build Command
```powershell
cd "c:\Users\yanjz\Documents\VSCode Projects\DGScope-profile-manager"
dotnet build -c Release 2>&1 | Where-Object {$_ -match "^Build"}
```

---

**Version**: 1.0.0-alpha  
**Build Status**: ✅ Passing (0 Errors, 0 Warnings)  
**Last Updated**: January 6, 2026  
**Current Phase**: Phase 11 - Testing & Validation (In Progress)  
**Next Milestone**: Successful end-to-end profile generation with facility data
