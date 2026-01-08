namespace DGScopeProfileManager.Models;

/// <summary>
/// Default settings template for DGScope profiles
/// </summary>
public class ProfileDefaultSettings
{
    // Complete snapshot of PrefSet settings
    public PrefSetSettings PrefSet { get; set; } = new PrefSetSettings();
    // Brightness Settings (detailed)
    public BrightnessSettings Brightness { get; set; } = new BrightnessSettings();

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

    // Window Settings
    public WindowSize? WindowSize { get; set; }
    public WindowLocation? WindowLocation { get; set; }

    public ProfileDefaultSettings()
    {
        // Initialize with common defaults and sync legacy fields
        Brightness = PrefSet.Brightness;
        ScreenCenterPoint = $"{PrefSet.ScreenCenterPointLatitude:F6}, {PrefSet.ScreenCenterPointLongitude:F6}";
        OwnedDataBlockPosition = PrefSet.OwnedDataBlockPosition;
        PreviewAreaLocation = $"{{X={PrefSet.PreviewAreaLocationX:F6}, Y={PrefSet.PreviewAreaLocationY:F6}}}";
        FontName = PrefSet.FontName;
        FontSize = PrefSet.FontSize.ToString();
        ScreenRotation = "0";
    }

    /// <summary>
    /// Convert to PrefSetSettings for use in the unified settings window
    /// </summary>
    public PrefSetSettings ToPrefSetSettings()
    {
        // Prefer the full snapshot when available
        if (PrefSet != null)
        {
            return PrefSet;
        }

        var settings = new PrefSetSettings
        {
            Brightness = this.Brightness
        };

        // Parse font settings - remove any size suffix like ", 10pt"
        if (!string.IsNullOrWhiteSpace(FontName))
        {
            var fontName = FontName;
            // Remove size suffix if present (e.g., "Consolas, 10pt" -> "Consolas")
            var commaIndex = fontName.IndexOf(',');
            if (commaIndex > 0)
                fontName = fontName.Substring(0, commaIndex).Trim();
            settings.FontName = fontName;
        }

        if (int.TryParse(FontSize, out var fontSize))
            settings.FontSize = fontSize;

        // Parse screen center point
        if (!string.IsNullOrWhiteSpace(ScreenCenterPoint))
        {
            var parts = ScreenCenterPoint.Split(',');
            if (parts.Length == 2)
            {
                if (double.TryParse(parts[0].Trim(), out var lat))
                    settings.ScreenCenterPointLatitude = lat;
                if (double.TryParse(parts[1].Trim(), out var lon))
                    settings.ScreenCenterPointLongitude = lon;
            }
        }

        if (!string.IsNullOrWhiteSpace(OwnedDataBlockPosition))
            settings.OwnedDataBlockPosition = OwnedDataBlockPosition;

        return settings;
    }

    /// <summary>
    /// Update from PrefSetSettings after editing
    /// </summary>
    public void UpdateFromPrefSetSettings(PrefSetSettings settings)
    {
        // Store full snapshot
        PrefSet = settings;

        // Keep legacy string fields in sync for backward compatibility
        Brightness = settings.Brightness;
        FontName = settings.FontName;
        FontSize = settings.FontSize.ToString();
        ScreenCenterPoint = $"{settings.ScreenCenterPointLatitude:F6}, {settings.ScreenCenterPointLongitude:F6}";
        OwnedDataBlockPosition = settings.OwnedDataBlockPosition;
        PreviewAreaLocation = $"{{X={settings.PreviewAreaLocationX:F6}, Y={settings.PreviewAreaLocationY:F6}}}";
    }

    /// <summary>
    /// Apply these default settings to a profile
    /// Note: Brightness is handled separately via Brightness XML element
    /// </summary>
    public void ApplyToProfile(DgScopeProfile profile)
    {
        // Brightness is handled via the Brightness property, not AllSettings

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
