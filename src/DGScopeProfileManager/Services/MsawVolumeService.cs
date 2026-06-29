namespace DGScopeProfileManager.Services;

/// <summary>
/// One MSAW grid cell: a square footprint with a terrain-derived ceiling.
/// </summary>
public class MsawCell
{
    /// <summary>Minimum Safe Altitude for this cell (ft MSL) = max terrain + buffer.</summary>
    public int Ceiling { get; set; }

    /// <summary>Polygon corners (lat, lon), in ring order. The ring auto-closes.</summary>
    public List<(double Lat, double Lon)> Corners { get; set; } = new();
}

/// <summary>
/// Builds MSAW volumes by tiling a radius around a facility into square cells and
/// setting each cell's ceiling from the highest terrain inside it (USGS 3DEP) plus a
/// safety buffer. Floor is always 0 (DGScope alerts when alt &lt; Ceiling inside the cell).
/// </summary>
public class MsawVolumeService
{
    public const double DefaultRadiusNm = 40.0;
    public const double DefaultCellNm = 2.0;
    public const int DefaultBufferFt = 300; // FAA MSAW buffer above terrain

    private readonly TerrainTileService _terrain = new();

    /// <summary>
    /// Generate MSAW cells covering a circle of <paramref name="radiusNm"/> around the
    /// center. Terrain comes from local SRTM tiles (downloaded/cached once), so the per-cell
    /// max is computed in memory. Cells whose terrain can't be resolved (e.g. open ocean)
    /// are skipped. <paramref name="progress"/> reports (completed, total) cell counts.
    /// </summary>
    public async Task<List<MsawCell>> GenerateAsync(
        double centerLat,
        double centerLon,
        double radiusNm = DefaultRadiusNm,
        double cellNm = DefaultCellNm,
        int bufferFt = DefaultBufferFt,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var cellGeometries = BuildGrid(centerLat, centerLon, radiusNm, cellNm);
        var cells = new List<MsawCell>(cellGeometries.Count);

        // Download/cache every tile the grid touches up front (a few per facility), then
        // compute each cell locally.
        var latPad = radiusNm / 60.0 + 0.05;
        var lonPad = radiusNm / (60.0 * Math.Max(Math.Cos(centerLat * Math.PI / 180.0), 1e-6)) + 0.05;
        await _terrain.EnsureTilesForBoundsAsync(
            centerLat - latPad, centerLon - lonPad, centerLat + latPad, centerLon + lonPad, cancellationToken);

        var total = cellGeometries.Count;
        var done = 0;
        foreach (var geo in cellGeometries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var maxFt = _terrain.GetMaxElevationFeet(geo.MinLon, geo.MinLat, geo.MaxLon, geo.MaxLat);
            if (maxFt.HasValue)
            {
                cells.Add(new MsawCell
                {
                    Ceiling = maxFt.Value + bufferFt,
                    Corners = geo.Corners
                });
            }

            // Local compute is near-instant, so throttle status updates to avoid flooding the UI.
            done++;
            if (progress != null && (done % 50 == 0 || done == total))
                progress.Report((done, total));
        }

        return cells;
    }

    private readonly struct CellGeometry
    {
        public readonly double MinLat, MinLon, MaxLat, MaxLon;
        public readonly List<(double Lat, double Lon)> Corners;

        public CellGeometry(double minLat, double minLon, double maxLat, double maxLon)
        {
            MinLat = minLat; MinLon = minLon; MaxLat = maxLat; MaxLon = maxLon;
            Corners = new List<(double, double)>
            {
                (minLat, minLon),
                (maxLat, minLon),
                (maxLat, maxLon),
                (minLat, maxLon),
            };
        }
    }

    /// <summary>
    /// Tile the coverage circle into a grid of square cells (in degrees, corrected for
    /// longitude convergence at the center latitude). Keeps cells whose center is within
    /// the radius.
    /// </summary>
    private static List<CellGeometry> BuildGrid(double centerLat, double centerLon, double radiusNm, double cellNm)
    {
        var cosLat = Math.Cos(centerLat * Math.PI / 180.0);
        if (Math.Abs(cosLat) < 1e-6) cosLat = 1e-6;

        var latStep = cellNm / 60.0;               // 1 NM = 1/60 degree latitude
        var lonStep = cellNm / (60.0 * cosLat);    // longitude degrees per NM shrink with latitude

        var n = (int)Math.Ceiling(radiusNm / cellNm);
        var cells = new List<CellGeometry>();

        for (var i = -n; i < n; i++)
        {
            var minLat = centerLat + i * latStep;
            var maxLat = minLat + latStep;
            var cellCenterLat = minLat + latStep / 2.0;

            for (var j = -n; j < n; j++)
            {
                var minLon = centerLon + j * lonStep;
                var maxLon = minLon + lonStep;
                var cellCenterLon = minLon + lonStep / 2.0;

                // Distance from facility center to cell center, in NM (equirectangular)
                var dLatNm = (cellCenterLat - centerLat) * 60.0;
                var dLonNm = (cellCenterLon - centerLon) * 60.0 * cosLat;
                if (Math.Sqrt(dLatNm * dLatNm + dLonNm * dLonNm) <= radiusNm)
                {
                    cells.Add(new CellGeometry(minLat, minLon, maxLat, maxLon));
                }
            }
        }

        return cells;
    }
}
