# DGScope Profile Manager - Future Features

This document tracks planned enhancements and features for future releases.

## High Priority

### Default Settings Template Management

**Goal**: Allow users to define and save default non-facility-specific settings that apply to all newly generated profiles.

**Use Case**: User wants all profiles to have consistent display settings (colors, fonts, rotation) without configuring each one individually.

**Implementation**:
1. **Settings Manager UI**:
   - New menu item: `Settings → Default Template`
   - Dialog to configure:
     - Background colors
     - Font settings (name, size)
     - Screen rotation
     - Range ring settings
     - Data block colors
     - Other non-location-specific settings

2. **Template Storage**:
   - Save template to: `%APPDATA%\DGScopeProfileManager\default_template.xml`
   - Load during profile generation
   - Apply template settings before facility-specific updates

3. **Template Preview**:
   - Visual preview of template settings
   - Color picker for color values
   - Font selection dialog

**Affected Files**:
- New: `Views/TemplateManagerWindow.xaml`
- Update: `Services/ProfileGeneratorService.cs` (load user template instead of embedded)
- Update: `Models/AppSettings.cs` (add DefaultTemplatePath property)

---

### Profile Backfill from Existing Configurations

**Goal**: Allow users to update the default template by importing settings from an existing profile they've customized in DGScope.

**Use Case**: User spent time perfecting colors and layout in DGScope. They want those settings to apply to all future generated profiles.

**Implementation**:
1. **Backfill Workflow**:
   - Select existing DGScope profile
   - Click "Import as Template"
   - Analyze profile to extract non-facility settings
   - Save as new default template

