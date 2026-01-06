# Sample Files Analysis

Based on the samples you provided, here's what I've implemented:

## CRC Profile Format (ZDC.json)

**Structure:**
- JSON file containing an array of video maps
- Each video map has properties like:
  - `id`: Unique identifier
  - `name`: Display name
  - `sourceFileName`: The actual file name
  - `tags`: Array of categorization tags
  - `starsBrightnessCategory`: Display settings
  - `starsId`: Reference ID

**Implementation:**
- `CrcProfileReader.cs` now parses the JSON
- Extracts the ARTCC code from filename
- Builds a list of available video maps from `sourceFileName` fields

## DGScope Profile Format (PCT.xml)

**Key Elements:**
- `<BackColor>`, `<BeaconColor>`, etc. - Display colors
- `<VideoMapFilename>` - Path to the video map file
- `<HomeLocation>` - Latitude/Longitude center point
- `<ScreenRotation>` - Display rotation angle
- `<FontName>`, `<FontSize>` - Font settings
- `<AltimeterStations>` - List of station identifiers
- Many other radar display settings

**Implementation:**
- `DgScopeProfileService.cs` reads/writes XML profiles
- Extracts key settings into a dictionary
- Can update settings and save back to XML
- `FixFilePaths()` can convert paths between absolute/relative

## Available Features

1. **Read CRC Profiles**: Scan `%LocalAppData%\CRC\ARTCCs` for JSON files
2. **Read DGScope Profiles**: Scan a folder for XML profiles
3. **Generate New Profiles**: Create DGScope profiles from CRC data
4. **Copy Video Maps**: Move video maps from CRC to DGScope locations
5. **Fix File Paths**: Convert between absolute and relative paths
6. **Batch Edit Settings**: Change settings across multiple profiles

## Next Steps

To complete the application, you'll need to:

1. **Define the profile generation template** - What should the default settings be?
2. **Create the WPF UI** - Build the graphical interface
3. **Add validation logic** - Ensure profiles are valid
4. **Implement profile scanning** - Find all existing DGScope profiles
5. **Add logging** - Track operations and errors

Would you like me to create a simple WPF UI to demonstrate these features?
