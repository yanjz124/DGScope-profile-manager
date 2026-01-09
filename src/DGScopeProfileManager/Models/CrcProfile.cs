using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Models;

/// <summary>
/// Represents a CRC profile loaded from AppData\Local\CRC\ARTCCs
/// </summary>
public class CrcProfile
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ArtccCode { get; set; } = string.Empty;
    public List<VideoMapInfo> VideoMaps { get; set; } = new();
    public List<CrcTracon> Tracons { get; set; } = new();
    public double? HomeLatitude { get; set; }
    public double? HomeLongitude { get; set; }
    
    public override string ToString() => ArtccCode;
}

/// <summary>
/// Represents video map information from CRC
/// </summary>
public class VideoMapInfo
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? StarsBrightnessCategory { get; set; }
    public string? StarsId { get; set; }

    // DCB button assignments - a map can have multiple button positions
    public List<DcbButtonAssignment> ButtonAssignments { get; set; } = new();

    // For backward compatibility and UI display, show the first assignment
    public string? DcbButton => ButtonAssignments.FirstOrDefault()?.DcbButton;
    public int? DcbButtonPosition => ButtonAssignments.FirstOrDefault()?.DcbButtonPosition;
    public string? MapGroupId => ButtonAssignments.FirstOrDefault()?.MapGroupId;

    public override string ToString() => !string.IsNullOrWhiteSpace(Name) ? Name : SourceFileName;
}

/// <summary>
/// Represents a single DCB button assignment for a video map
/// A map can have multiple assignments (appear at multiple button positions)
/// </summary>
public class DcbButtonAssignment
{
    public string DcbButton { get; set; } = string.Empty; // 1-based button number (position + 1)
    public int DcbButtonPosition { get; set; } // 0-based position (0-35)
    public string MapGroupId { get; set; } = string.Empty; // ID of the mapGroup/area that assigned this button
}

/// <summary>
/// Represents a DGScope video map file entry with optional metadata
/// </summary>
public class VideoMapFile
{
    public string FileName { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? StarsBrightnessCategory { get; set; }
    public string? StarsId { get; set; }
    public string? DcbButton { get; set; }
    public int? DcbButtonPosition { get; set; } // Position in DCBMapList (0-35)
}

/// <summary>
/// Represents an area within a TRACON facility
/// </summary>
public class CrcArea
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<string> SsaAirports { get; set; } = new();
    public string? MapGroupId { get; set; } // ID of the corresponding mapGroup for this area

    public string AirportsDisplay => SsaAirports.Count > 0
        ? $"Airports: {string.Join(", ", SsaAirports)}"
        : "No airports";

    public override string ToString() => Name;
}

/// <summary>
/// Represents a TRACON facility within a CRC profile
/// </summary>
public class CrcTracon
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<VideoMapInfo> AvailableVideoMaps { get; set; } = new();
    public List<string> SsaAirports { get; set; } = new();
    public List<CrcArea> Areas { get; set; } = new();

    /// <summary>
    /// Check if this facility matches TRACON/RAPCON/CERAP/RATCF keywords
    /// </summary>
    public bool IsControlledFacility()
    {
        var keywords = new[] { "TRACON", "RAPCON", "CERAP", "RATCF" };
        return keywords.Any(kw => Type.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Convert SSA airport codes to ICAO altimeter station codes
    /// Uses AirportLookupService to convert FAA LID to proper ICAO codes
    /// </summary>
    public List<string> GetAltimeterStations(string artccCode)
    {
        var lookupService = AirportLookupService.Instance;

        return SsaAirports.Select(airport =>
        {
            return lookupService.ConvertToIcao(airport, artccCode);
        }).ToList();
    }

    public override string ToString() => $"{Name} ({Type})";
}
