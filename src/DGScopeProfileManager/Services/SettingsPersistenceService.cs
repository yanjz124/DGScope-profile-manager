using System.IO;
using System.Text.Json;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Persists application settings to disk
/// </summary>
public class SettingsPersistenceService
{
    private readonly string _settingsPath;
    private const string SettingsFileName = "settings.json";
    
    public SettingsPersistenceService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DGScopeProfileManager"
        );
        
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, SettingsFileName);
    }
    
    /// <summary>
    /// Load settings from disk, or return defaults if file doesn't exist
    /// </summary>
    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }
        
        try
        {
            var json = File.ReadAllText(_settingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            
            if (data != null)
            {
                var settings = new AppSettings
                {
                    CrcFolderPath = data.CrcFolderPath,
                    DgScopeFolderPath = data.DgScopeFolderPath
                };

                // Load default settings if present
                if (data.DefaultSettings != null)
                {
                    settings.DefaultSettings = data.DefaultSettings;
                }

                return settings;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
        }
        
        return new AppSettings();
    }
    
    /// <summary>
    /// Save settings to disk
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var data = new SettingsData
            {
                CrcFolderPath = settings.CrcFolderPath,
                DgScopeFolderPath = settings.DgScopeFolderPath,
                DefaultSettings = settings.DefaultSettings
            };
            
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
    
    private class SettingsData
    {
        public string CrcFolderPath { get; set; } = string.Empty;
        public string DgScopeFolderPath { get; set; } = string.Empty;
        public ProfileDefaultSettings? DefaultSettings { get; set; }
    }
}
