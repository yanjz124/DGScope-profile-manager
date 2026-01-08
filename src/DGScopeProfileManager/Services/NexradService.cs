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
        System.Diagnostics.Debug.WriteLine($"📄 Read {lines.Length} lines from NEXRAD file");

        if (lines.Length > 0)
        {
            System.Diagnostics.Debug.WriteLine($"Header line 1: {lines[0]}");
            if (lines.Length > 1)
                System.Diagnostics.Debug.WriteLine($"Header line 2: {lines[1]}");
        }

        int lineNum = 0;
        int tooShort = 0;
        int badIcao = 0;
        int parseError = 0;
        int successCount = 0;

        // Skip header lines (first 2 lines)
        foreach (var line in lines.Skip(2))
        {
            lineNum++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                // Fixed-width format parsing based on header line:
                // NCDCID   ICAO WBAN  NAME                           COUNTRY              ST COUNTY                         LAT       LON        ELEV   UTC   STNTYPE
                // -------- ---- ----- ------------------------------ -------------------- -- ------------------------------ --------- ---------- ------ ----- --------------------------------------------------
                // ICAO: positions 9-12 (4 chars)
                // NAME: positions 20-49 (30 chars)
                // LAT: positions 106-114 (9 chars)
                // LON: positions 116-125 (10 chars)
                // ELEV: positions 127-132 (6 chars)
                // STNTYPE: positions 140-189 (50 chars)

                if (line.Length < 133)
                {
                    tooShort++;
                    if (tooShort <= 3)
                        System.Diagnostics.Debug.WriteLine($"  Line {lineNum} too short ({line.Length} chars): {line.Substring(0, Math.Min(50, line.Length))}");
                    continue;
                }

                var icao = line.Substring(9, 4).Trim();
                var name = line.Substring(20, 30).Trim();
                var latStr = line.Substring(106, 9).Trim();
                var lonStr = line.Substring(116, 10).Trim();
                var elevStr = line.Substring(127, 6).Trim();
                var stnType = line.Length >= 190 ? line.Substring(140, 50).Trim() : string.Empty;

                // Debug first few lines
                if (lineNum <= 5)
                {
                    System.Diagnostics.Debug.WriteLine($"  Line {lineNum}: ICAO='{icao}' Name='{name}' Lat='{latStr}' Lon='{lonStr}' Type='{stnType}'");
                }

                if (string.IsNullOrEmpty(icao) || (!icao.StartsWith("K") && !icao.StartsWith("T")))
                {
                    badIcao++;
                    if (badIcao <= 3)
                        System.Diagnostics.Debug.WriteLine($"  Line {lineNum} bad ICAO '{icao}' (must start with K or T)");
                    continue;
                }

                if (!double.TryParse(latStr, out var lat) ||
                    !double.TryParse(lonStr, out var lon) ||
                    !int.TryParse(elevStr, out var elev))
                {
                    parseError++;
                    if (parseError <= 3)
                        System.Diagnostics.Debug.WriteLine($"  Line {lineNum} parse error: ICAO={icao} lat='{latStr}' lon='{lonStr}' elev='{elevStr}'");
                    continue;
                }

                _stations.Add(new NexradStation
                {
                    Icao = icao,
                    Name = name,
                    StationType = stnType,
                    Latitude = lat,
                    Longitude = lon,
                    Elevation = elev
                });

                successCount++;
                if (successCount <= 3)
                {
                    System.Diagnostics.Debug.WriteLine($"  ✓ Added: {icao} - {name} ({lat}, {lon})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error parsing line {lineNum}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Line content: {line.Substring(0, Math.Min(100, line.Length))}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"📊 NEXRAD Parsing Summary:");
        System.Diagnostics.Debug.WriteLine($"   Total lines processed: {lineNum}");
        System.Diagnostics.Debug.WriteLine($"   Successfully loaded: {_stations.Count}");
        System.Diagnostics.Debug.WriteLine($"   Too short: {tooShort}");
        System.Diagnostics.Debug.WriteLine($"   Bad ICAO: {badIcao}");
        System.Diagnostics.Debug.WriteLine($"   Parse errors: {parseError}");
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
