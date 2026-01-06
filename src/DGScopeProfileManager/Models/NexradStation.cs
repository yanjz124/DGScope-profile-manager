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
