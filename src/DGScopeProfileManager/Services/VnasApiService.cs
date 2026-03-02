using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Service for fetching ATPA volume data from the VATSIM VNAS API
/// </summary>
public class VnasApiService
{
    private const string BaseUrl = "https://data-api.vnas.vatsim.net/api/artccs";

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    static VnasApiService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DGScope-Profile-Manager");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Fetch ATPA volumes for a given ARTCC, grouped by child facility ID (TRACON).
    /// </summary>
    /// <param name="artccCode">ARTCC code (e.g., "ZDC") - will be uppercased</param>
    /// <returns>Dictionary of facility ID → volumes, or empty on error</returns>
    public async Task<Dictionary<string, List<VnasAtpaVolume>>> FetchAtpaVolumesByFacilityAsync(string artccCode)
    {
        var result = new Dictionary<string, List<VnasAtpaVolume>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var url = $"{BaseUrl}/{artccCode.ToUpperInvariant()}/";
            Debug.WriteLine($"Fetching ATPA volumes from: {url}");

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"VNAS API returned {response.StatusCode} for {artccCode}");
                return result;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            if (!root.TryGetProperty("facility", out var facility))
            {
                Debug.WriteLine("No facility found in VNAS response");
                return result;
            }

            if (!facility.TryGetProperty("childFacilities", out var childFacilities))
            {
                Debug.WriteLine("No childFacilities found in facility");
                return result;
            }

            foreach (var child in childFacilities.EnumerateArray())
            {
                var childId = child.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(childId)) continue;

                if (!child.TryGetProperty("starsConfiguration", out var starsConfig))
                    continue;

                if (!starsConfig.TryGetProperty("atpaVolumes", out var atpaVolumes))
                    continue;

                var facilityVolumes = new List<VnasAtpaVolume>();
                foreach (var vol in atpaVolumes.EnumerateArray())
                {
                    try
                    {
                        var volume = ParseVolume(vol);
                        if (volume != null)
                            facilityVolumes.Add(volume);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error parsing ATPA volume: {ex.Message}");
                    }
                }

