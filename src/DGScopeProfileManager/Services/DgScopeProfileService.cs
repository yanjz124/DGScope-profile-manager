using System.IO;
using System.Xml.Linq;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Manages DGScope XML profiles - reading, writing, and batch editing
/// </summary>
public class DgScopeProfileService
{
    private readonly string _dgScopePath;
    
    public DgScopeProfileService(string dgScopePath)
    {
        _dgScopePath = dgScopePath;
    }
    
    /// <summary>
    /// Scans DGScope directory for all XML profiles
    /// </summary>
    public List<DgScopeProfile> ScanProfiles()
    {
        var profiles = new List<DgScopeProfile>();
        
        if (!Directory.Exists(_dgScopePath))
        {
            return profiles;
        }
        
        var xmlFiles = Directory.GetFiles(_dgScopePath, "*.xml", SearchOption.AllDirectories);
        
        foreach (var xmlFile in xmlFiles)
        {
            try
            {
                var profile = LoadProfile(xmlFile);
                profiles.Add(profile);
            }
            catch
            {
                // Skip invalid profiles
            }
        }
        
        return profiles;
    }
    
    /// <summary>
    /// Loads a DGScope profile from XML
    /// </summary>
    public DgScopeProfile LoadProfile(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root;
        
        if (root == null)
        {
            throw new InvalidOperationException($"Invalid XML file: {filePath}");
        }
        
        var profile = new DgScopeProfile
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath
        };
        
        // Parse all XML elements into AllSettings dictionary
        foreach (var element in root.Elements())
        {
            try
            {
                // Skip complex elements
                if (element.HasElements && element.Name != "HomeLocation" && element.Name != "WindowLocation" && element.Name != "WindowSize")
                    continue;
                    
                profile.AllSettings[element.Name.LocalName] = element.Value;
            }
            catch { }
        }
        
        // Parse specific properties for display
        if (int.TryParse(root.Element("BackColor")?.Value ?? "", out int backColor))
            profile.BackColor = backColor;
        if (int.TryParse(root.Element("RangeRingColor")?.Value ?? "", out int rangeRingColor))
            profile.RangeRingColor = rangeRingColor;
        if (int.TryParse(root.Element("VideoMapLineColor")?.Value ?? "", out int videoMapLineColor))
            profile.VideoMapLineColor = videoMapLineColor;
        if (int.TryParse(root.Element("ReturnColor")?.Value ?? "", out int returnColor))
            profile.ReturnColor = returnColor;
        if (int.TryParse(root.Element("BeaconColor")?.Value ?? "", out int beaconColor))
            profile.BeaconColor = beaconColor;
        if (int.TryParse(root.Element("DataBlockColor")?.Value ?? "", out int dataBlockColor))
            profile.DataBlockColor = dataBlockColor;
        
        profile.FontName = root.Element("FontName")?.Value;
        if (int.TryParse(root.Element("FontSize")?.Value ?? "", out int fontSize))
            profile.FontSize = fontSize;
        if (int.TryParse(root.Element("ScreenRotation")?.Value ?? "", out int screenRotation))
            profile.ScreenRotation = screenRotation;
            
        profile.VideoMapFilename = root.Element("VideoMapFilename")?.Value;
        if (!string.IsNullOrEmpty(profile.VideoMapFilename))
        {
            profile.VideoMapPaths.Add(profile.VideoMapFilename);
        }
        
        if (int.TryParse(root.Element("FadeTime")?.Value ?? "", out int fadeTime))
            profile.FadeTime = fadeTime;
        if (int.TryParse(root.Element("LostTargetSeconds")?.Value ?? "", out int lostTargetSeconds))
            profile.LostTargetSeconds = lostTargetSeconds;
        if (int.TryParse(root.Element("AircraftGCInterval")?.Value ?? "", out int aircraftGCInterval))
            profile.AircraftGCInterval = aircraftGCInterval;
            
        if (int.TryParse(root.Element("MaxAltitude")?.Value ?? "", out int maxAlt))
            profile.MaxAltitude = maxAlt;
        if (int.TryParse(root.Element("MinAltitude")?.Value ?? "", out int minAlt))
            profile.MinAltitude = minAlt;
            
