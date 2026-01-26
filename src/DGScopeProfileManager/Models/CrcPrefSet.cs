using System.Text.Json.Serialization;

namespace DGScopeProfileManager.Models;

/// <summary>
/// Represents a CRC STARS PrefSet loaded from PrefSets/STARS/{FacilityId}.json
/// Contains display settings like brightness, range, leader direction, selected video maps, etc.
/// </summary>
public class CrcPrefSet
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Version")]
    public int Version { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Range")]
    public double Range { get; set; } = 50;

    [JsonPropertyName("DisplayCenter")]
    public CrcGeoPoint? DisplayCenter { get; set; }

    [JsonPropertyName("DisplayOffCenter")]
    public bool DisplayOffCenter { get; set; }

    [JsonPropertyName("RangeRingSpacing")]
    public int RangeRingSpacing { get; set; } = 5;

    [JsonPropertyName("RangeRingCenter")]
    public CrcGeoPoint? RangeRingCenter { get; set; }

    [JsonPropertyName("RangeRingsOffCenter")]
    public bool RangeRingsOffCenter { get; set; }

    // Leader Directions (N, NE, E, SE, S, SW, W, NW)
    [JsonPropertyName("LeaderDirTracked")]
    public string LeaderDirTracked { get; set; } = "N";

    [JsonPropertyName("LeaderDirAssociated")]
    public string LeaderDirAssociated { get; set; } = "N";

    [JsonPropertyName("LeaderDirUnassociated")]
    public string LeaderDirUnassociated { get; set; } = "N";

    [JsonPropertyName("LeaderLength")]
    public int LeaderLength { get; set; } = 2;

    [JsonPropertyName("HistoryCount")]
    public int HistoryCount { get; set; } = 5;

    [JsonPropertyName("PtlLength")]
    public double PtlLength { get; set; } = 1.0;

    [JsonPropertyName("PtlOwn")]
    public bool PtlOwn { get; set; } = true;

    [JsonPropertyName("PtlAll")]
    public bool PtlAll { get; set; } = true;

    // Brightness Settings (0-100)
    [JsonPropertyName("BrightnessDcb")]
    public int BrightnessDcb { get; set; } = 100;

    [JsonPropertyName("BrightnessMpa")]
    public int BrightnessMpa { get; set; } = 60;

    [JsonPropertyName("BrightnessMpb")]
    public int BrightnessMpb { get; set; } = 25;

    [JsonPropertyName("BrightnessFdb")]
    public int BrightnessFdb { get; set; } = 100;

    [JsonPropertyName("BrightnessLst")]
    public int BrightnessLst { get; set; } = 75;

    [JsonPropertyName("BrightnessPos")]
    public int BrightnessPos { get; set; } = 80;

    [JsonPropertyName("BrightnessLdb")]
    public int BrightnessLdb { get; set; } = 50;

    [JsonPropertyName("BrightnessOth")]
    public int BrightnessOth { get; set; } = 100;

    [JsonPropertyName("BrightnessTls")]
    public int BrightnessTls { get; set; } = 40;

    [JsonPropertyName("BrightnessRr")]
    public int BrightnessRr { get; set; } = 15;

    [JsonPropertyName("BrightnessCmp")]
    public int BrightnessCmp { get; set; } = 0;

    [JsonPropertyName("BrightnessBcn")]
    public int BrightnessBcn { get; set; } = 55;

    [JsonPropertyName("BrightnessPri")]
    public int BrightnessPri { get; set; } = 100;

    [JsonPropertyName("BrightnessHst")]
    public int BrightnessHst { get; set; } = 60;

    [JsonPropertyName("BrightnessWx")]
    public int BrightnessWx { get; set; } = 30;

    [JsonPropertyName("BrightnessWxc")]
    public int BrightnessWxc { get; set; } = 15;

    // Character Sizes
    [JsonPropertyName("CharSizeDataBlocks")]
    public int CharSizeDataBlocks { get; set; } = 2;

    [JsonPropertyName("CharSizeLists")]
    public int CharSizeLists { get; set; } = 2;

    [JsonPropertyName("CharSizeDcb")]
    public int CharSizeDcb { get; set; } = 1;

    [JsonPropertyName("CharSizeTools")]
    public int CharSizeTools { get; set; } = 2;

    [JsonPropertyName("CharSizePositionSymbols")]
    public int CharSizePositionSymbols { get; set; } = 2;

    // Selected Video Map IDs (STARS map numbers that should be displayed)
    [JsonPropertyName("SelectedVideoMapIds")]
    public List<int> SelectedVideoMapIds { get; set; } = new();

    // Altitude Filters
    [JsonPropertyName("AltitudeFilterUnassociated")]
    public CrcAltitudeFilter? AltitudeFilterUnassociated { get; set; }

    [JsonPropertyName("AltitudeFilterAssociated")]
    public CrcAltitudeFilter? AltitudeFilterAssociated { get; set; }

    // DCB Location (Top, Bottom, Left, Right)
    [JsonPropertyName("DcbLocation")]
    public string DcbLocation { get; set; } = "Top";

    public override string ToString() => Name;
}

/// <summary>
/// Geographic point from CRC JSON
/// </summary>
public class CrcGeoPoint
{
    [JsonPropertyName("Lat")]
    public double Lat { get; set; }

    [JsonPropertyName("Lon")]
    public double Lon { get; set; }
}

/// <summary>
/// Altitude filter from CRC JSON
/// Note: CRC uses flight level format (e.g., 243 = FL243 = 24,300 feet)
/// DGScope uses actual feet multiplied by 100 (e.g., 24300)
/// </summary>
public class CrcAltitudeFilter
{
    [JsonPropertyName("Low")]
    public int Low { get; set; }

    [JsonPropertyName("High")]
    public int High { get; set; }

    [JsonPropertyName("IsValid")]
    public bool IsValid { get; set; } = true;
}
