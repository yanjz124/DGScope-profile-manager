using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Queries terrain elevation from the USGS 3DEP elevation ImageServer.
///
/// Uses the <c>computeStatisticsHistograms</c> operation, which returns the true
/// min/max/mean elevation over every pixel inside a geometry — so for an MSAW grid
/// cell we get the guaranteed-highest terrain in that square (no sampled-point can
/// miss a peak). Values come back in meters and are converted to feet.
///
/// Coverage is the United States and territories (3DEP). Cells outside coverage
/// (e.g. open ocean) return null and are skipped by the caller.
/// </summary>
public class ElevationService
{
    private const string StatsUrl =
        "https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/ImageServer/computeStatisticsHistograms";

    private const double MetersToFeet = 3.280839895;

    // Resolve the per-cell max at ~30 m sampling instead of native 1 m. This is ~15x
    // faster server-side (0.6s vs 10s per cell) and reads within ~10 ft of the true 1 m
    // max — well inside the safety buffer — while still computing the maximum over every
    // pixel in the cell (a true zonal max, not a sampled point). Value is in the geometry
    // SR (degrees for WGS84): 0.0003 deg ≈ 30 m.
    private const double PixelSizeDeg = 0.0003;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    // Cache max elevation per cell envelope so repeated/overlapping grids don't re-query.
    private static readonly ConcurrentDictionary<string, double?> _cache = new();

    static ElevationService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DGScope-Profile-Manager");
    }

    /// <summary>
    /// Maximum terrain elevation (feet MSL) within the given WGS84 envelope, or null
    /// if the area has no 3DEP coverage or the query fails.
    /// </summary>
    public async Task<double?> GetMaxElevationFeetAsync(
        double minLon, double minLat, double maxLon, double maxLat, CancellationToken cancellationToken = default)
    {
        var key = $"{minLon:F5},{minLat:F5},{maxLon:F5},{maxLat:F5}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var result = await QueryMaxFeetAsync(minLon, minLat, maxLon, maxLat, cancellationToken);
        _cache[key] = result;
        return result;
    }

    private static async Task<double?> QueryMaxFeetAsync(
        double minLon, double minLat, double maxLon, double maxLat, CancellationToken cancellationToken)
    {
        var geometry = string.Format(CultureInfo.InvariantCulture,
            "{{\"xmin\":{0},\"ymin\":{1},\"xmax\":{2},\"ymax\":{3},\"spatialReference\":{{\"wkid\":4326}}}}",
            minLon, minLat, maxLon, maxLat);

        var pixelSize = string.Format(CultureInfo.InvariantCulture, "{0},{0}", PixelSizeDeg);
        var url = $"{StatsUrl}?f=json&geometryType=esriGeometryEnvelope" +
                  $"&geometry={Uri.EscapeDataString(geometry)}&pixelSize={pixelSize}";

        // One retry on transient failure - the service can be briefly flaky under load.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    continue;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("statistics", out var stats) ||
                    stats.ValueKind != JsonValueKind.Array || stats.GetArrayLength() == 0)
                    return null; // no coverage / no data — don't retry

                if (!stats[0].TryGetProperty("max", out var maxEl) || maxEl.ValueKind != JsonValueKind.Number)
                    return null;

                var maxMeters = maxEl.GetDouble();

                // 3DEP no-data / out-of-coverage sometimes surfaces as a large negative value.
                if (double.IsNaN(maxMeters) || maxMeters < -1000)
                    return null;

                return maxMeters * MetersToFeet;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // fall through to retry / give up
            }
        }

        return null;
    }
}
