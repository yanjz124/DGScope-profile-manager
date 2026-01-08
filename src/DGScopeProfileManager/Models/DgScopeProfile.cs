namespace DGScopeProfileManager.Models;

/// <summary>
/// Represents a DGScope profile (XML-based)
/// </summary>
public class DgScopeProfile
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    
    // Display settings
    public string? FontName { get; set; }
    public int? FontSize { get; set; }
    public int? ScreenRotation { get; set; }
    public string? VideoMapFilename { get; set; }
    
    // Colors
    public int? BackColor { get; set; }
    public int? RangeRingColor { get; set; }
    public int? VideoMapLineColor { get; set; }
    public int? ReturnColor { get; set; }
    public int? BeaconColor { get; set; }
    public int? DataBlockColor { get; set; }
    
    // Timing
    public int? FadeTime { get; set; }
    public int? LostTargetSeconds { get; set; }
    public int? AircraftGCInterval { get; set; }
    
    // Altitude settings
    public int? MaxAltitude { get; set; }
    public int? MinAltitude { get; set; }

    // General settings
    public bool? ShowRangeRings { get; set; }
    public string? WindowState { get; set; }
    public int? TargetFrameRate { get; set; }
    public string? VSync { get; set; }
    public bool? ATPAActive { get; set; }

    // CurrentPrefSet settings
    public PrefSetSettings? CurrentPrefSet { get; set; }

    // All settings as key-value pairs for preservation
    public Dictionary<string, string> AllSettings { get; set; } = new();
    public List<string> VideoMapPaths { get; set; } = new();

    // Home Location coordinates (radar/airport center)
    public double? HomeLocationLatitude { get; set; }
    public double? HomeLocationLongitude { get; set; }

    public override string ToString() => Name;

    /// <summary>
    /// Load PrefSetSettings from the profile, or create new with default values
    /// </summary>
    public PrefSetSettings LoadPrefSetSettings()
    {
        // If CurrentPrefSet exists, return it
        if (CurrentPrefSet != null)
            return CurrentPrefSet;

        // Otherwise create a new PrefSetSettings with current values
        var settings = new PrefSetSettings();

        // Copy basic settings from profile
        if (!string.IsNullOrWhiteSpace(FontName))
            settings.FontName = FontName;
        
        if (FontSize.HasValue)
            settings.FontSize = FontSize.Value;

        return settings;
    }
}