        if (bool.TryParse(root.Element("ShowRangeRings")?.Value ?? "", out bool showRangeRings))
            profile.ShowRangeRings = showRangeRings;
        if (bool.TryParse(root.Element("ATPAActive")?.Value ?? "", out bool atpaActive))
            profile.ATPAActive = atpaActive;
            
        profile.WindowState = root.Element("WindowState")?.Value;
        profile.VSync = root.Element("VSync")?.Value;
        if (int.TryParse(root.Element("TargetFrameRate")?.Value ?? "", out int targetFrameRate))
            profile.TargetFrameRate = targetFrameRate;
        
        return profile;
    }
    
    /// <summary>
    /// Saves changes to a DGScope profile
    /// </summary>
    public void SaveProfile(DgScopeProfile profile)
    {
        var doc = XDocument.Load(profile.FilePath);
        var root = doc.Root;
        
        if (root == null)
        {
            throw new InvalidOperationException($"Invalid XML file: {profile.FilePath}");
        }
        
        // Update settings in XML from AllSettings
        foreach (var setting in profile.AllSettings)
        {
            var element = root.Element(setting.Key);
            if (element != null && !element.HasElements)
            {
                element.Value = setting.Value;
            }
        }
        
        // Update specific properties if they were changed
        if (profile.BackColor.HasValue)
        {
            var elem = root.Element("BackColor");
            if (elem != null) elem.Value = profile.BackColor.ToString() ?? "";
        }
        if (profile.FontName != null)
        {
            var elem = root.Element("FontName");
            if (elem != null) elem.Value = profile.FontName;
        }
        if (profile.FontSize.HasValue)
        {
            var elem = root.Element("FontSize");
            if (elem != null) elem.Value = profile.FontSize.ToString() ?? "";
        }
        if (profile.ScreenRotation.HasValue)
        {
            var elem = root.Element("ScreenRotation");
            if (elem != null) elem.Value = profile.ScreenRotation.ToString() ?? "";
        }
            
        // Update video map path if changed
        if (profile.VideoMapPaths.Count > 0)
        {
            var videoMapElement = root.Element("VideoMapFilename");
            if (videoMapElement != null)
            {
                videoMapElement.Value = profile.VideoMapPaths[0];
            }
        }
        
        // Save the modified XML
        doc.Save(profile.FilePath);
    }
    
    /// <summary>
    /// Fixes file paths in a profile (makes them absolute or relative as needed)
    /// </summary>
    public void FixFilePaths(DgScopeProfile profile, bool makeAbsolute = true)
    {
        var updatedPaths = new List<string>();
        
        foreach (var videoMapPath in profile.VideoMapPaths)
        {
            if (string.IsNullOrEmpty(videoMapPath))
            {
                continue;
            }
            
            string fixedPath;
            
            if (makeAbsolute)
            {
                // Convert to absolute path if relative
                if (!Path.IsPathRooted(videoMapPath))
                {
                    var profileDir = Path.GetDirectoryName(profile.FilePath) ?? string.Empty;
                    fixedPath = Path.GetFullPath(Path.Combine(profileDir, videoMapPath));
                }
                else
                {
                    fixedPath = Path.GetFullPath(videoMapPath);
                }
            }
            else
            {
                // Convert to relative path
                var profileDir = Path.GetDirectoryName(profile.FilePath) ?? string.Empty;
                fixedPath = Path.GetRelativePath(profileDir, videoMapPath);
            }
            
            updatedPaths.Add(fixedPath);
        }
        
        profile.VideoMapPaths = updatedPaths;
    }
    
    /// <summary>
    /// Applies batch settings to multiple profiles
    /// </summary>
    public void ApplyBatchSettings(IEnumerable<DgScopeProfile> profiles, Dictionary<string, string> settings)
    {
        foreach (var profile in profiles)
        {
            foreach (var setting in settings)
            {
                profile.AllSettings[setting.Key] = setting.Value;
            }
            SaveProfile(profile);
        }
    }
}
