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

            return new AirportInfo
            {
                LocalCode = localCode,
                IcaoCode = icaoCode,
                GpsCode = gpsCode,
                Name = fields[3],
                Type = fields[2]
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

    public class AirportInfo
    {
        public string LocalCode { get; set; } = string.Empty;
        public string IcaoCode { get; set; } = string.Empty;
        public string GpsCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