2. **Setting Classification**:
   - **Facility-Specific** (don't import):
     - HomeLocation
     - ScreenCenterPoint
     - RangeRingLocation
     - AltimeterStations
     - NexradSensorId
     - VideoMapFilename
     - Receivers
   - **Global/Template Settings** (import):
     - Colors (background, video map, beacon, datablock)
     - Fonts (name, size)
     - Screen rotation
     - Display options
     - UI layout settings

3. **Merge Strategy**:
   - Allow selective import (checkboxes for setting categories)
   - Preview changes before applying
   - Keep version history of templates

**Affected Files**:
- New: `Services/TemplateExtractionService.cs`
- Update: `MainWindow.xaml` (add "Import as Template" button)
- Update: `Views/TemplateManagerWindow.xaml` (import dialog)

---

## Medium Priority

### Bulk Profile Generation

**Goal**: Generate profiles for all TRACONs in an ARTCC at once.

**Use Case**: User wants to create profiles for all facilities in ZNY (N90, HPN, etc.) in a single operation.

**Implementation**:
1. **Bulk Generation Dialog**:
   - Select ARTCC
   - Checkboxes for each available TRACON
   - Option: "Generate for all areas" vs. "First area only"
   - Default profile naming strategy

2. **Progress Tracking**:
   - Progress bar for generation status
   - Log window showing each profile created
   - Error handling (skip failed, continue with rest)

3. **Post-Generation Actions**:
   - Summary report (X profiles created, Y failed)
   - Open generated profile folder
   - Option to edit batch before saving

**Affected Files**:
- New: `Views/BulkGenerationWindow.xaml`
- New: `Services/BulkProfileGeneratorService.cs`
- Update: `MainWindow.xaml` (add "Bulk Generate" button)

---

### Profile Comparison and Merge Tool

**Goal**: Compare two DGScope profiles side-by-side and merge differences.

**Use Case**: User has two profiles for the same facility with different settings and wants to see differences or create a hybrid.

**Implementation**:
1. **Comparison View**:
   - Split view showing two profiles
   - Highlight differences
   - Color-coded: added (green), removed (red), modified (yellow)

2. **Merge Tool**:
   - Select which value to keep for each difference
   - Apply merge to create new profile
   - Save as new file or overwrite

3. **Diff Engine**:
   - Parse both XML files
   - Compare element by element
   - Handle missing elements gracefully

**Affected Files**:
- New: `Views/ProfileComparisonWindow.xaml`
- New: `Services/ProfileComparisonService.cs`
- New: `Models/ProfileDifference.cs`

---

## Low Priority / Nice-to-Have

### Video Map Preview

**Goal**: Display GeoJSON video maps visually in the application.

**Implementation**:
- Integrate GeoJSON rendering library
- Show map preview when selecting video map
- Display map bounds and center point
- Overlay facility location

**Challenges**:
- GeoJSON rendering complexity
- Performance with large maps
- Coordinate system handling

---

### Color Picker UI

**Goal**: Replace numeric color input with visual color picker.

**Implementation**:
- Use WPF ColorPicker control or third-party library
- Convert between ARGB integer and Color object
- Preview colors in real-time

---

### Import/Export Profiles

**Goal**: Backup and restore profile collections.

**Implementation**:
1. **Export**:
   - Select profiles to export
   - Create ZIP archive with XMLs and video maps
   - Include metadata (ARTCC, facility, date)

2. **Import**:
   - Select ZIP file
   - Preview contents
   - Import to specified location
   - Resolve conflicts (overwrite/skip/rename)

**Affected Files**:
- New: `Services/ProfileImportExportService.cs`
- Update: `MainWindow.xaml` (add Import/Export menu items)

---

### Search and Filter

**Goal**: Find profiles quickly by name, ARTCC, facility, or settings.

**Implementation**:
- Search bar in main window
- Filter by:
  - ARTCC code
  - Facility type (TRACON/RAPCON/etc.)
  - Has video map
  - Settings criteria (rotation, font, etc.)
- Save filter presets

---

### Undo/Redo System

**Goal**: Track changes and allow rollback.

**Implementation**:
- Command pattern for all modifications
- Maintain history stack
- Undo/Redo buttons in UI
- Clear history on application close

**Challenges**:
- Memory usage for large operations
- State management complexity

---

### Multi-Instance Support

**Goal**: Allow multiple users on the same machine to have separate settings.

**Implementation**:
- Store settings in user profile
- Support multiple DGScope installations
- Profile switcher UI

---

### Cloud Sync (Long-term)

**Goal**: Sync profiles across multiple computers.

**Implementation**:
- Optional cloud storage integration (OneDrive, Dropbox, custom)
- Conflict resolution
- Offline mode

---

## Technical Debt

### Error Handling Improvements

- Add comprehensive try-catch blocks
- User-friendly error messages
- Error logging to file
- Crash recovery

### Unit Test Coverage

- Create test project
- Test CRC parsing with various JSON structures
- Test profile generation logic
- Mock file I/O for testing

### Performance Optimization

- Lazy loading for large profile trees
- Asynchronous file operations
- Progress indicators for long operations
- Cancel long-running operations

### Code Refactoring

- Extract hard-coded strings to resources
- Consolidate duplicate XML manipulation code
- Simplify complex methods (ProfileGeneratorService)
- Improve naming consistency

---

## Ideas for Consideration

### Plugin System

Allow community-developed extensions for:
- Custom profile generators
- Additional data sources
- Export formats
- Video map processors

### Web Version

Browser-based version using Blazor WebAssembly:
- No installation required
- Cross-platform (Mac, Linux)
- Challenges: File system access, WPF features

### Profile Validation

Validate generated profiles:
- Check required elements present
- Verify coordinate ranges
- Validate ICAO codes
- Test video map file exists

### Statistics and Analytics

- Track profile usage
- Most common settings
- Facility coverage report
- Video map usage statistics

### Collaboration Features

- Share profiles with other users
- Profile repository/marketplace
- Rating and review system
- Version control integration

---

## Contributing

If you'd like to implement any of these features:

1. Create an issue on GitHub describing your approach
2. Reference the relevant TODO item
3. Follow the development guidelines in DEVELOPMENT.md
4. Submit a pull request with documentation

## Priority Legend

- **High Priority**: Core functionality, high user value
- **Medium Priority**: Valuable but not essential
- **Low Priority**: Nice-to-have, quality-of-life improvements
- **Ideas**: Exploratory, may not be feasible

---

**Last Updated**: January 6, 2026
