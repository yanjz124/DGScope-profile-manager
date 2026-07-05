namespace DGScopeProfileManager.Models;

/// <summary>
/// Represents a NEXRAD weather radar station
/// </summary>
public class NexradStation
{
    public string Icao { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StationType { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Elevation { get; set; }

    /// <summary>
    /// True if this station is a TDWR (Terminal Doppler Weather Radar) rather than a WSR-88D.
    /// Prefers the parsed station type; falls back to the ICAO prefix (TDWR ICAOs start with 'T',
    /// WSR-88D with 'K') when the type is unavailable.
    /// </summary>
    public bool IsTdwr => StationType.Contains("TDWR", StringComparison.OrdinalIgnoreCase)
        || (string.IsNullOrEmpty(StationType) && IsTdwrSensorId(Icao));

    /// <summary>
    /// Determine TDWR vs WSR-88D from a sensor ICAO alone. TDWR sites use a 'T' prefix,
    /// WSR-88D sites use a 'K' prefix.
    /// </summary>
    public static bool IsTdwrSensorId(string sensorId) =>
        !string.IsNullOrWhiteSpace(sensorId) &&
        sensorId.TrimStart().StartsWith("T", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Build the NWS radar overlay URL for a sensor. WSR-88D uses Base Reflectivity product 94
    /// (DS.p94r0); TDWR uses Base Reflectivity product 180 (DS.180z0). The two products live at
    /// different paths, so the correct one must be selected per radar type.
    /// </summary>
    public static string BuildRadarUrl(string sensorId, bool isTdwr)
    {
        var product = isTdwr ? "180z0" : "p94r0";
        return $"https://tgftp.nws.noaa.gov/SL.us008001/DF.of/DC.radar/DS.{product}/SI.{sensorId.ToLower()}/sn.last";
    }

    /// <summary>
    /// Calculate distance in nautical miles to another lat/lon point
    /// </summary>
    public double DistanceToNauticalMiles(double targetLat, double targetLon)
    {
        const double EarthRadiusKm = 6371.0;
        const double KmToNauticalMiles = 0.539957;

        var lat1Rad = DegreesToRadians(Latitude);
        var lat2Rad = DegreesToRadians(targetLat);
        var deltaLat = DegreesToRadians(targetLat - Latitude);
        var deltaLon = DegreesToRadians(targetLon - Longitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceKm = EarthRadiusKm * c;

        return distanceKm * KmToNauticalMiles;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
