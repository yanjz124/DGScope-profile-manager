using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Generates new DGScope profiles from CRC profiles
/// </summary>
public class ProfileGeneratorService
{
    private readonly NexradService _nexradService;
    private readonly VnasApiService _vnasApiService;

    public ProfileGeneratorService()
    {
        _nexradService = new NexradService();
        _vnasApiService = new VnasApiService();

        // Load NEXRAD stations from file
        var nexradPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nexrad-stations.txt");
        if (File.Exists(nexradPath))
        {
            _nexradService.LoadStations(nexradPath);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"NEXRAD stations file not found at: {nexradPath}");
        }
    }

    private XDocument LoadTemplateDocument(string outputDirectory, string? facilityId)
    {
        var templateProfile = FindSimilarTemplate(outputDirectory, facilityId);

        if (templateProfile != null)
        {
            // Load existing similar profile as template - preserve EVERYTHING
            // First read as text to fix old DBC tags before XML parsing
            var xmlText = File.ReadAllText(templateProfile);

            // Fix old DBC tags to DCB for backward compatibility
            xmlText = xmlText.Replace("<DBCFontName>", "<DCBFontName>");
            xmlText = xmlText.Replace("</DBCFontName>", "</DCBFontName>");
            xmlText = xmlText.Replace("<DBCFontSize>", "<DCBFontSize>");
            xmlText = xmlText.Replace("</DBCFontSize>", "</DCBFontSize>");

            // Now parse the fixed XML
            return XDocument.Parse(xmlText);
        }

        return LoadDefaultTemplate();
    }

    private List<VideoMapFile> CopyVideoMapFiles(
        IEnumerable<VideoMapInfo> videoMaps,
        CrcProfile crcProfile,
        CrcTracon? selectedTracon,
        string outputDirectory,
        string? crcVideoMapFolder,
        CrcArea? selectedArea)
    {
        var mapFiles = new List<VideoMapFile>();

        if (videoMaps == null)
        {
            return mapFiles;
        }

        var videoMapsDir = Path.Combine(outputDirectory, "VideoMaps");
        Directory.CreateDirectory(videoMapsDir);

        var prefix = selectedTracon?.Id ?? crcProfile.ArtccCode;

        foreach (var map in videoMaps)
        {
            var sourceFileName = Path.GetFileName(map.SourceFileName);
            var destFileName = !string.IsNullOrWhiteSpace(prefix)
                ? $"{prefix}_{sourceFileName}"
                : (sourceFileName ?? "map.geojson");
            var destFilePath = Path.Combine(videoMapsDir, destFileName);

            // Try to copy the video map file if CRC folder is configured
            if (!string.IsNullOrEmpty(crcVideoMapFolder))
            {
                string? sourceFilePath = null;

                // CRC stores video maps in: CRC\VideoMaps\{ARTCC}\{id}.geojson
                if (!string.IsNullOrEmpty(map.Id))
                {
                    sourceFilePath = Path.Combine(crcVideoMapFolder, crcProfile.ArtccCode, $"{map.Id}.geojson");
                }

                // Fallback: try the sourceFileName directly (in case structure is different)
                if (sourceFilePath == null || !File.Exists(sourceFilePath))
                {
                    sourceFilePath = Path.Combine(crcVideoMapFolder, map.SourceFileName);
                }

                try
                {
                    if (File.Exists(sourceFilePath))
                    {
                        File.Copy(sourceFilePath, destFilePath, overwrite: true);
                        System.Diagnostics.Debug.WriteLine($"✓ Copied video map: {sourceFilePath} -> {destFilePath}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✗ Source video map not found");
                        System.Diagnostics.Debug.WriteLine($"  Tried: {sourceFilePath}");
                        System.Diagnostics.Debug.WriteLine($"  Video Map ID: {map.Id}");
                        System.Diagnostics.Debug.WriteLine($"  Source File Name: {map.SourceFileName}");
                        System.Diagnostics.Debug.WriteLine($"  ARTCC: {crcProfile.ArtccCode}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Error copying video map: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("✗ CRC VideoMap folder not configured");
            }

            // Filter button assignments based on selected area
            // A map can have multiple button assignments (appear at multiple positions)
            var relevantAssignments = map.ButtonAssignments;

            if (selectedArea != null && !string.IsNullOrWhiteSpace(selectedArea.MapGroupId))
            {
                // Only include assignments from the selected area
                relevantAssignments = map.ButtonAssignments
                    .Where(a => a.MapGroupId == selectedArea.MapGroupId)
                    .ToList();
            }

            // Create a VideoMapFile entry for each button assignment
            // If a map appears at multiple button positions, we add it multiple times
            if (relevantAssignments.Count > 0)
            {
                foreach (var assignment in relevantAssignments)
                {
                    mapFiles.Add(new VideoMapFile
                    {
                        FileName = destFilePath,
                        Name = string.IsNullOrWhiteSpace(map.Name) ? null : map.Name,
                        ShortName = string.IsNullOrWhiteSpace(map.ShortName) ? null : map.ShortName,
                        StarsBrightnessCategory = map.StarsBrightnessCategory,
                        StarsId = map.StarsId,
                        DcbButton = assignment.DcbButton,
                        DcbButtonPosition = assignment.DcbButtonPosition
                    });
                }
            }
            else
            {
                // No button assignments - include the map without DCB button info
                mapFiles.Add(new VideoMapFile
                {
                    FileName = destFilePath,
                    Name = string.IsNullOrWhiteSpace(map.Name) ? null : map.Name,
                    ShortName = string.IsNullOrWhiteSpace(map.ShortName) ? null : map.ShortName,
                    StarsBrightnessCategory = map.StarsBrightnessCategory,
                    StarsId = map.StarsId,
                    DcbButton = null,
                    DcbButtonPosition = null
                });
            }
        }

        return mapFiles;
    }

    private void ApplyVideoMapFiles(XElement root, List<VideoMapFile> videoMapFiles)
    {
        var existingVideoMaps = root.Element("VideoMapFiles");
        existingVideoMaps?.Remove();

        // Remove legacy VideoMapFilename if it exists
        var legacyVideoMap = root.Element("VideoMapFilename");
        legacyVideoMap?.Remove();

        if (videoMapFiles == null || videoMapFiles.Count == 0)
        {
            return;
        }

        var listElement = new XElement("VideoMapFiles");

        // Build map number to button positions mapping for DCBMapList generation
        // A map can appear at multiple button positions (e.g., map #5 at positions 0, 5, 10)
        var mapNumberToButtonPositions = new Dictionary<int, List<int>>();

        // Group by unique map and collect all DCB button numbers
        var uniqueMaps = new Dictionary<string, VideoMapFile>();
        var mapToButtonNumbers = new Dictionary<string, List<string>>(); // Track all button numbers per map
        var usedNumbers = new HashSet<int>();
        var nextNumber = 1;

        foreach (var map in videoMapFiles)
        {
            // MapNumber corresponds to StarsId when provided; otherwise sequential
            int mapNumber;
            if (!string.IsNullOrWhiteSpace(map.StarsId) && int.TryParse(map.StarsId, out var parsed))
            {
                mapNumber = parsed;
            }
            else
            {
                mapNumber = nextNumber;
            }

            // Generate unique key for this map
            var mapKey = $"{map.FileName}_{mapNumber}";

            if (!uniqueMaps.ContainsKey(mapKey))
            {
                // First time seeing this map - add it
                uniqueMaps[mapKey] = map;
                mapToButtonNumbers[mapKey] = new List<string>();
                usedNumbers.Add(mapNumber);
                nextNumber = Math.Max(nextNumber, mapNumber + 1);
            }

            // Collect DCB button numbers for this map (for comma-separated DCBButton element)
            if (!string.IsNullOrWhiteSpace(map.DcbButton))
            {
                mapToButtonNumbers[mapKey].Add(map.DcbButton);
            }

            // Track button position for DCBMapList generation
            // A map can have multiple positions
            if (map.DcbButtonPosition.HasValue)
            {
                if (!mapNumberToButtonPositions.ContainsKey(mapNumber))
                {
                    mapNumberToButtonPositions[mapNumber] = new List<int>();
                }
                mapNumberToButtonPositions[mapNumber].Add(map.DcbButtonPosition.Value);
            }
        }

        // Now emit unique VideoMapFile entries (one per unique map)
        foreach (var kvp in uniqueMaps)
        {
            var mapKey = kvp.Key;
            var map = kvp.Value;

            // Recalculate map number
            int mapNumber;
            if (!string.IsNullOrWhiteSpace(map.StarsId) && int.TryParse(map.StarsId, out var parsed))
            {
                mapNumber = parsed;
            }
            else
            {
                mapNumber = 1; // Fallback
            }

            var mapElement = new XElement("VideoMapFile",
                new XElement("Filepath", map.FileName),
                new XElement("MapNumber", mapNumber));

            if (!string.IsNullOrWhiteSpace(map.ShortName))
                mapElement.Add(new XElement("ShortName", map.ShortName));

            if (!string.IsNullOrWhiteSpace(map.Name))
                mapElement.Add(new XElement("FullName", map.Name));

            if (!string.IsNullOrWhiteSpace(map.StarsBrightnessCategory))
                mapElement.Add(new XElement("BrightnessGroup", map.StarsBrightnessCategory));

            // Include comma-separated DCB button numbers (e.g., "3,11" for buttons 3 and 11)
            // This is the recommended format for maps at multiple button positions
            if (mapToButtonNumbers.TryGetValue(mapKey, out var buttonNumbers) && buttonNumbers.Count > 0)
            {
                var dcbButtonValue = string.Join(",", buttonNumbers.OrderBy(b => int.Parse(b)));
                mapElement.Add(new XElement("DCBButton", dcbButtonValue));
            }

            listElement.Add(mapElement);
        }

        root.Add(listElement);

        // Generate DCBMapList for TCP section
        GenerateDCBMapList(root, mapNumberToButtonPositions);
    }

    /// <summary>
    /// Generates the DCBMapList for the TCP section based on button position mappings
    /// A map can appear at multiple button positions
    /// </summary>
    private void GenerateDCBMapList(XElement root, Dictionary<int, List<int>> mapNumberToButtonPositions)
    {
        // Create the DCBMapList array (36 positions, all zeros by default)
        var dcbMapList = new int[36];

        // Populate the array: dcbMapList[buttonPosition] = mapNumber
        // A map can appear at multiple positions
        foreach (var kvp in mapNumberToButtonPositions)
        {
            var mapNumber = kvp.Key;
            var buttonPositions = kvp.Value;

            foreach (var buttonPosition in buttonPositions)
            {
                if (buttonPosition >= 0 && buttonPosition < 36)
                {
                    dcbMapList[buttonPosition] = mapNumber;
                }
            }
        }

        // Find or create TCP element
        var tcp = root.Element("TCP");
        if (tcp == null)
        {
            tcp = new XElement("TCP");
            root.Add(tcp);
        }

        // Remove existing DCBMapList if present
        var existingDcbMapList = tcp.Element("DCBMapList");
        existingDcbMapList?.Remove();

        // Create new DCBMapList element
        var dcbMapListElement = new XElement("DCBMapList");
        foreach (var mapNumber in dcbMapList)
        {
            dcbMapListElement.Add(new XElement("int", mapNumber));
        }

        tcp.Add(dcbMapListElement);
    }

    /// <summary>
    /// Generates a DGScope profile from a CRC profile with the selected video map
    /// </summary>
    public DgScopeProfile? GenerateFromCrc(
        CrcProfile crcProfile,
        string outputDirectory,
        CrcTracon? selectedTracon = null,
        VideoMapInfo? selectedVideoMap = null,
        string? crcVideoMapFolder = null,
        CrcArea? selectedArea = null,
        string? customProfileName = null,
        ProfileDefaultSettings? defaultSettings = null)
    {
        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputDirectory);

        // Set profile name based on custom name or selected TRACON
        var profileCode = selectedTracon?.Id ?? crcProfile.ArtccCode;
        var profileName = selectedTracon?.Name ?? crcProfile.ArtccCode;

        // Use custom profile name if provided (e.g., "ACY_main.xml")
        string fileName;
        if (!string.IsNullOrWhiteSpace(customProfileName))
        {
            fileName = $"{profileCode}_{customProfileName}.xml";
        }
        else
        {
            fileName = $"{profileCode}.xml";
        }

        var outputPath = Path.Combine(outputDirectory, fileName);

        // Load template (existing profile if available, otherwise embedded default)
        var doc = LoadTemplateDocument(outputDirectory, selectedTracon?.Id);
        var root = doc.Root;
        if (root == null)
        {
            throw new InvalidOperationException("Failed to create profile XML");
        }

        // Copy and register selected video map (if any) into the new multi-map structure
        var selectedMaps = selectedVideoMap != null
            ? new List<VideoMapInfo> { selectedVideoMap }
            : Enumerable.Empty<VideoMapInfo>();

        var videoMapFiles = CopyVideoMapFiles(selectedMaps, crcProfile, selectedTracon, outputDirectory, crcVideoMapFolder, selectedArea);
        ApplyVideoMapFiles(root, videoMapFiles);

        // 2. Update home location
        // Priority: selectedArea > selectedTracon > crcProfile
        double? latitude = selectedArea?.Latitude ?? selectedTracon?.Latitude ?? crcProfile.HomeLatitude;
        double? longitude = selectedArea?.Longitude ?? selectedTracon?.Longitude ?? crcProfile.HomeLongitude;

        if (latitude.HasValue && longitude.HasValue)
        {
            UpdateHomeLocation(root, latitude.Value, longitude.Value);
            UpdateScreenCenterPoint(root, latitude.Value, longitude.Value);
            UpdateRangeRingLocation(root, latitude.Value, longitude.Value);
        }

        // 3. Update altimeter stations
        // Priority: selectedArea > selectedTracon (if selectedArea is provided, use its airports)
        List<string>? ssaAirports = null;
        if (selectedArea != null && selectedArea.SsaAirports.Count > 0)
        {
            ssaAirports = selectedArea.SsaAirports;
            System.Diagnostics.Debug.WriteLine($"Using ssaAirports from selected area '{selectedArea.Name}': {string.Join(", ", ssaAirports)}");
        }
        else if (selectedTracon != null && selectedTracon.SsaAirports.Count > 0)
        {
            ssaAirports = selectedTracon.SsaAirports;
            System.Diagnostics.Debug.WriteLine($"Using aggregate ssaAirports from all areas in {selectedTracon.Id}: {string.Join(", ", ssaAirports)}");
        }

        if (ssaAirports != null && ssaAirports.Count > 0)
        {
            // Use AirportLookupService for accurate FAA LID to ICAO conversion
            var lookupService = AirportLookupService.Instance;
            var artccCode = crcProfile.ArtccCode;

            var altimeterStations = ssaAirports.Select(airport =>
            {
                return lookupService.ConvertToIcao(airport, artccCode);
            }).ToList();

            UpdateAltimeterStations(root, altimeterStations);
            System.Diagnostics.Debug.WriteLine($"✓ Added {altimeterStations.Count} altimeter stations: {string.Join(", ", altimeterStations)}");
        }

        // 4. Update receiver configuration
        if (selectedTracon != null && latitude.HasValue && longitude.HasValue)
        {
            UpdateReceiverConfig(root, selectedTracon.Id, latitude.Value, longitude.Value);
        }

        // 5. Update NEXRAD configuration (automatic selection based on proximity)
        if (latitude.HasValue && longitude.HasValue)
        {
            var nexradStation = _nexradService.FindClosestStation(latitude.Value, longitude.Value);
            if (nexradStation != null)
            {
                UpdateNexradConfig(root, nexradStation.Icao, 300);
                System.Diagnostics.Debug.WriteLine($"✓ Selected NEXRAD station: {nexradStation.Icao} ({nexradStation.Name}) - {nexradStation.DistanceToNauticalMiles(latitude.Value, longitude.Value):F1} NM away");
            }
        }

        // 6. Apply default settings (if provided)
        if (defaultSettings != null)
        {
            ApplyDefaultSettings(root, defaultSettings);
        }

        // Save the generated profile
        try
        {
            doc.Save(outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving profile to {outputPath}: {ex.Message}");
            throw;
        }

        // Return the generated profile
        var dgScopeProfile = new DgScopeProfile
        {
            Name = profileName,
            FilePath = outputPath,
            VideoMapFiles = videoMapFiles,
            VideoMapPaths = videoMapFiles.Select(v => v.FileName).ToList()
        };

        return dgScopeProfile;
    }

    /// <summary>
    /// Generates a DGScope profile from a CRC profile with multiple selected video maps (merged into one GeoJSON)
    /// </summary>
    public DgScopeProfile? GenerateFromCrcWithMultipleMaps(
        CrcProfile crcProfile,
        string outputDirectory,
        List<VideoMapInfo> selectedVideoMaps,
        string? crcVideoMapFolder = null,
        CrcTracon? selectedTracon = null,
        CrcArea? selectedArea = null,
        string? customProfileName = null,
        ProfileDefaultSettings? defaultSettings = null,
        CrcPrefSet? crcPrefSet = null,
        string? facilityIdOverride = null,
        bool importAtpaVolumes = true)
    {
        if (selectedVideoMaps == null || selectedVideoMaps.Count == 0)
        {
            return GenerateFromCrc(crcProfile, outputDirectory, selectedTracon, null, crcVideoMapFolder, selectedArea, customProfileName, defaultSettings);
        }

        // If only one map, use the regular method
        if (selectedVideoMaps.Count == 1)
        {
            return GenerateFromCrc(crcProfile, outputDirectory, selectedTracon, selectedVideoMaps[0], crcVideoMapFolder, selectedArea, customProfileName, defaultSettings);
        }

        // Multiple maps: copy individually and emit VideoMapFiles list instead of merging
        try
        {
            // Create output directory if it doesn't exist
            Directory.CreateDirectory(outputDirectory);

            var profileCode = selectedTracon?.Id ?? crcProfile.ArtccCode;
            var profileName = selectedTracon?.Name ?? crcProfile.ArtccCode;

            string fileName;
            if (!string.IsNullOrWhiteSpace(customProfileName))
            {
                fileName = $"{profileCode}_{customProfileName}.xml";
            }
            else
            {
                fileName = $"{profileCode}.xml";
            }

            var outputPath = Path.Combine(outputDirectory, fileName);

            // Load template
            var doc = LoadTemplateDocument(outputDirectory, selectedTracon?.Id);
            var root = doc.Root;
            if (root == null)
            {
                throw new InvalidOperationException("Failed to create profile XML");
            }

            var videoMapFiles = CopyVideoMapFiles(selectedVideoMaps, crcProfile, selectedTracon, outputDirectory, crcVideoMapFolder, selectedArea);

            if (videoMapFiles.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("✗ No video map files copied; falling back to single-map generation");
                return GenerateFromCrc(crcProfile, outputDirectory, selectedTracon, null, crcVideoMapFolder, selectedArea, customProfileName, defaultSettings);
            }

            ApplyVideoMapFiles(root, videoMapFiles);

            // 2. Update home location
            double? latitude = selectedArea?.Latitude ?? selectedTracon?.Latitude ?? crcProfile.HomeLatitude;
            double? longitude = selectedArea?.Longitude ?? selectedTracon?.Longitude ?? crcProfile.HomeLongitude;

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateHomeLocation(root, latitude.Value, longitude.Value);
                UpdateScreenCenterPoint(root, latitude.Value, longitude.Value);
                UpdateRangeRingLocation(root, latitude.Value, longitude.Value);
            }

            // 3. Update altimeter stations
            List<string>? ssaAirports = null;
            if (selectedArea != null && selectedArea.SsaAirports.Count > 0)
            {
                ssaAirports = selectedArea.SsaAirports;
                System.Diagnostics.Debug.WriteLine($"Using ssaAirports from selected area '{selectedArea.Name}': {string.Join(", ", ssaAirports)}");
            }
            else if (selectedTracon != null && selectedTracon.SsaAirports.Count > 0)
            {
                ssaAirports = selectedTracon.SsaAirports;
                System.Diagnostics.Debug.WriteLine($"Using aggregate ssaAirports from all areas in {selectedTracon.Id}: {string.Join(", ", ssaAirports)}");
            }

            if (ssaAirports != null && ssaAirports.Count > 0)
            {
                var lookupService = AirportLookupService.Instance;
                var artccCode = crcProfile.ArtccCode;

                var altimeterStations = ssaAirports.Select(airport =>
                {
                    return lookupService.ConvertToIcao(airport, artccCode);
                }).ToList();

                UpdateAltimeterStations(root, altimeterStations);
                System.Diagnostics.Debug.WriteLine($"✓ Added {altimeterStations.Count} altimeter stations: {string.Join(", ", altimeterStations)}");
            }

            // 4. Update receiver configuration
            var receiverFacilityId = facilityIdOverride ?? selectedTracon?.Id;
            if (!string.IsNullOrWhiteSpace(receiverFacilityId) && latitude.HasValue && longitude.HasValue)
            {
                UpdateReceiverConfig(root, receiverFacilityId, latitude.Value, longitude.Value);
            }

            // 5. Update NEXRAD configuration (automatic selection based on proximity)
            if (latitude.HasValue && longitude.HasValue)
            {
                var nexradStation = _nexradService.FindClosestStation(latitude.Value, longitude.Value);
                if (nexradStation != null)
                {
                    UpdateNexradConfig(root, nexradStation.Icao, 300);
                    System.Diagnostics.Debug.WriteLine($"✓ Selected NEXRAD station: {nexradStation.Icao} ({nexradStation.Name}) - {nexradStation.DistanceToNauticalMiles(latitude.Value, longitude.Value):F1} NM away");
                }
            }

            // 6. Apply default settings (if provided)
            if (defaultSettings != null)
            {
                ApplyDefaultSettings(root, defaultSettings);
            }

            // 7. Apply CRC PrefSet settings (if provided) - overrides default settings
            if (crcPrefSet != null)
            {
                ApplyCrcPrefSetSettings(root, crcPrefSet);
                System.Diagnostics.Debug.WriteLine($"Applied CRC PrefSet: {crcPrefSet.Name}");
            }

            // 8. Import ATPA volumes from VNAS API (if enabled), filtered by TRACON
            if (importAtpaVolumes)
            {
                try
                {
                    var volumesByFacility = _vnasApiService.FetchAtpaVolumesByFacilityAsync(crcProfile.ArtccCode).GetAwaiter().GetResult();
                    var facilityId = selectedTracon?.Id ?? "";

                    // Use facility-specific volumes if available, otherwise fall back to all
                    List<VnasAtpaVolume> atpaVolumes;
                    if (!string.IsNullOrEmpty(facilityId) && volumesByFacility.TryGetValue(facilityId, out var facilityVolumes))
                    {
                        atpaVolumes = facilityVolumes;
                        System.Diagnostics.Debug.WriteLine($"Using {atpaVolumes.Count} ATPA volumes for facility {facilityId}");
                    }
                    else
                    {
                        atpaVolumes = volumesByFacility.Values.SelectMany(v => v).ToList();
                        System.Diagnostics.Debug.WriteLine($"No facility match for '{facilityId}', using all {atpaVolumes.Count} volumes");
                    }

                    if (atpaVolumes.Count > 0)
                    {
                        ApplyAtpaVolumes(root, atpaVolumes);
                        System.Diagnostics.Debug.WriteLine($"Imported {atpaVolumes.Count} ATPA volumes from VNAS for {crcProfile.ArtccCode}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"No ATPA volumes found in VNAS for {crcProfile.ArtccCode}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ATPA volume import failed (non-fatal): {ex.Message}");
                }
            }

            // Save the generated profile
            try
            {
                doc.Save(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving profile to {outputPath}: {ex.Message}");
                throw;
            }

            // Return the generated profile
            var dgScopeProfile = new DgScopeProfile
            {
                Name = profileName,
                FilePath = outputPath,
                VideoMapFiles = videoMapFiles,
                VideoMapPaths = videoMapFiles.Select(v => v.FileName).ToList()
            };

            return dgScopeProfile;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error in GenerateFromCrcWithMultipleMaps: {ex.Message}");
            return GenerateFromCrc(crcProfile, outputDirectory, selectedTracon, null, crcVideoMapFolder, selectedArea, customProfileName, defaultSettings);
        }
    }

    /// <summary>
    /// Generate profile with a pre-merged GeoJSON file (already at the destination path)
    /// </summary>
    private DgScopeProfile? GenerateFromCrcWithMergedMap(
        CrcProfile crcProfile,
        string outputDirectory,
        CrcTracon? selectedTracon = null,
        string? mergedMapPath = null,
        CrcArea? selectedArea = null,
        string? customProfileName = null,
        ProfileDefaultSettings? defaultSettings = null)
    {
        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputDirectory);

        var profileCode = selectedTracon?.Id ?? crcProfile.ArtccCode;
        var profileName = selectedTracon?.Name ?? crcProfile.ArtccCode;

        string fileName;
        if (!string.IsNullOrWhiteSpace(customProfileName))
        {
            fileName = $"{profileCode}_{customProfileName}.xml";
        }
        else
        {
            fileName = $"{profileCode}.xml";
        }

        var outputPath = Path.Combine(outputDirectory, fileName);

        // Load template
        var doc = LoadDefaultTemplate();
        var root = doc.Root;
        if (root == null)
        {
            throw new InvalidOperationException("Failed to create profile XML");
        }

        // 1. Update video map filename (using merged map)
        if (!string.IsNullOrEmpty(mergedMapPath))
        {
            ApplyVideoMapFiles(root, new List<VideoMapFile>
            {
                new VideoMapFile { FileName = mergedMapPath }
            });
        }

        // 2. Update home location
        double? latitude = selectedArea?.Latitude ?? selectedTracon?.Latitude ?? crcProfile.HomeLatitude;
        double? longitude = selectedArea?.Longitude ?? selectedTracon?.Longitude ?? crcProfile.HomeLongitude;

        if (latitude.HasValue && longitude.HasValue)
        {
            UpdateHomeLocation(root, latitude.Value, longitude.Value);
            UpdateScreenCenterPoint(root, latitude.Value, longitude.Value);
            UpdateRangeRingLocation(root, latitude.Value, longitude.Value);
        }

        // 3. Update altimeter stations
        List<string>? ssaAirports = null;
        if (selectedArea != null && selectedArea.SsaAirports.Count > 0)
        {
            ssaAirports = selectedArea.SsaAirports;
        }
        else if (selectedTracon != null && selectedTracon.SsaAirports.Count > 0)
        {
            ssaAirports = selectedTracon.SsaAirports;
        }

        if (ssaAirports != null && ssaAirports.Count > 0)
        {
            var lookupService = AirportLookupService.Instance;
            var artccCode = crcProfile.ArtccCode;
            var altimeterStations = ssaAirports.Select(airport => lookupService.ConvertToIcao(airport, artccCode)).ToList();
            UpdateAltimeterStations(root, altimeterStations);
        }

        // 4. Update receiver configuration
        if (selectedTracon != null && latitude.HasValue && longitude.HasValue)
        {
            UpdateReceiverConfig(root, selectedTracon.Id, latitude.Value, longitude.Value);
        }

        // 5. Update NEXRAD configuration
        if (latitude.HasValue && longitude.HasValue)
        {
            var nexradStation = _nexradService.FindClosestStation(latitude.Value, longitude.Value);
            if (nexradStation != null)
            {
                UpdateNexradConfig(root, nexradStation.Icao, 300);
            }
        }

        // 6. Apply default settings
        if (defaultSettings != null)
        {
            ApplyDefaultSettings(root, defaultSettings);
        }

        // Save the profile
        try
        {
            doc.Save(outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving profile to {outputPath}: {ex.Message}");
            throw;
        }

        return new DgScopeProfile
        {
            Name = profileName,
            FilePath = outputPath
        };
    }

    /// <summary>
    /// Load the embedded default template
    /// </summary>
    private XDocument LoadDefaultTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "DGScopeProfileManager.Resources.DefaultTemplate.xml";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        return XDocument.Load(stream);
    }

    /// <summary>
    /// Find a similar profile in the output directory to use as template
    /// </summary>
    private string? FindSimilarTemplate(string outputDirectory, string? facilityId)
    {
        if (!Directory.Exists(outputDirectory))
            return null;

        // Look for existing profiles
        var profiles = Directory.GetFiles(outputDirectory, "*.xml");
        if (profiles.Length > 0)
        {
            return profiles[0]; // Use first available profile as template
        }

        return null;
    }

    /// <summary>
    /// Helper method to set or create an XML element
    /// </summary>
    private void SetOrCreateElement(XElement parent, string elementName, string value)
    {
        var element = parent.Element(elementName);
        if (element != null)
        {
            element.Value = value;
        }
        else
        {
            parent.Add(new XElement(elementName, value));
        }
    }

    /// <summary>
    /// Update home location coordinates
    /// </summary>
    private void UpdateHomeLocation(XElement root, double latitude, double longitude)
    {
        var homeLocation = root.Element("HomeLocation");
        if (homeLocation != null)
        {
            SetOrCreateElement(homeLocation, "Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture));
            SetOrCreateElement(homeLocation, "Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture));
        }
        else
        {
            root.Add(new XElement("HomeLocation",
                new XElement("Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture)),
                new XElement("Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture))));
        }
    }

    /// <summary>
    /// Update screen center point coordinates
    /// </summary>
    private void UpdateScreenCenterPoint(XElement root, double latitude, double longitude)
    {
        var currentPrefSet = root.Element("CurrentPrefSet");
        if (currentPrefSet == null)
        {
            currentPrefSet = new XElement("CurrentPrefSet");
            root.Add(currentPrefSet);
        }

        var screenCenterPoint = currentPrefSet.Element("ScreenCenterPoint");
        if (screenCenterPoint != null)
        {
            SetOrCreateElement(screenCenterPoint, "Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture));
            SetOrCreateElement(screenCenterPoint, "Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture));
        }
        else
        {
            currentPrefSet.Add(new XElement("ScreenCenterPoint",
                new XElement("Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture)),
                new XElement("Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture))));
        }
    }

    /// <summary>
    /// Update range ring location coordinates
    /// </summary>
    private void UpdateRangeRingLocation(XElement root, double latitude, double longitude)
    {
        var currentPrefSet = root.Element("CurrentPrefSet");
        if (currentPrefSet == null)
        {
            currentPrefSet = new XElement("CurrentPrefSet");
            root.Add(currentPrefSet);
        }

        var rangeRingLocation = currentPrefSet.Element("RangeRingLocation");
        if (rangeRingLocation != null)
        {
            SetOrCreateElement(rangeRingLocation, "Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture));
            SetOrCreateElement(rangeRingLocation, "Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture));
        }
        else
        {
            currentPrefSet.Add(new XElement("RangeRingLocation",
                new XElement("Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture)),
                new XElement("Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture))));
        }
    }

    /// <summary>
    /// Update altimeter stations list
    /// </summary>
    private void UpdateAltimeterStations(XElement root, List<string> stations)
    {
        var altimeterElement = root.Element("AltimeterStations");

        if (altimeterElement != null)
        {
            // Clear existing stations
            altimeterElement.RemoveAll();
        }
        else
        {
            // Create element if it doesn't exist
            altimeterElement = new XElement("AltimeterStations");
            root.Add(altimeterElement);
        }

        // Add each station as a <string> child element
        foreach (var station in stations)
        {
            altimeterElement.Add(new XElement("string", station));
        }
    }

    /// <summary>
    /// Update the ScopeServer receiver configuration with facility info
    /// Updates the Receivers element (note: plural, contains Receiver children)
    /// </summary>
    private void UpdateReceiverConfig(XElement root, string facilityId, double latitude, double longitude)
    {
        // Construct dSTARS URL from the active data source: {base}/dstars/{FACILITY_ID}/updates
        var dstarsUrl = DataSourceService.BuildUpdatesUrl(facilityId);

        var receivers = root.Element("Receivers");

        // Create Receivers element if it doesn't exist
        if (receivers == null)
        {
            receivers = new XElement("Receivers");
            root.Add(receivers);
        }

        // Look for existing Receiver element
        var receiver = receivers.Elements("Receiver").FirstOrDefault();

        if (receiver == null)
        {
            // Create new Receiver with ScopeServerClient
            receiver = new XElement("Receiver",
                new XAttribute("AssemblyQualifiedName", "DGScope.Receivers.ScopeServer.ScopeServerClient, DGScope.Receivers.ScopeServer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"),
                new XElement("ScopeServerClient",
                    new XElement("Name", facilityId),
                    new XElement("Enabled", "true"),
                    new XElement("Location",
                        new XElement("Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture)),
                        new XElement("Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture))),
                    new XElement("Range", "250"),
                    new XElement("CreateNewAircraft", "true"),
                    new XElement("Url", dstarsUrl)));
            receivers.Add(receiver);
            return;
        }

        // Update existing Receiver
        var scopeServerClient = receiver.Element("ScopeServerClient");
        if (scopeServerClient == null)
        {
            // Create ScopeServerClient if it doesn't exist
            scopeServerClient = new XElement("ScopeServerClient",
                new XElement("Name", facilityId),
                new XElement("Enabled", "true"),
                new XElement("Location",
                    new XElement("Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture)),
                    new XElement("Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture))),
                new XElement("Range", "250"),
                new XElement("CreateNewAircraft", "true"),
                new XElement("Url", dstarsUrl));
            receiver.Add(scopeServerClient);
            return;
        }

        // Update existing ScopeServerClient fields
        SetOrCreateElement(scopeServerClient, "Name", facilityId);
        SetOrCreateElement(scopeServerClient, "Url", dstarsUrl);

        // Update location
        var locationElement = scopeServerClient.Element("Location");
        if (locationElement != null)
        {
            SetOrCreateElement(locationElement, "Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture));
            SetOrCreateElement(locationElement, "Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture));
        }
        else
        {
            scopeServerClient.Add(new XElement("Location",
                new XElement("Latitude", latitude.ToString("F7", CultureInfo.InvariantCulture)),
                new XElement("Longitude", longitude.ToString("F7", CultureInfo.InvariantCulture))));
        }
    }

    /// <summary>
    /// Update NEXRAD weather radar configuration
    /// </summary>
    private void UpdateNexradConfig(XElement root, string sensorId, int downloadInterval)
    {
        // Construct NEXRAD URL
        var nexradUrl = $"https://tgftp.nws.noaa.gov/SL.us008001/DF.of/DC.radar/DS.p94r0/SI.{sensorId.ToLower()}/sn.last";

        var nexrad = root.Element("Nexrad");

        if (nexrad != null)
        {
            // Update existing NEXRAD element
            SetOrCreateElement(nexrad, "URL", nexradUrl);
            SetOrCreateElement(nexrad, "DownloadInterval", downloadInterval.ToString());
            SetOrCreateElement(nexrad, "SensorID", sensorId.ToUpper());
        }
        else
        {
            // Create new NEXRAD element (minimal - user can configure colors in DGScope)
            nexrad = new XElement("Nexrad",
                new XElement("WxRadarMode", "NWSNexrad"),
                new XElement("Enabled", "true"),
                new XElement("URL", nexradUrl),
                new XElement("DownloadInterval", downloadInterval.ToString()),
                new XElement("SensorID", sensorId.ToUpper()));
            root.Add(nexrad);
        }
    }

    /// <summary>
    /// Apply default settings from ProfileDefaultSettings to the generated profile XML
    /// </summary>
    private void ApplyDefaultSettings(XElement root, ProfileDefaultSettings defaults)
    {
        try
        {
            // Apply font settings
            if (!string.IsNullOrWhiteSpace(defaults.FontName))
            {
                SetOrCreateElement(root, "FontName", defaults.FontName);
            }

            if (!string.IsNullOrWhiteSpace(defaults.FontSize))
            {
                SetOrCreateElement(root, "FontSize", defaults.FontSize);
            }

            // Apply window position from PrefSetSettings
            if (defaults.PrefSet != null)
            {
                var windowSizeElem = root.Element("WindowSize");
                if (windowSizeElem != null)
                {
                    SetOrCreateElement(windowSizeElem, "Width", defaults.PrefSet.WindowSizeWidth.ToString());
                    SetOrCreateElement(windowSizeElem, "Height", defaults.PrefSet.WindowSizeHeight.ToString());
                }

                var windowLocElem = root.Element("WindowLocation");
                if (windowLocElem != null)
                {
                    SetOrCreateElement(windowLocElem, "X", defaults.PrefSet.WindowLocationX.ToString());
                    SetOrCreateElement(windowLocElem, "Y", defaults.PrefSet.WindowLocationY.ToString());
                }
            }

            System.Diagnostics.Debug.WriteLine("✓ Applied default settings to generated profile");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error applying default settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply CRC PrefSet settings to the generated profile XML
    /// This includes brightness, range, leader direction, altitude filters, etc.
    /// </summary>
    private void ApplyCrcPrefSetSettings(XElement root, CrcPrefSet prefSet)
    {
        try
        {
            // Get or create CurrentPrefSet element
            var currentPrefSet = root.Element("CurrentPrefSet");
            if (currentPrefSet == null)
            {
                currentPrefSet = new XElement("CurrentPrefSet");
                root.Add(currentPrefSet);
            }

            // Apply Range
            SetOrCreateElement(currentPrefSet, "Range", ((int)Math.Round(prefSet.Range)).ToString());

            // Apply Display Center (ScreenCenterPoint)
            if (prefSet.DisplayCenter != null)
            {
                var screenCenterPoint = currentPrefSet.Element("ScreenCenterPoint");
                if (screenCenterPoint == null)
                {
                    screenCenterPoint = new XElement("ScreenCenterPoint");
                    currentPrefSet.Add(screenCenterPoint);
                }
                SetOrCreateElement(screenCenterPoint, "Latitude", prefSet.DisplayCenter.Lat.ToString("F7", CultureInfo.InvariantCulture));
                SetOrCreateElement(screenCenterPoint, "Longitude", prefSet.DisplayCenter.Lon.ToString("F7", CultureInfo.InvariantCulture));
            }

            // Apply ScopeCentered (inverse of DisplayOffCenter)
            SetOrCreateElement(currentPrefSet, "ScopeCentered", (!prefSet.DisplayOffCenter).ToString().ToLower());

            // Apply Range Ring settings
            SetOrCreateElement(currentPrefSet, "RangeRingSpacing", prefSet.RangeRingSpacing.ToString());
            SetOrCreateElement(currentPrefSet, "RangeRingsCentered", (!prefSet.RangeRingsOffCenter).ToString().ToLower());

            if (prefSet.RangeRingCenter != null)
            {
                var rangeRingLocation = currentPrefSet.Element("RangeRingLocation");
                if (rangeRingLocation == null)
                {
                    rangeRingLocation = new XElement("RangeRingLocation");
                    currentPrefSet.Add(rangeRingLocation);
                }
                SetOrCreateElement(rangeRingLocation, "Latitude", prefSet.RangeRingCenter.Lat.ToString("F7", CultureInfo.InvariantCulture));
                SetOrCreateElement(rangeRingLocation, "Longitude", prefSet.RangeRingCenter.Lon.ToString("F7", CultureInfo.InvariantCulture));
            }

            // Apply Leader Direction settings
            SetOrCreateElement(currentPrefSet, "OwnedDataBlockPosition", prefSet.LeaderDirTracked);
            SetOrCreateElement(currentPrefSet, "UnownedDataBlockPosition", prefSet.LeaderDirAssociated);
            SetOrCreateElement(currentPrefSet, "UnassociatedDataBlockPosition", prefSet.LeaderDirUnassociated);
            SetOrCreateElement(currentPrefSet, "LeaderLength", prefSet.LeaderLength.ToString());

            // Apply History settings
            SetOrCreateElement(currentPrefSet, "HistoryNum", prefSet.HistoryCount.ToString());

            // Apply PTL settings
            SetOrCreateElement(currentPrefSet, "PTLLength", ((int)Math.Round(prefSet.PtlLength)).ToString());
            SetOrCreateElement(currentPrefSet, "PTLOwn", prefSet.PtlOwn.ToString().ToLower());
            SetOrCreateElement(currentPrefSet, "PTLAll", prefSet.PtlAll.ToString().ToLower());

            // Apply DCB Location
            SetOrCreateElement(currentPrefSet, "DCBLocation", prefSet.DcbLocation);

            // Apply Altitude Filters (CRC uses FL format like 243, DGScope uses feet*100 like 24300)
            if (prefSet.AltitudeFilterAssociated != null)
            {
                SetOrCreateElement(currentPrefSet, "AltitudeFilterAssociatedMax", (prefSet.AltitudeFilterAssociated.High * 100).ToString());
                SetOrCreateElement(currentPrefSet, "AltitudeFilterAssociatedMin", (prefSet.AltitudeFilterAssociated.Low * 100).ToString());
            }

            if (prefSet.AltitudeFilterUnassociated != null)
            {
                SetOrCreateElement(currentPrefSet, "AltitudeFilterUnAssociatedMax", (prefSet.AltitudeFilterUnassociated.High * 100).ToString());
                SetOrCreateElement(currentPrefSet, "AltitudeFilterUnAssociatedMin", (prefSet.AltitudeFilterUnassociated.Low * 100).ToString());
            }

            // Apply Brightness settings
            var brightness = currentPrefSet.Element("Brightness");
            if (brightness == null)
            {
                brightness = new XElement("Brightness");
                currentPrefSet.Add(brightness);
            }

            SetOrCreateElement(brightness, "DCB", prefSet.BrightnessDcb.ToString());
            SetOrCreateElement(brightness, "MapA", prefSet.BrightnessMpa.ToString());
            SetOrCreateElement(brightness, "MapB", prefSet.BrightnessMpb.ToString());
            SetOrCreateElement(brightness, "FullDataBlocks", prefSet.BrightnessFdb.ToString());
            SetOrCreateElement(brightness, "Lists", prefSet.BrightnessLst.ToString());
            SetOrCreateElement(brightness, "PositionSymbols", prefSet.BrightnessPos.ToString());
            SetOrCreateElement(brightness, "LimitedDataBlocks", prefSet.BrightnessLdb.ToString());
            SetOrCreateElement(brightness, "OtherFDBs", prefSet.BrightnessOth.ToString());
            SetOrCreateElement(brightness, "Tools", prefSet.BrightnessTls.ToString());
            SetOrCreateElement(brightness, "RangeRings", prefSet.BrightnessRr.ToString());
            SetOrCreateElement(brightness, "Compass", prefSet.BrightnessCmp.ToString());
            SetOrCreateElement(brightness, "BeaconTargets", prefSet.BrightnessBcn.ToString());
            SetOrCreateElement(brightness, "PrimaryTargets", prefSet.BrightnessPri.ToString());
            SetOrCreateElement(brightness, "History", prefSet.BrightnessHst.ToString());
            SetOrCreateElement(brightness, "Weather", prefSet.BrightnessWx.ToString());
            SetOrCreateElement(brightness, "WeatherContrast", prefSet.BrightnessWxc.ToString());

            // Apply Selected Video Maps (DisplayedMaps)
            if (prefSet.SelectedVideoMapIds != null && prefSet.SelectedVideoMapIds.Count > 0)
            {
                var displayedMaps = currentPrefSet.Element("DisplayedMaps");
                if (displayedMaps == null)
                {
                    displayedMaps = new XElement("DisplayedMaps");
                    currentPrefSet.Add(displayedMaps);
                }
                else
                {
                    displayedMaps.RemoveAll();
                }

                foreach (var mapId in prefSet.SelectedVideoMapIds)
                {
                    displayedMaps.Add(new XElement("int", mapId));
                }

                System.Diagnostics.Debug.WriteLine($"✓ Set DisplayedMaps: {string.Join(", ", prefSet.SelectedVideoMapIds)}");
            }

            System.Diagnostics.Debug.WriteLine($"✓ Applied CRC PrefSet settings: {prefSet.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error applying CRC PrefSet settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply ATPA volumes fetched from the VNAS API to the DGScope profile XML.
    /// Populates the ATPAVolumes element with volume definitions including
    /// runway thresholds, dimensions, scratchpad filters, and 2.5nm approach settings.
    /// </summary>
    private void ApplyAtpaVolumes(XElement root, List<VnasAtpaVolume> volumes)
    {
        // Enable ATPA globally and monitor cones
        var atpaActiveEl = root.Element("ATPAActive");
        if (atpaActiveEl != null)
            atpaActiveEl.Value = "true";
        else
            root.Add(new XElement("ATPAActive", "true"));

        var monitorConesEl = root.Element("DrawATPAMonitorCones");
        if (monitorConesEl != null)
            monitorConesEl.Value = "true";
        else
            root.Add(new XElement("DrawATPAMonitorCones", "true"));

        var atpaVolumesElement = root.Element("ATPAVolumes");
        if (atpaVolumesElement == null)
        {
            // Insert after ATPASeparationTable if it exists, otherwise add at end
            var separationTable = root.Element("ATPASeparationTable");
            atpaVolumesElement = new XElement("ATPAVolumes");
            if (separationTable != null)
            {
                separationTable.AddAfterSelf(atpaVolumesElement);
            }
            else
            {
                root.Add(atpaVolumesElement);
            }
        }
        else
        {
            atpaVolumesElement.RemoveAll();
        }

        foreach (var vol in volumes)
        {
            var volumeElement = new XElement("ATPAVolume",
                new XElement("VolumeId", vol.VolumeId),
                new XElement("Name", vol.Name),
                new XElement("Active", "true"),
                new XElement("Draw", "false"),
                new XElement("RunwayThreshold",
                    new XElement("Latitude", vol.ThresholdLatitude.ToString(CultureInfo.InvariantCulture)),
                    new XElement("Longitude", vol.ThresholdLongitude.ToString(CultureInfo.InvariantCulture))
                ),
                new XElement("TrueHeading", vol.TrueHeading),
                new XElement("MaxHeadingDeviation", vol.MaximumHeadingDeviation),
                new XElement("Ceiling", vol.Ceiling),
                new XElement("Floor", vol.Floor),
                new XElement("Length", vol.Length.ToString(CultureInfo.InvariantCulture)),
                new XElement("WidthLeft", vol.WidthLeft),
                new XElement("WidthRight", vol.WidthRight),
                new XElement("TwoPointFiveEnabled", vol.TwoPointFiveApproachEnabled.ToString().ToLowerInvariant()),
                new XElement("TwoPointFiveActive", vol.TwoPointFiveApproachEnabled.ToString().ToLowerInvariant()),
                new XElement("TwoPointFiveDistance", vol.TwoPointFiveApproachDistance.ToString(CultureInfo.InvariantCulture)),
                new XElement("Destination", vol.AirportId),
                new XElement("LeaderFilters"),
                BuildScratchpadFilters(vol.Scratchpads),
                new XElement("TcpDisplay"),
                new XElement("TcpExclusion")
            );

            atpaVolumesElement.Add(volumeElement);
        }
    }

    /// <summary>
    /// Build the ScratchpadFilters XML element from VNAS scratchpad filter data.
    /// Maps VNAS number names ("One"→1) and types ("Exclude"→"Exclusion").
    /// </summary>
    private static XElement BuildScratchpadFilters(List<VnasScratchpadFilter> scratchpads)
    {
        var element = new XElement("ScratchpadFilters");

        foreach (var sp in scratchpads)
        {
            element.Add(new XElement("ScratchpadFilter",
                new XElement("ScratchpadValue", sp.Entry),
                new XElement("ScratchpadNum", sp.ScratchpadNumber),
                new XElement("ScratchpadFilterType", sp.FilterType)
            ));
        }

        return element;
    }

}
