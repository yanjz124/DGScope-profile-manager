using System.IO;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Scans and manages the hierarchical DGScope folder structure (ARTCC/Facility/Profiles)
/// </summary>
public class FacilityScanner
{
    private readonly DgScopeProfileService _profileService;
    
    public FacilityScanner()
    {
        _profileService = new DgScopeProfileService(string.Empty);
    }
    
    /// <summary>
    /// Scans the DGScope root folder and returns organized facilities
    /// Recursively finds all XML files regardless of folder structure
    /// This method will never throw exceptions - it catches all errors
    /// </summary>
    public List<Facility> ScanFacilities(string rootPath)
    {
        var facilities = new Dictionary<string, Facility>();
        
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return new List<Facility>();
        }
        
        try
        {
            if (!Directory.Exists(rootPath))
            {
                return new List<Facility>();
            }
            
            // Recursively find all XML files
            var xmlFiles = new List<string>();
            try
            {
                xmlFiles = Directory.GetFiles(rootPath, "*.xml", SearchOption.AllDirectories).ToList();
            }
            catch
            {
                // If we can't get all files recursively, try without recursion
                try
                {
                    xmlFiles = Directory.GetFiles(rootPath, "*.xml", SearchOption.TopDirectoryOnly).ToList();
                }
                catch
                {
                    // Can't access any files
                    return new List<Facility>();
                }
            }
            
            foreach (var xmlFile in xmlFiles)
            {
                try
                {
                    if (!File.Exists(xmlFile))
                        continue;
                        
                    var fileDir = Path.GetDirectoryName(xmlFile);
                    if (string.IsNullOrWhiteSpace(fileDir))
                        continue;
                        
                    var relativePath = string.Empty;
                    try
                    {
                        relativePath = Path.GetRelativePath(rootPath, fileDir);
                    }
                    catch
                    {
                        relativePath = Path.GetFileName(fileDir);
                    }
                    
                    var pathParts = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // Try to determine ARTCC and Facility from path
                    string artccCode = "Unknown";
                    string facilityName = "Profiles";
                    
                    if (pathParts != null && pathParts.Length > 0)
                    {
                        artccCode = pathParts[0]; // First folder is likely ARTCC
                        facilityName = pathParts.Length > 1 ? pathParts[1] : pathParts[0];
                    }
                    
                    // Create unique key for this facility
                    var facilityKey = $"{artccCode}/{facilityName}";
                    
                    if (!facilities.ContainsKey(facilityKey))
                    {
                        facilities[facilityKey] = new Facility
                        {
                            Name = facilityName ?? "Unknown",
                            ArtccCode = artccCode ?? "Unknown",
                            Path = fileDir,
                            Profiles = new List<DgScopeProfile>()
                        };
                    }
                    
                    // Try to load the profile
                    try
                    {
                        var tempService = new DgScopeProfileService(fileDir);
                        var profile = tempService.LoadProfile(xmlFile);
                        if (profile != null && facilities[facilityKey].Profiles != null)
                        {
                            facilities[facilityKey].Profiles.Add(profile);
                        }
                    }
                    catch
                    {
                        // Skip this XML file - it's not a valid DGScope profile
                    }
                }
                catch
                {
                    // Skip this file completely
                    continue;
                }
            }
        }
        catch
        {
            // If anything goes catastrophically wrong, return what we have
        }
        
        try
        {
            return facilities.Values.Where(f => f?.Profiles != null && f.Profiles.Count > 0).ToList();
        }
        catch
        {
            return new List<Facility>();
        }
    }
}
