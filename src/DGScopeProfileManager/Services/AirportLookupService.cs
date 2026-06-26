using System.Globalization;
using System.IO;
using System.Reflection;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Service to convert FAA LID codes to ICAO codes using OurAirports database
/// Data source: https://ourairports.com/data/ (Public Domain, updated nightly)
/// </summary>
public class AirportLookupService
{
    private readonly Dictionary<string, AirportInfo> _airportsByLocal = new();
    private readonly Dictionary<string, AirportInfo> _airportsByIcao = new();
    private static AirportLookupService? _instance;
    private static readonly object _lock = new();

    public static AirportLookupService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new AirportLookupService();
                }
            }
            return _instance;
        }
    }

    private AirportLookupService()
    {
        LoadAirportData();
    }

    private void LoadAirportData()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DGScopeProfileManager.Resources.airports.csv";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Console.WriteLine($"Warning: Could not find embedded resource: {resourceName}");
                return;
            }

            using var reader = new StreamReader(stream);

            // Skip header line
            reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var airport = ParseAirportLine(line);
                if (airport != null)
                {
                    // Index by local code (FAA LID)
                    if (!string.IsNullOrWhiteSpace(airport.LocalCode))
                    {
                        _airportsByLocal[airport.LocalCode.ToUpper()] = airport;
                    }

                    // Index by ICAO code
                    if (!string.IsNullOrWhiteSpace(airport.IcaoCode))
                    {
                        _airportsByIcao[airport.IcaoCode.ToUpper()] = airport;
                    }
                }
            }

            Console.WriteLine($"Loaded {_airportsByLocal.Count} airports by local code, {_airportsByIcao.Count} by ICAO");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading airport data: {ex.Message}");
        }
    }

    private AirportInfo? ParseAirportLine(string line)
    {
        try
        {
            var fields = ParseCsvLine(line);
            if (fields.Length < 17) return null;

            // Only include airports in the US
            if (fields[8] != "US") return null;

            var localCode = fields[15]; // local_code
            var icaoCode = fields[12];  // icao_code
            var gpsCode = fields[14];   // gps_code

            // Skip if no local code
            if (string.IsNullOrWhiteSpace(localCode))
                return null;

            // elevation_ft is integer feet in the OurAirports data
            int? elevation = int.TryParse(fields[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out var elev)
                ? elev : null;
            double? lat = double.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var la)
                ? la : null;
            double? lon = double.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var lo)
                ? lo : null;

            return new AirportInfo
            {
                LocalCode = localCode,
                IcaoCode = icaoCode,
                GpsCode = gpsCode,
                Name = fields[3],
                Type = fields[2],
                ElevationFt = elevation,
                Latitude = lat,
                Longitude = lon
            };
        }
        catch
        {
            return null;
        }
    }

    private string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = "";

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field);
                field = "";
            }
            else
            {
                field += c;
            }
        }
        fields.Add(field);

        return fields.ToArray();
    }

    /// <summary>
    /// Convert FAA LID to ICAO code
    /// Returns ICAO if available, otherwise GPS code (K-prefix), otherwise fallback to K+LID
    /// </summary>
    public string ConvertToIcao(string faaLid, string? artccCode = null)
    {
        if (string.IsNullOrWhiteSpace(faaLid))
            return string.Empty;

        var upperLid = faaLid.ToUpper();

        // Try to find by local code
        if (_airportsByLocal.TryGetValue(upperLid, out var airport))
        {
            // Prefer ICAO code if available
            if (!string.IsNullOrWhiteSpace(airport.IcaoCode))
                return airport.IcaoCode;

            // Fall back to GPS code (often K-prefixed)
            if (!string.IsNullOrWhiteSpace(airport.GpsCode))
                return airport.GpsCode;
        }

        // Fallback: Use the old brute-force approach
        var isPacific = artccCode?.ToUpper() is "ZAN" or "ZHN" or "ZUA";
        var prefix = isPacific ? "P" : "K";
        return prefix + upperLid;
    }

    /// <summary>
    /// Resolve an airport by FAA local code or ICAO code (e.g. "BWI" or "KBWI").
    /// </summary>
    public AirportInfo? GetAirport(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var upper = code.ToUpper();
        if (_airportsByLocal.TryGetValue(upper, out var byLocal))
            return byLocal;
        if (_airportsByIcao.TryGetValue(upper, out var byIcao))
            return byIcao;
        return null;
    }

    /// <summary>
    /// Field elevation (ft MSL) for an airport, or null if unknown.
    /// </summary>
    public int? GetElevationFt(string code) => GetAirport(code)?.ElevationFt;

    /// <summary>
    /// Whether the airport has an official ICAO identifier (per CRC, CA suppression is
    /// emitted only for ICAO airports). True if the resolved record carries an ICAO code,
    /// or the code itself is already a US ICAO id (K/P-prefixed 4-letter).
    /// </summary>
    public bool HasIcaoId(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var airport = GetAirport(code);
        if (airport != null && !string.IsNullOrWhiteSpace(airport.IcaoCode))
            return true;

        var upper = code.ToUpper();
        return upper.Length == 4 && (upper[0] == 'K' || upper[0] == 'P') && _airportsByIcao.ContainsKey(upper);
    }

    /// <summary>
    /// ICAO-identified airports within <paramref name="radiusNm"/> of a point that have a
    /// known location and field elevation. Used to place MSAW suppression circles over fields.
    /// </summary>
    public List<AirportInfo> GetIcaoAirportsWithin(double centerLat, double centerLon, double radiusNm)
    {
        var cosLat = Math.Cos(centerLat * Math.PI / 180.0);
        var results = new List<AirportInfo>();

        foreach (var airport in _airportsByIcao.Values)
        {
            if (airport.Latitude == null || airport.Longitude == null || airport.ElevationFt == null)
                continue;

            var dLatNm = (airport.Latitude.Value - centerLat) * 60.0;
            var dLonNm = (airport.Longitude.Value - centerLon) * 60.0 * cosLat;
            if (Math.Sqrt(dLatNm * dLatNm + dLonNm * dLonNm) <= radiusNm)
                results.Add(airport);
        }

        return results;
    }

    public class AirportInfo
    {
        public string LocalCode { get; set; } = string.Empty;
        public string IcaoCode { get; set; } = string.Empty;
        public string GpsCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int? ElevationFt { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
