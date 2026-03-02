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
    /// Fetch ATPA volumes for a given ARTCC from the VNAS API
    /// </summary>
    /// <param name="artccCode">ARTCC code (e.g., "ZDC") - will be uppercased</param>
    /// <returns>List of ATPA volumes, or empty list on error</returns>
    public async Task<List<VnasAtpaVolume>> FetchAtpaVolumesAsync(string artccCode)
    {
        var volumes = new List<VnasAtpaVolume>();

        try
        {
            var url = $"{BaseUrl}/{artccCode.ToUpperInvariant()}/";
            Debug.WriteLine($"Fetching ATPA volumes from: {url}");

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"VNAS API returned {response.StatusCode} for {artccCode}");
                return volumes;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            // ATPA volumes are under facility.childFacilities[].starsConfiguration.atpaVolumes[]
            if (!root.TryGetProperty("facility", out var facility))
            {
                Debug.WriteLine("No facility found in VNAS response");
                return volumes;
            }

            if (!facility.TryGetProperty("childFacilities", out var childFacilities))
            {
                Debug.WriteLine("No childFacilities found in facility");
                return volumes;
            }

            foreach (var child in childFacilities.EnumerateArray())
            {
                if (!child.TryGetProperty("starsConfiguration", out var starsConfig))
                    continue;

                if (!starsConfig.TryGetProperty("atpaVolumes", out var atpaVolumes))
                    continue;

                foreach (var vol in atpaVolumes.EnumerateArray())
                {
                    try
                    {
                        var volume = ParseVolume(vol);
                        if (volume != null)
                        {
                            volumes.Add(volume);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error parsing ATPA volume: {ex.Message}");
                    }
                }
            }

            Debug.WriteLine($"Fetched {volumes.Count} ATPA volumes for {artccCode}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching ATPA volumes for {artccCode}: {ex.Message}");
        }

        return volumes;
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
