using System.IO;
using System.Text.Json;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Service to read CRC STARS PrefSets from the PrefSets/STARS folder
/// </summary>
public class CrcPrefSetReader
{
    private readonly string _prefSetsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initialize with the CRC root folder path
    /// </summary>
    public CrcPrefSetReader(string crcRootPath)
    {
        _prefSetsPath = Path.Combine(crcRootPath, "PrefSets", "STARS");
    }

    /// <summary>
    /// Check if PrefSets are available for a given facility ID
    /// </summary>
    public bool HasPrefSets(string facilityId)
    {
        if (string.IsNullOrWhiteSpace(facilityId))
            return false;

        var filePath = Path.Combine(_prefSetsPath, $"{facilityId}.json");
        return File.Exists(filePath);
    }

    /// <summary>
    /// Get all available PrefSets for a facility
    /// </summary>
    public List<CrcPrefSet> GetPrefSets(string facilityId)
    {
        if (string.IsNullOrWhiteSpace(facilityId))
            return new List<CrcPrefSet>();

        var filePath = Path.Combine(_prefSetsPath, $"{facilityId}.json");
        if (!File.Exists(filePath))
            return new List<CrcPrefSet>();

        try
        {
            var json = File.ReadAllText(filePath);
            var prefSets = JsonSerializer.Deserialize<List<CrcPrefSet>>(json, JsonOptions);
            return prefSets ?? new List<CrcPrefSet>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading PrefSets for {facilityId}: {ex.Message}");
            return new List<CrcPrefSet>();
        }
    }

    /// <summary>
    /// Get list of all facility IDs that have PrefSets available
    /// </summary>
    public List<string> GetAvailableFacilities()
    {
        if (!Directory.Exists(_prefSetsPath))
            return new List<string>();

        try
        {
            return Directory.GetFiles(_prefSetsPath, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error listing PrefSets: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Convert a CRC PrefSet to DGScope PrefSetSettings
    /// </summary>
    public static PrefSetSettings ConvertToPrefSetSettings(CrcPrefSet crcPrefSet)
    {
        var settings = new PrefSetSettings
        {
            // Range
            Range = (int)Math.Round(crcPrefSet.Range),

            // Display Center
            ScreenCenterPointLatitude = crcPrefSet.DisplayCenter?.Lat ?? 0,
            ScreenCenterPointLongitude = crcPrefSet.DisplayCenter?.Lon ?? 0,

            // Range Rings
            RangeRingSpacing = crcPrefSet.RangeRingSpacing,
            RangeRingsCentered = !crcPrefSet.RangeRingsOffCenter,
            RangeRingLocationLatitude = crcPrefSet.RangeRingCenter?.Lat ?? crcPrefSet.DisplayCenter?.Lat ?? 0,
            RangeRingLocationLongitude = crcPrefSet.RangeRingCenter?.Lon ?? crcPrefSet.DisplayCenter?.Lon ?? 0,

            // Leader Directions
            OwnedDataBlockPosition = crcPrefSet.LeaderDirTracked,
            UnownedDataBlockPosition = crcPrefSet.LeaderDirAssociated,
            UnassociatedDataBlockPosition = crcPrefSet.LeaderDirUnassociated,
            LeaderLength = crcPrefSet.LeaderLength,

            // History
            HistoryNum = crcPrefSet.HistoryCount,

            // PTL
            PTLLength = (int)Math.Round(crcPrefSet.PtlLength),
            PTLOwn = crcPrefSet.PtlOwn,
            PTLAll = crcPrefSet.PtlAll,

            // Scope settings
            ScopeCentered = !crcPrefSet.DisplayOffCenter,

            // DCB Location
            DCBLocation = crcPrefSet.DcbLocation,

            // Altitude Filters (CRC uses FL format like 243, DGScope uses feet*100 like 24300)
            AltitudeFilterAssociatedMin = (crcPrefSet.AltitudeFilterAssociated?.Low ?? 0) * 100,
            AltitudeFilterAssociatedMax = (crcPrefSet.AltitudeFilterAssociated?.High ?? 999) * 100,
            AltitudeFilterUnAssociatedMin = (crcPrefSet.AltitudeFilterUnassociated?.Low ?? 0) * 100,
            AltitudeFilterUnAssociatedMax = (crcPrefSet.AltitudeFilterUnassociated?.High ?? 999) * 100,

            // Brightness
            Brightness = new BrightnessSettings
            {
                DCB = crcPrefSet.BrightnessDcb,
                MapA = crcPrefSet.BrightnessMpa,
                MapB = crcPrefSet.BrightnessMpb,
                FullDataBlocks = crcPrefSet.BrightnessFdb,
                Lists = crcPrefSet.BrightnessLst,
                PositionSymbols = crcPrefSet.BrightnessPos,
                LimitedDataBlocks = crcPrefSet.BrightnessLdb,
                OtherFDBs = crcPrefSet.BrightnessOth,
                Tools = crcPrefSet.BrightnessTls,
                RangeRings = crcPrefSet.BrightnessRr,
                Compass = crcPrefSet.BrightnessCmp,
                BeaconTargets = crcPrefSet.BrightnessBcn,
                PrimaryTargets = crcPrefSet.BrightnessPri,
                History = crcPrefSet.BrightnessHst,
                Weather = crcPrefSet.BrightnessWx,
                WeatherContrast = crcPrefSet.BrightnessWxc
            }
        };

        // Clamp brightness values
        settings.Brightness.ClampValues();

        return settings;
    }
}
