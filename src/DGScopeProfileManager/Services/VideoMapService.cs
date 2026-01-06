using System.IO;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Handles copying and managing video maps from CRC to DGScope
/// </summary>
public class VideoMapService
{
    private readonly string _videoMapSourcePath;
    
    public VideoMapService()
    {
        _videoMapSourcePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CRC", "VideoMaps"
        );
    }
    
    public VideoMapService(string customPath)
    {
        _videoMapSourcePath = customPath;
    }
    
    /// <summary>
    /// Gets all available video maps
    /// </summary>
    public List<string> GetAvailableVideoMaps()
    {
        if (!Directory.Exists(_videoMapSourcePath))
        {
            throw new DirectoryNotFoundException($"VideoMaps directory not found: {_videoMapSourcePath}");
        }
        
        return Directory.GetFiles(_videoMapSourcePath, "*.*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();
    }
    
    /// <summary>
    /// Copies selected video maps to destination
    /// </summary>
    public void CopyVideoMaps(IEnumerable<string> mapNames, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);
        
        foreach (var mapName in mapNames)
        {
            var sourcePath = Path.Combine(_videoMapSourcePath, mapName);
            var destPath = Path.Combine(destinationPath, mapName);
            
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destPath, overwrite: true);
            }
        }
    }
}
