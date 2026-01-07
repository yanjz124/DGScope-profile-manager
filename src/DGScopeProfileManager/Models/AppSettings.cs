using System.IO;

namespace DGScopeProfileManager.Models;

/// <summary>
/// Application settings for folder paths and preferences
/// </summary>
public class AppSettings
{
    public string CrcFolderPath { get; set; } = string.Empty;
    public string DgScopeFolderPath { get; set; } = string.Empty;
    public string DgScopeExePath { get; set; } = string.Empty;

    // Computed properties for CRC subfolders
    public string CrcArtccFolderPath => Path.Combine(CrcFolderPath, "ARTCCs");
    public string CrcVideoMapFolderPath => Path.Combine(CrcFolderPath, "VideoMaps");

    // Default settings template
    public ProfileDefaultSettings DefaultSettings { get; set; } = new ProfileDefaultSettings();

    public AppSettings()
    {
        // Set default to CRC root folder
        CrcFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CRC"
        );
    }
}
