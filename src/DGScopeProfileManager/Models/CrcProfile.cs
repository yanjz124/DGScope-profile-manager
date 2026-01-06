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
    public string SourceFileName { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    
    public override string ToString() => SourceFileName;
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
    /// Add 'K' prefix for most, 'P' prefix for ZAN and HCF
    /// </summary>
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

    public override string ToString() => $"{Name} ({Type})";
}
