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
        // Auto-detect CRC root folder from Windows install location
        CrcFolderPath = DetectCrcFolder();

        // Auto-detect DGScope profiles folder (same directory as the executable)
        DgScopeFolderPath = DetectDgScopeProfilesFolder();

        // Auto-detect DGScope executable
        DgScopeExePath = DetectDgScopeExecutable();
    }

    private static string DetectCrcFolder()
    {
        // Try standard CRC installation location
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CRC"
        );

        if (Directory.Exists(defaultPath))
        {
            return defaultPath;
        }

        return defaultPath;
    }

    private static string DetectDgScopeProfilesFolder()
    {
        // DGScope profiles folder should be in the same directory as the Profile Manager executable
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var profilesPath = Path.Combine(exeDirectory, "..", "profiles");

        // Normalize the path
        profilesPath = Path.GetFullPath(profilesPath);

        if (Directory.Exists(profilesPath))
        {
            return profilesPath;
        }

        // Fallback: create it next to the exe
        return profilesPath;
    }

    private static string DetectDgScopeExecutable()
    {
        // DGScope executable should be in ../scope/scope.exe relative to the Profile Manager
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var scopeExePath = Path.Combine(exeDirectory, "..", "scope", "scope.exe");

        // Normalize the path
        scopeExePath = Path.GetFullPath(scopeExePath);

        if (File.Exists(scopeExePath))
        {
            return scopeExePath;
        }

        return string.Empty;
    }
}
