using DGScopeProfileManager.Models;
using System.IO;
using System.Text.Json;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Reads CRC profiles from AppData\Local\CRC\ARTCCs
/// </summary>
public class CrcProfileReader
{
    private readonly string _crcPath;
    
    public CrcProfileReader()
    {
        _crcPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CRC", "ARTCCs"
        );
    }
    
    public CrcProfileReader(string customPath)
    {
        _crcPath = customPath;
    }
    
    /// <summary>
    /// Scans the CRC directory and returns all available profiles
    /// </summary>
    public List<CrcProfile> GetAllProfiles()
    {
        var profiles = new List<CrcProfile>();
        
        if (!Directory.Exists(_crcPath))
        {
            throw new DirectoryNotFoundException($"CRC directory not found: {_crcPath}");
        }
        
        // Find all JSON files in the CRC ARTCCs directory
        var jsonFiles = Directory.GetFiles(_crcPath, "*.json", SearchOption.TopDirectoryOnly);
        
        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                var profile = LoadProfile(jsonFile);
                profiles.Add(profile);
            }
            catch (Exception ex)
            {
                // Log error and continue with other files
                Console.WriteLine($"Error loading profile {jsonFile}: {ex.Message}");
            }
        }
        
        return profiles;
    }
    
    /// <summary>
    /// Loads a single CRC profile from a JSON file
    /// </summary>
    public CrcProfile LoadProfile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(json);
        var root = jsonDoc.RootElement;
        
        var profile = new CrcProfile
        {
            Path = filePath,
            Name = Path.GetFileNameWithoutExtension(filePath)
        };
        
        // Extract ARTCC code from filename (e.g., ZDC.json -> ZDC)
        profile.ArtccCode = profile.Name;
        
        // Parse video maps from the JSON structure - build a lookup map
        var videoMapsLookup = new Dictionary<string, VideoMapInfo>();
        if (root.TryGetProperty("videoMaps", out var videoMapsElement))
        {
            foreach (var mapItem in videoMapsElement.EnumerateArray())
            {
                var mapInfo = new VideoMapInfo();
                string mapId = string.Empty;
                
                if (mapItem.TryGetProperty("sourceFileName", out var fileName))
                    mapInfo.SourceFileName = fileName.GetString() ?? string.Empty;

                if (mapItem.TryGetProperty("name", out var name))
                    mapInfo.Name = name.GetString() ?? string.Empty;

                if (mapItem.TryGetProperty("shortName", out var shortName))
                    mapInfo.ShortName = shortName.GetString() ?? string.Empty;

                if (mapItem.TryGetProperty("starsBrightnessCategory", out var brightnessCategory))
                    mapInfo.StarsBrightnessCategory = brightnessCategory.GetString();

                if (mapItem.TryGetProperty("starsId", out var starsId))
                    mapInfo.StarsId = starsId.ValueKind == System.Text.Json.JsonValueKind.Number
                        ? starsId.GetRawText()
                        : starsId.GetString();
                
                if (mapItem.TryGetProperty("id", out var id))
                {
                    mapId = id.GetString() ?? string.Empty;
                    mapInfo.Id = mapId;
                }
                
                if (mapItem.TryGetProperty("tags", out var tags))
                {
                    foreach (var tag in tags.EnumerateArray())
                    {
                        if (tag.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            mapInfo.Tags.Add(tag.GetString() ?? string.Empty);
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(mapInfo.SourceFileName) && !string.IsNullOrEmpty(mapId))
                {
                    videoMapsLookup[mapId] = mapInfo;
                    profile.VideoMaps.Add(mapInfo);
                }
            }
        }
        
        // Extract home location from visibilityCenters
        if (root.TryGetProperty("visibilityCenters", out var visibilityElement))
        {
            var centers = visibilityElement.EnumerateArray().ToList();
            if (centers.Count > 0)
            {
                var first = centers[0];
                if (first.TryGetProperty("Lat", out var lat))
                    profile.HomeLatitude = lat.GetDouble();
                if (first.TryGetProperty("Lon", out var lon))
                    profile.HomeLongitude = lon.GetDouble();
            }
        }
        
        // Parse all facilities recursively (including nested TRACONs, ATCTs, etc.)
        if (root.TryGetProperty("facility", out var facilityElement))
        {
            if (facilityElement.TryGetProperty("childFacilities", out var childFacilitiesElement))
            {
                System.Diagnostics.Debug.WriteLine($"[{profile.ArtccCode}] Processing childFacilities...");
                ProcessFacilitiesRecursively(childFacilitiesElement, profile, videoMapsLookup);
                System.Diagnostics.Debug.WriteLine($"[{profile.ArtccCode}] Found {profile.Tracons.Count} TRACONs total");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[{profile.ArtccCode}] No childFacilities found in facility element");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[{profile.ArtccCode}] No facility element found in JSON");
        }
        
        return profile;
    }

    /// <summary>
    /// Recursively process all facilities in the tree and add matching ones to the profile
    /// </summary>
    private void ProcessFacilitiesRecursively(JsonElement facilitiesElement, CrcProfile profile, Dictionary<string, VideoMapInfo> videoMapsLookup)
    {
        System.Diagnostics.Debug.WriteLine($"ProcessFacilitiesRecursively: Processing {facilitiesElement.GetArrayLength()} facilities");
        
        foreach (var child in facilitiesElement.EnumerateArray())
        {
            try
            {
                var tracon = new CrcTracon();
                var hasStarsConfig = false;
                
                if (child.TryGetProperty("id", out var id))
                    tracon.Id = id.GetString() ?? string.Empty;
                    
                if (child.TryGetProperty("name", out var name))
                    tracon.Name = name.GetString() ?? string.Empty;
                    
                if (child.TryGetProperty("type", out var type))
                    tracon.Type = type.GetString() ?? string.Empty;
                
                // Check if ANY descendant node has starsConfiguration (including sectors/positions)
                hasStarsConfig = HasStarsConfigurationRecursive(child);
                
                // Extract ssaAirports from starsConfiguration
                if (child.TryGetProperty("starsConfiguration", out var starsConfig))
                {
                    hasStarsConfig = true;
                    // Extract facility location and ssaAirports from starsConfiguration.areas
                    // The visibilityCenter can be:
                    // - A string: "@{lat=39.452745; lon=-74.591952}"
                    // - An object with lat/lon properties
                    if (starsConfig.TryGetProperty("areas", out var areas))
                    {
                        var areasArray = areas.EnumerateArray().ToList();
                        if (areasArray.Count > 0)
                        {
                            var firstArea = areasArray[0];

                            // Extract location from first area
                            if (firstArea.TryGetProperty("visibilityCenter", out var visCenter))
                            {
                                // Try to parse as object first (most likely case with ConvertFrom-Json)
                                if (visCenter.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    if (visCenter.TryGetProperty("lat", out var latObj) &&
                                        latObj.TryGetDouble(out var latVal))
                                    {
                                        tracon.Latitude = latVal;
                                    }
                                    if (visCenter.TryGetProperty("lon", out var lonObj) &&
                                        lonObj.TryGetDouble(out var lonVal))
                                    {
                                        tracon.Longitude = lonVal;
                                    }
                                }
                                // Fall back to string parsing if it's a string
                                else if (visCenter.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var vCenterStr = visCenter.GetString() ?? string.Empty;
                                    ExtractLatLonFromString(vCenterStr, out var lat, out var lon);
                                    tracon.Latitude = lat;
                                    tracon.Longitude = lon;
                                }
                            }

                            // Parse all areas and collect ssaAirports
                            var ssaAirportsSet = new HashSet<string>();
                            foreach (var area in areasArray)
                            {
                                var crcArea = new CrcArea();

                                // Extract area ID and name
                                if (area.TryGetProperty("id", out var areaId))
                                    crcArea.Id = areaId.GetString() ?? string.Empty;
                                if (area.TryGetProperty("name", out var areaName))
                                    crcArea.Name = areaName.GetString() ?? string.Empty;

                                // Extract area location
                                if (area.TryGetProperty("visibilityCenter", out var areaVisCenter))
                                {
                                    if (areaVisCenter.ValueKind == System.Text.Json.JsonValueKind.Object)
                                    {
                                        if (areaVisCenter.TryGetProperty("lat", out var areaLatObj) &&
                                            areaLatObj.TryGetDouble(out var areaLatVal))
                                        {
                                            crcArea.Latitude = areaLatVal;
                                        }
                                        if (areaVisCenter.TryGetProperty("lon", out var areaLonObj) &&
                                            areaLonObj.TryGetDouble(out var areaLonVal))
                                        {
                                            crcArea.Longitude = areaLonVal;
                                        }
                                    }
                                    else if (areaVisCenter.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        var vCenterStr = areaVisCenter.GetString() ?? string.Empty;
                                        ExtractLatLonFromString(vCenterStr, out var areaLat, out var areaLon);
                                        crcArea.Latitude = areaLat;
                                        crcArea.Longitude = areaLon;
                                    }
                                }

                                // Extract ssaAirports for this area
                                if (area.TryGetProperty("ssaAirports", out var ssaAirports))
                                {
                                    foreach (var airport in ssaAirports.EnumerateArray())
                                    {
                                        if (airport.ValueKind == System.Text.Json.JsonValueKind.String)
                                        {
                                            var airportCode = airport.GetString();
                                            if (!string.IsNullOrEmpty(airportCode))
                                            {
                                                crcArea.SsaAirports.Add(airportCode);
                                                ssaAirportsSet.Add(airportCode);
                                            }
                                        }
                                    }
                                }

                                // Add area to tracon
                                if (!string.IsNullOrEmpty(crcArea.Name))
                                {
                                    tracon.Areas.Add(crcArea);
                                }
                            }

                            // Add collected airports to tracon (aggregate from all areas)
                            tracon.SsaAirports.AddRange(ssaAirportsSet.OrderBy(x => x));
                            System.Diagnostics.Debug.WriteLine($"Found {tracon.Areas.Count} areas and {ssaAirportsSet.Count} unique ssaAirports for {tracon.Id}");
                        }
                    }
                    
                    // Extract available video maps for this facility
                    if (starsConfig.TryGetProperty("videoMapIds", out var videoMapIds))
                    {
                        foreach (var mapIdElement in videoMapIds.EnumerateArray())
                        {
                            var mapId = mapIdElement.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(mapId) && videoMapsLookup.TryGetValue(mapId, out var mapInfo))
                            {
                                tracon.AvailableVideoMaps.Add(mapInfo);
                            }
                        }
                    }

                    // Map CRC mapGroups to DCB buttons when present
                    if (starsConfig.TryGetProperty("mapGroups", out var mapGroups))
                    {
                        var groupIndex = 1;
                        foreach (var group in mapGroups.EnumerateArray())
                        {
                            string? dcbButton = null;

                            if (group.TryGetProperty("button", out var buttonValue))
                            {
                                dcbButton = buttonValue.GetString();
                            }
                            else if (group.TryGetProperty("name", out var groupName))
                            {
                                dcbButton = groupName.GetString();
                            }
                            else
                            {
                                dcbButton = groupIndex.ToString();
                            }

                            if (group.TryGetProperty("mapIds", out var groupMapIds))
                            {
                                foreach (var mapIdElement in groupMapIds.EnumerateArray())
                                {
                                    var groupMapId = mapIdElement.GetString();
                                    if (string.IsNullOrWhiteSpace(groupMapId))
                                    {
                                        continue;
                                    }

                                    if (videoMapsLookup.TryGetValue(groupMapId, out var mapInfo))
                                    {
                                        // Preserve the first mapping to avoid clobbering explicit buttons
                                        if (string.IsNullOrWhiteSpace(mapInfo.DcbButton))
                                        {
                                            mapInfo.DcbButton = dcbButton;
                                        }
                                    }
                                }
                            }

                            groupIndex++;
                        }
                    }
                }
                
                // Add if it's a controlled facility OR it has explicit STARS configuration
                if (!string.IsNullOrEmpty(tracon.Id) && 
                    !string.IsNullOrEmpty(tracon.Name) && 
                    (tracon.IsControlledFacility() || hasStarsConfig))
                {
                    System.Diagnostics.Debug.WriteLine($"  ✓ Adding {tracon.Id} ({tracon.Name}) - IsControlled={tracon.IsControlledFacility()}, HasStars={hasStarsConfig}");
                    profile.Tracons.Add(tracon);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  ✗ Skipping {tracon.Id} ({tracon.Name}) - IsControlled={tracon.IsControlledFacility()}, HasStars={hasStarsConfig}");
                }
                
                // Recursively process any child facilities under this facility
                if (child.TryGetProperty("childFacilities", out var nestedChildFacilities))
                {
                    ProcessFacilitiesRecursively(nestedChildFacilities, profile, videoMapsLookup);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  ✗ ERROR processing facility: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"     Stack: {ex.StackTrace}");
                // Skip malformed child facilities
            }
        }
    }
    
    /// <summary>
    /// Extract latitude and longitude from string format "@{lat=39.452745; lon=-74.591952}"
    /// </summary>
    private void ExtractLatLonFromString(string input, out double? latitude, out double? longitude)
    {
        latitude = null;
        longitude = null;
        
        if (string.IsNullOrEmpty(input))
            return;
        
        try
        {
            // Remove "@{" prefix and "}" suffix
            var content = input.Replace("@{", "").Replace("}", "").Trim();
            
            // Split by semicolon
            var parts = content.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();
                    
                    if (key.Equals("lat", StringComparison.OrdinalIgnoreCase) &&
                        double.TryParse(value, out var latVal))
                    {
                        latitude = latVal;
                    }
                    else if (key.Equals("lon", StringComparison.OrdinalIgnoreCase) &&
                             double.TryParse(value, out var lonVal))
                    {
                        longitude = lonVal;
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, leave null
        }
    }

    /// <summary>
    /// Recursively checks if a facility element or any descendant (sectors, child facilities) has starsConfiguration
    /// This handles cases where STARS config is on the facility itself or nested in sectors/positions
    /// </summary>
    private bool HasStarsConfigurationRecursive(JsonElement element)
    {
        // Check if this element has starsConfiguration
        if (element.TryGetProperty("starsConfiguration", out _))
        {
            return true;
        }

        // Check childFacilities recursively
        if (element.TryGetProperty("childFacilities", out var childFacilities))
        {
            foreach (var child in childFacilities.EnumerateArray())
            {
                if (HasStarsConfigurationRecursive(child))
                {
                    return true;
                }
            }
        }

        // Check sectors/positions that may have starsConfiguration
        if (element.TryGetProperty("positions", out var positions))
        {
            foreach (var position in positions.EnumerateArray())
            {
                if (position.TryGetProperty("starsConfiguration", out _))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
