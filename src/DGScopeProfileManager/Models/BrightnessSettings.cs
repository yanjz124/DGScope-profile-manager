namespace DGScopeProfileManager.Models;

/// <summary>
/// Brightness settings for various display elements (values 0-100)
/// </summary>
public class BrightnessSettings
{
    public int DCB { get; set; } = 100;
    public int Background { get; set; } = 100;
    public int MapA { get; set; } = 100;
    public int MapB { get; set; } = 100;
    public int FullDataBlocks { get; set; } = 100;
    public int Lists { get; set; } = 100;
    public int PositionSymbols { get; set; } = 100;
    public int LimitedDataBlocks { get; set; } = 100;
    public int OtherFDBs { get; set; } = 100;
    public int Tools { get; set; } = 100;
    public int RangeRings { get; set; } = 100;
    public int Compass { get; set; } = 0;
    public int BeaconTargets { get; set; } = 100;
    public int PrimaryTargets { get; set; } = 100;
    public int History { get; set; } = 100;
    public int Weather { get; set; } = 30;
    public int WeatherContrast { get; set; } = 100;

    /// <summary>
    /// Validate that all values are within the valid range (0-100)
    /// </summary>
    public bool Validate(out string error)
    {
        var properties = GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (prop.PropertyType == typeof(int))
            {
                var value = (int?)prop.GetValue(this);
                if (value < 0 || value > 100)
                {
                    error = $"{prop.Name} must be between 0 and 100 (current value: {value})";
                    return false;
                }
            }
        }
        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Clamp all values to the valid range (0-100)
    /// </summary>
    public void ClampValues()
    {
        var properties = GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (prop.PropertyType == typeof(int))
            {
                var value = (int?)prop.GetValue(this) ?? 0;
                var clamped = Math.Max(0, Math.Min(100, value));
                prop.SetValue(this, clamped);
            }
        }
    }
}
