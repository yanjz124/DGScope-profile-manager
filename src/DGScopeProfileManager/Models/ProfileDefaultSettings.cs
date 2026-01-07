namespace DGScopeProfileManager.Models;

/// <summary>
/// Default settings template for DGScope profiles
/// </summary>
public class ProfileDefaultSettings
{
    // Brightness Settings
    public string? Brightness { get; set; }

    // Screen Position Settings
    public string? ScreenCenterPoint { get; set; }
    public string? OwnedDataBlockPosition { get; set; }
    public string? PreviewAreaLocation { get; set; }

    // Font Settings
    public string? FontName { get; set; }
    public string? FontSize { get; set; }

    // Display Settings
    public string? ScreenRotation { get; set; }
    public string? BackColor { get; set; }

    // Location Settings
    public string? HomeLatitude { get; set; }
    public string? HomeLongitude { get; set; }

    // Other Settings
    public string? AltimeterStations { get; set; }

    public ProfileDefaultSettings()
    {
        // Initialize with common defaults
        Brightness = "DGScope.STARS.PrefSet+BrightnessSettings";
        ScreenCenterPoint = "0, 0";
        OwnedDataBlockPosition = "N";
        PreviewAreaLocation = "{X=0, Y=0}";
        FontName = "Consolas, 10pt";
        FontSize = "10";
        ScreenRotation = "0";
    }

    /// <summary>
    /// Apply these default settings to a profile's AllSettings dictionary
    /// </summary>
    public void ApplyToProfile(DgScopeProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(Brightness))
            profile.AllSettings["Brightness"] = Brightness;

        if (!string.IsNullOrWhiteSpace(ScreenCenterPoint))
            profile.AllSettings["ScreenCenterPoint"] = ScreenCenterPoint;

        if (!string.IsNullOrWhiteSpace(OwnedDataBlockPosition))
            profile.AllSettings["OwnedDataBlockPosition"] = OwnedDataBlockPosition;

        if (!string.IsNullOrWhiteSpace(PreviewAreaLocation))
            profile.AllSettings["PreviewAreaLocation"] = PreviewAreaLocation;

        if (!string.IsNullOrWhiteSpace(FontName))
        {
            profile.AllSettings["FontName"] = FontName;
            profile.FontName = FontName;
        }

        if (!string.IsNullOrWhiteSpace(FontSize))
        {
            profile.AllSettings["FontSize"] = FontSize;
            if (int.TryParse(FontSize, out var fs))
                profile.FontSize = fs;
        }

        if (!string.IsNullOrWhiteSpace(ScreenRotation))
        {
            profile.AllSettings["ScreenRotation"] = ScreenRotation;
            if (int.TryParse(ScreenRotation, out var sr))
                profile.ScreenRotation = sr;
        }

        if (!string.IsNullOrWhiteSpace(BackColor))
        {
            profile.AllSettings["BackColor"] = BackColor;
            if (int.TryParse(BackColor, out var bc))
                profile.BackColor = bc;
        }

        if (!string.IsNullOrWhiteSpace(HomeLatitude))
            profile.AllSettings["HomeLatitude"] = HomeLatitude;

        if (!string.IsNullOrWhiteSpace(HomeLongitude))
            profile.AllSettings["HomeLongitude"] = HomeLongitude;

        if (!string.IsNullOrWhiteSpace(AltimeterStations))
            profile.AllSettings["AltimeterStations"] = AltimeterStations;
    }
}
