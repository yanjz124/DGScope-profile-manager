using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Local terrain elevation from SRTM <c>.hgt</c> tiles.
///
/// Tiles come from the AWS "elevation-tiles-prod" open dataset (Skadi format, ~30 m,
/// no auth/key). Each 1°×1° tile is a raw big-endian Int16 grid of meters, so once a
/// tile is downloaded (cached to AppData) the max elevation over any area is computed
/// instantly in memory — no per-cell web calls, and fully offline after first download.
/// </summary>
public class TerrainTileService
{
    private const string TileBaseUrl = "https://s3.amazonaws.com/elevation-tiles-prod/skadi";
    private const double MetersToFeet = 3.280839895;
    private const short VoidValue = -32768;
    private const int MaxCachedTiles = 16; // cap memory (~26 MB per 3601² tile)

    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    private static readonly ConcurrentDictionary<string, Tile?> _tiles = new();
    private static readonly string _cacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DGScopeProfileManager", "dem");

    static TerrainTileService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DGScope-Profile-Manager");
        Directory.CreateDirectory(_cacheDir);
    }

    private sealed class Tile
    {
        public required short[] Data;
        public required int Side;       // samples per edge (3601 for SRTM1, 1201 for SRTM3)
        public required int TileLat;    // integer SW-corner latitude
        public required int TileLon;    // integer SW-corner longitude
    }

    /// <summary>
    /// Ensure every 1°×1° tile covering the bounding box is downloaded and parsed.
    /// Safe to call before a batch of <see cref="GetMaxElevationFeet"/> lookups.
    /// </summary>
    public async Task EnsureTilesForBoundsAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken cancellationToken = default)
    {
        var keys = new List<(int lat, int lon)>();
        for (var lat = (int)Math.Floor(minLat); lat <= (int)Math.Floor(maxLat); lat++)
            for (var lon = (int)Math.Floor(minLon); lon <= (int)Math.Floor(maxLon); lon++)
                keys.Add((lat, lon));

        foreach (var (lat, lon) in keys)
            await EnsureTileAsync(lat, lon, cancellationToken);
    }

    /// <summary>
    /// Maximum terrain elevation (ft MSL) within the WGS84 bounding box, scanning every
    /// SRTM sample inside it. Returns null if no tile/data covers the area.
    /// </summary>
    public int? GetMaxElevationFeet(double minLon, double minLat, double maxLon, double maxLat)
    {
        short maxMeters = VoidValue;

        for (var lat = (int)Math.Floor(minLat); lat <= (int)Math.Floor(maxLat); lat++)
        {
            for (var lon = (int)Math.Floor(minLon); lon <= (int)Math.Floor(maxLon); lon++)
            {
                if (!_tiles.TryGetValue(TileKey(lat, lon), out var tile) || tile == null)
                    continue;

                var m = MaxInTile(tile, minLat, minLon, maxLat, maxLon);
                if (m > maxMeters) maxMeters = m;
            }
        }

        if (maxMeters == VoidValue || maxMeters < -1000)
            return null;

        return (int)Math.Round(maxMeters * MetersToFeet);
    }

    private static short MaxInTile(Tile tile, double minLat, double minLon, double maxLat, double maxLon)
    {
        var side = tile.Side;
        var last = side - 1;

        // Clamp the requested box to this tile's degree extent
        var loLat = Math.Max(minLat, tile.TileLat);
        var hiLat = Math.Min(maxLat, tile.TileLat + 1);
        var loLon = Math.Max(minLon, tile.TileLon);
        var hiLon = Math.Min(maxLon, tile.TileLon + 1);
        if (loLat > hiLat || loLon > hiLon) return VoidValue;

        // Row 0 is the north edge (lat = TileLat + 1); rows increase southward.
        var rowTop = (int)Math.Floor((tile.TileLat + 1 - hiLat) * last);
        var rowBot = (int)Math.Ceiling((tile.TileLat + 1 - loLat) * last);
        var colLeft = (int)Math.Floor((loLon - tile.TileLon) * last);
        var colRight = (int)Math.Ceiling((hiLon - tile.TileLon) * last);

        rowTop = Math.Clamp(rowTop, 0, last);
        rowBot = Math.Clamp(rowBot, 0, last);
        colLeft = Math.Clamp(colLeft, 0, last);
        colRight = Math.Clamp(colRight, 0, last);

        short max = VoidValue;
        for (var r = rowTop; r <= rowBot; r++)
        {
            var rowOffset = r * side;
            for (var c = colLeft; c <= colRight; c++)
            {
                var v = tile.Data[rowOffset + c];
                if (v != VoidValue && v > max) max = v;
            }
        }
        return max;
    }

    private async Task EnsureTileAsync(int lat, int lon, CancellationToken cancellationToken)
    {
        var key = TileKey(lat, lon);
        if (_tiles.ContainsKey(key))
            return;

        // Bound memory: if we've accumulated many tiles, drop them (disk cache remains).
        if (_tiles.Count >= MaxCachedTiles)
            _tiles.Clear();

        try
        {
            var name = TileName(lat, lon);           // e.g. N37W080
            var hgtPath = Path.Combine(_cacheDir, name + ".hgt");

            byte[] bytes;
            if (File.Exists(hgtPath))
            {
                bytes = await File.ReadAllBytesAsync(hgtPath, cancellationToken);
            }
            else
            {
                var folder = name[..3]; // N37
                var url = $"{TileBaseUrl}/{folder}/{name}.hgt.gz";
                using var resp = await _httpClient.GetAsync(url, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    _tiles[key] = null; // no tile here (e.g. open ocean) - remember the miss
                    return;
                }

                await using var netStream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                await using var gz = new GZipStream(netStream, CompressionMode.Decompress);
                using var ms = new MemoryStream();
                await gz.CopyToAsync(ms, cancellationToken);
                bytes = ms.ToArray();
                await File.WriteAllBytesAsync(hgtPath, bytes, cancellationToken);
            }

            _tiles[key] = ParseHgt(bytes, lat, lon);
        }
        catch
        {
            _tiles[key] = null;
        }
    }

    private static Tile? ParseHgt(byte[] bytes, int lat, int lon)
    {
        var samples = bytes.Length / 2;
        var side = (int)Math.Round(Math.Sqrt(samples));
        if (side * side != samples)
            return null;

        // .hgt is big-endian Int16; convert to host order.
        var data = new short[samples];
        for (int i = 0, b = 0; i < samples; i++, b += 2)
            data[i] = (short)((bytes[b] << 8) | bytes[b + 1]);

        return new Tile { Data = data, Side = side, TileLat = lat, TileLon = lon };
    }

    private static string TileKey(int lat, int lon) => $"{lat},{lon}";

    private static string TileName(int lat, int lon)
    {
        var ns = lat >= 0 ? 'N' : 'S';
        var ew = lon >= 0 ? 'E' : 'W';
        return $"{ns}{Math.Abs(lat):D2}{ew}{Math.Abs(lon):D3}";
    }
}
