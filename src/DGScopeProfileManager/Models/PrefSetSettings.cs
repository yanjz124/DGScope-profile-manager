namespace DGScopeProfileManager.Models;

/// <summary>
/// Represents the complete DGScope profile settings
/// Includes both RadarWindow-level and CurrentPrefSet settings
/// </summary>
public class PrefSetSettings
{
    // ===== RadarWindow Level Settings =====

    // Font Settings
    public string FontName { get; set; } = "FixedDemiBold";
    public int FontSize { get; set; } = 10;
    public string FontSizeUnit { get; set; } = "Point";
    public string DBCFontName { get; set; } = "Consolas";
    public int DBCFontSize { get; set; } = 10;
    public string DCBFontSizeUnit { get; set; } = "Point";

    // ===== CurrentPrefSet Settings =====

    // Screen Position
    public double ScreenCenterPointLatitude { get; set; } = 40.1967000;
    public double ScreenCenterPointLongitude { get; set; } = -76.7589000;

    // Preview and Status Areas
    public double PreviewAreaLocationX { get; set; } = 0;
    public double PreviewAreaLocationY { get; set; } = 0;
    public double StatusAreaLocationX { get; set; } = 0;
    public double StatusAreaLocationY { get; set; } = 0;

    // Range Rings
    public bool RangeRingsDisplayed { get; set; } = false;
    public double RangeRingLocationLatitude { get; set; } = 40.1967000;
    public double RangeRingLocationLongitude { get; set; } = -76.7589000;
    public int RangeRingSpacing { get; set; } = 5;
    public bool RangeRingsCentered { get; set; } = true;

    // Data Block Settings
    public string DCBLocation { get; set; } = "Top";
    public string OwnedDataBlockPosition { get; set; } = "N";
    public string UnownedDataBlockPosition { get; set; } = "N";
    public string UnassociatedDataBlockPosition { get; set; } = "N";
    public bool DCBVisible { get; set; } = true;

    // Scope Settings
    public bool ScopeCentered { get; set; } = false;
    public int Range { get; set; } = 50;

    // PTL (Predicted Track Line) Settings
    public int PTLLength { get; set; } = 1;
    public bool PTLOwn { get; set; } = true;
    public bool PTLAll { get; set; } = false;

    // History Settings
    public int HistoryNum { get; set; } = 5;
    public double HistoryRate { get; set; } = 4.5;

    // Leader Line Settings
    public int LeaderLength { get; set; } = 1;

    // Altitude Filters
    public int AltitudeFilterAssociatedMax { get; set; } = 99900;
    public int AltitudeFilterAssociatedMin { get; set; } = -9900;
    public int AltitudeFilterUnAssociatedMax { get; set; } = 99900;
    public int AltitudeFilterUnAssociatedMin { get; set; } = -9900;

    // Brightness Settings
    public BrightnessSettings Brightness { get; set; } = new BrightnessSettings();

    /// <summary>
    /// Validate all numeric fields are within acceptable ranges
    /// </summary>
    public bool Validate(out string error)
    {
        // Validate Font Sizes (4-72 typical)
        if (FontSize < 4 || FontSize > 72)
        {
            error = "Font Size must be between 4 and 72";
            return false;
        }

        if (DBCFontSize < 4 || DBCFontSize > 72)
        {
            error = "DBC Font Size must be between 4 and 72";
            return false;
        }

        // Validate Range (1-250 nm typically)
        if (Range < 1 || Range > 250)
        {
            error = "Range must be between 1 and 250";
            return false;
        }

        // Validate HistoryNum (0-10 typical)
        if (HistoryNum < 0 || HistoryNum > 10)
        {
            error = "History Num must be between 0 and 10";
            return false;
        }

        // Validate PTLLength (0-10)
        if (PTLLength < 0 || PTLLength > 10)
        {
            error = "PTL Length must be between 0 and 10";
            return false;
        }

        // Validate LeaderLength (0-10)
        if (LeaderLength < 0 || LeaderLength > 10)
        {
            error = "Leader Length must be between 0 and 10";
            return false;
        }

        // Validate RangeRingSpacing (1-50)
        if (RangeRingSpacing < 1 || RangeRingSpacing > 50)
        {
            error = "Range Ring Spacing must be between 1 and 50";
            return false;
        }

        // Validate Brightness
        if (!Brightness.Validate(out error))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }
}
