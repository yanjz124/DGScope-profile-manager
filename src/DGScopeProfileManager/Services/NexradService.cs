using System.Collections.Generic;
using System.IO;
using System.Linq;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Service for finding and selecting NEXRAD stations
/// </summary>
public class NexradService
{
    private List<NexradStation>? _stations;

    /// <summary>
    /// Load NEXRAD stations from the nexrad-stations.txt file
    /// </summary>
    public void LoadStations(string filePath)
    {
        _stations = new List<NexradStation>();

        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"NEXRAD stations file not found: {filePath}");
            return;
        }

        var lines = File.ReadAllLines(filePath);

        // Skip header lines (first 2 lines)
        foreach (var line in lines.Skip(2))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                // Fixed-width format parsing
                // ICAO is at columns 9-12 (0-indexed: 9-12)
                // NAME is at columns 19-48
                // STNTYPE is at columns 101-150
                // LAT is at columns 68-77
                // LON is at columns 78-88
                // ELEV is at columns 89-94

                if (line.Length < 100)
                    continue;

                var icao = line.Substring(9, 4).Trim();
                var name = line.Substring(19, 30).Trim();
                var latStr = line.Substring(68, 10).Trim();
                var lonStr = line.Substring(78, 11).Trim();
                var elevStr = line.Substring(89, 6).Trim();
                var stnType = line.Length >= 150 ? line.Substring(101, 50).Trim() : string.Empty;

                if (string.IsNullOrEmpty(icao) || !icao.StartsWith("K") && !icao.StartsWith("T"))
                    continue;

                if (!double.TryParse(latStr, out var lat) ||
                    !double.TryParse(lonStr, out var lon) ||
                    !int.TryParse(elevStr, out var elev))
                    continue;

                _stations.Add(new NexradStation
                {
                    Icao = icao,
                    Name = name,
                    StationType = stnType,
                    Latitude = lat,
                    Longitude = lon,
                    Elevation = elev
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing NEXRAD station line: {ex.Message}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"Loaded {_stations.Count} NEXRAD stations");
    }

    /// <summary>
    /// Find the closest NEXRAD station (WSR-88D or TDWR) to the given coordinates
    /// Prefers WSR-88D over TDWR if both are similar distance
    /// </summary>
    public NexradStation? FindClosestStation(double latitude, double longitude)
    {
        if (_stations == null || _stations.Count == 0)
            return null;

        var stationsWithDistance = _stations
            .Select(s => new
            {
                Station = s,
                Distance = s.DistanceToNauticalMiles(latitude, longitude),
                IsNexrad = s.StationType.Contains("NEXRAD"),
                IsTdwr = s.StationType.Contains("TDWR")
            })
            .OrderBy(s => s.Distance)
            .ToList();

        // Get the closest station
        var closest = stationsWithDistance.FirstOrDefault();
        if (closest == null)
            return null;

        // If it's already a NEXRAD (WSR-88D), return it
        if (closest.IsNexrad)
            return closest.Station;

        // If it's a TDWR, check if there's a WSR-88D within 20% more distance
        if (closest.IsTdwr)
        {
            var maxNexradDistance = closest.Distance * 1.2;
            var nearbyNexrad = stationsWithDistance
                .FirstOrDefault(s => s.IsNexrad && s.Distance <= maxNexradDistance);

            if (nearbyNexrad != null)
                return nearbyNexrad.Station;
        }

        return closest.Station;
    }

    /// <summary>
    /// Get all NEXRAD stations sorted by distance from the given coordinates
    /// </summary>
    public List<(NexradStation Station, double Distance)> GetAllStationsWithDistance(double latitude, double longitude)
    {
        if (_stations == null || _stations.Count == 0)
            return new List<(NexradStation, double)>();

        return _stations
            .Select(s => (Station: s, Distance: s.DistanceToNauticalMiles(latitude, longitude)))
            .OrderBy(s => s.Distance)
            .ToList();
    }
}