                if (facilityVolumes.Count > 0)
                    result[childId] = facilityVolumes;
            }

            Debug.WriteLine($"Fetched ATPA volumes for {artccCode}: {result.Count} facilities, {result.Values.Sum(v => v.Count)} total volumes");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching ATPA volumes for {artccCode}: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Fetch all ATPA volumes for a given ARTCC (flattened across all child facilities).
    /// </summary>
    public async Task<List<VnasAtpaVolume>> FetchAtpaVolumesAsync(string artccCode)
    {
        var grouped = await FetchAtpaVolumesByFacilityAsync(artccCode);
        return grouped.Values.SelectMany(v => v).ToList();
    }

    /// <summary>
    /// Extract the TRACON/facility ID from a profile name (e.g., "PCT_MTV N" → "PCT").
    /// </summary>
    public static string GetFacilityIdFromProfileName(string profileName)
    {
        var idx = profileName.IndexOf('_');
        return idx > 0 ? profileName[..idx] : profileName;
    }

    private static VnasAtpaVolume? ParseVolume(JsonElement vol)
    {
        var volumeId = vol.GetProperty("volumeId").GetString() ?? "";
        var name = vol.GetProperty("name").GetString() ?? "";

        if (string.IsNullOrEmpty(volumeId))
            return null;

        var volume = new VnasAtpaVolume
        {
            VolumeId = volumeId,
            Name = name,
            AirportId = vol.TryGetProperty("airportId", out var aid) ? aid.GetString() ?? "" : "",
            Ceiling = vol.TryGetProperty("ceiling", out var ceil) ? ceil.GetInt32() : 0,
            Floor = vol.TryGetProperty("floor", out var floor) ? floor.GetInt32() : 0,
            MagneticHeading = vol.TryGetProperty("magneticHeading", out var mh) ? mh.GetInt32() : 0,
            MaximumHeadingDeviation = vol.TryGetProperty("maximumHeadingDeviation", out var mhd) ? mhd.GetInt32() : 90,
            Length = vol.TryGetProperty("length", out var len) ? len.GetDouble() : 0,
            WidthLeft = vol.TryGetProperty("widthLeft", out var wl) ? wl.GetInt32() : 0,
            WidthRight = vol.TryGetProperty("widthRight", out var wr) ? wr.GetInt32() : 0,
            TwoPointFiveApproachEnabled = vol.TryGetProperty("twoPointFiveApproachEnabled", out var tpfe) && tpfe.GetBoolean(),
            TwoPointFiveApproachDistance = vol.TryGetProperty("twoPointFiveApproachDistance", out var tpfd) ? tpfd.GetDouble() : 0
        };

        // Parse runway threshold
        if (vol.TryGetProperty("runwayThreshold", out var threshold))
        {
            volume.ThresholdLatitude = threshold.TryGetProperty("lat", out var lat) ? lat.GetDouble() : 0;
            volume.ThresholdLongitude = threshold.TryGetProperty("lon", out var lon) ? lon.GetDouble() : 0;
        }

        // Parse scratchpads
        if (vol.TryGetProperty("scratchpads", out var scratchpads))
        {
            foreach (var sp in scratchpads.EnumerateArray())
            {
                var entry = sp.TryGetProperty("entry", out var e) ? e.GetString() ?? "" : "";
                var spNumStr = sp.TryGetProperty("scratchPadNumber", out var spn) ? spn.GetString() ?? "One" : "One";
                var typeStr = sp.TryGetProperty("type", out var t) ? t.GetString() ?? "Exclude" : "Exclude";

                volume.Scratchpads.Add(new VnasScratchpadFilter
                {
                    Entry = entry,
                    ScratchpadNumber = ParseScratchpadNumber(spNumStr),
                    FilterType = typeStr == "Ineligible" ? "Ineligible" : "Exclusion"
                });
            }
        }

        return volume;
    }

    private static int ParseScratchpadNumber(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "one" => 1,
            "two" => 2,
            "three" => 3,
            "four" => 4,
            _ => 1
        };
    }

    /// <summary>
    /// Approximate magnetic declination for continental US using a polynomial model.
    /// Positive = east declination, negative = west declination.
    /// Accurate to within ~2° for CONUS (sufficient for ATPA volumes).
    /// </summary>
    public static double CalculateMagneticDeclination(double latitude, double longitude)
    {
        // Polynomial fit from NOAA WMM reference points (~2025 epoch)
        // Primary dependence on longitude with latitude correction
        var lon = longitude;
        var lat = latitude;
        var declination = -0.00411 * lon * lon - 1.340 * lon - 89.31;
        declination -= 0.13 * (lat - 37.0);
        return declination;
    }

    /// <summary>
    /// Convert magnetic heading to true heading using the runway threshold position.
    /// </summary>
    public static int MagneticToTrueHeading(int magneticHeading, double latitude, double longitude)
    {
        var declination = CalculateMagneticDeclination(latitude, longitude);
        var trueHeading = magneticHeading + declination;

        // Normalize to 0-360
        trueHeading = ((trueHeading % 360) + 360) % 360;
        return (int)Math.Round(trueHeading);
    }
}

/// <summary>
/// ATPA volume data from the VNAS API
/// </summary>
public class VnasAtpaVolume
{
    public string VolumeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string AirportId { get; set; } = "";
    public double ThresholdLatitude { get; set; }
    public double ThresholdLongitude { get; set; }
    public int MagneticHeading { get; set; }
    public int MaximumHeadingDeviation { get; set; } = 90;
    public int Ceiling { get; set; }
    public int Floor { get; set; }
    public double Length { get; set; }
    public int WidthLeft { get; set; }
    public int WidthRight { get; set; }
    public bool TwoPointFiveApproachEnabled { get; set; }
    public double TwoPointFiveApproachDistance { get; set; }
    public List<VnasScratchpadFilter> Scratchpads { get; set; } = new();

    /// <summary>
    /// True heading computed from magnetic heading and declination at the runway threshold.
    /// </summary>
    public int TrueHeading => VnasApiService.MagneticToTrueHeading(MagneticHeading, ThresholdLatitude, ThresholdLongitude);
}

/// <summary>
/// Scratchpad filter from VNAS ATPA volume
/// </summary>
public class VnasScratchpadFilter
{
    public string Entry { get; set; } = "";
    public int ScratchpadNumber { get; set; } = 1;
    public string FilterType { get; set; } = "Exclusion"; // "Exclusion" or "Ineligible"
}
