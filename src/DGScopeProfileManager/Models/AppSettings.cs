using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Reflection;

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

    // Window positions
    public Dictionary<string, WindowPosition> WindowPositions { get; set; } = new Dictionary<string, WindowPosition>();

    public AppSettings()
    {
        // Auto-detect CRC root folder from Windows install location
        CrcFolderPath = DetectCrcFolder();

        // Auto-detect DGScope profiles folder (same directory as the executable)
        DgScopeFolderPath = DetectDgScopeProfilesFolder();

        // Auto-detect DGScope executable
        DgScopeExePath = DetectDgScopeExecutable();

        // Load default settings from embedded default.xml
        LoadDefaultSettingsFromResource();
    }

    /// <summary>
    /// Load default settings from the user-editable default.xml file
    /// </summary>
    private void LoadDefaultSettingsFromResource()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DGScopeProfileManager"
            );
            
            Directory.CreateDirectory(appDataPath);
            var defaultXmlPath = Path.Combine(appDataPath, "default.xml");

            // If default.xml doesn't exist, extract from embedded template
            if (!File.Exists(defaultXmlPath))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "DGScopeProfileManager.Resources.DefaultTemplate.xml";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var fileStream = File.Create(defaultXmlPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                        System.Diagnostics.Debug.WriteLine($"Created default.xml at {defaultXmlPath}");
                    }
                }
            }

            // Now load the default.xml
            if (File.Exists(defaultXmlPath))
            {
                var xdoc = XDocument.Load(defaultXmlPath);
                var root = xdoc.Root;

                if (root != null)
                {
                    var defaults = new ProfileDefaultSettings();

                    // Load WindowSize
                    var windowSizeElem = root.Element("WindowSize");
                    if (windowSizeElem != null)
                    {
                        var width = windowSizeElem.Element("Width")?.Value;
                        var height = windowSizeElem.Element("Height")?.Value;
                        if (int.TryParse(width, out var w) && int.TryParse(height, out var h))
                        {
                            defaults.WindowSize = new WindowSize { Width = w, Height = h };
                        }
                    }

                    // Load WindowLocation
                    var windowLocElem = root.Element("WindowLocation");
                    if (windowLocElem != null)
                    {
                        var x = windowLocElem.Element("X")?.Value;
                        var y = windowLocElem.Element("Y")?.Value;
                        if (int.TryParse(x, out var xVal) && int.TryParse(y, out var yVal))
                        {
                            defaults.WindowLocation = new WindowLocation { X = xVal, Y = yVal };
                        }
                    }

                    // Load other default settings
                    var homeLatElem = root.Element("HomeLatitude");
                    if (homeLatElem != null && !string.IsNullOrWhiteSpace(homeLatElem.Value))
                    {
                        defaults.HomeLatitude = homeLatElem.Value;
                    }

                    var homeLonElem = root.Element("HomeLongitude");
                    if (homeLonElem != null && !string.IsNullOrWhiteSpace(homeLonElem.Value))
                    {
                        defaults.HomeLongitude = homeLonElem.Value;
                    }

                    var altimeterElem = root.Element("AltimeterStations");
                    if (altimeterElem != null && !string.IsNullOrWhiteSpace(altimeterElem.Value))
                    {
                        defaults.AltimeterStations = altimeterElem.Value;
                    }

                    DefaultSettings = defaults;
                    System.Diagnostics.Debug.WriteLine($"Loaded default settings from {defaultXmlPath}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading default settings: {ex.Message}");
        }
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
