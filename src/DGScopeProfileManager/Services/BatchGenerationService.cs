using System.IO;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Service for batch generation of multiple profiles with progress reporting
/// </summary>
public class BatchGenerationService
{
    private readonly ProfileGeneratorService _generator;

    public BatchGenerationService()
    {
        _generator = new ProfileGeneratorService();
    }

    /// <summary>
    /// Generate multiple profiles asynchronously with progress reporting
    /// </summary>
    public async Task<BatchGenerationResult> GenerateBatchAsync(
        IEnumerable<BatchGenerationItem> items,
        BatchGenerationOptions options,
        IProgress<BatchProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();
        var results = new List<GenerationItemResult>();
        var completed = 0;
        var failed = 0;

        foreach (var item in itemList)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            progress?.Report(new BatchProgressInfo
            {
                CurrentItem = item.DisplayName,
                CurrentIndex = completed,
                TotalCount = itemList.Count,
                Status = BatchStatus.InProgress
            });

            try
            {
                var profile = await Task.Run(() => GenerateSingleProfile(item, options), cancellationToken);
                results.Add(new GenerationItemResult
                {
                    Item = item,
                    Success = true,
                    GeneratedProfile = profile
                });
                completed++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                results.Add(new GenerationItemResult
                {
                    Item = item,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                failed++;
            }
        }

        progress?.Report(new BatchProgressInfo
        {
            CurrentItem = string.Empty,
            CurrentIndex = completed,
            TotalCount = itemList.Count,
            Status = cancellationToken.IsCancellationRequested ? BatchStatus.Cancelled : BatchStatus.Completed
        });

        return new BatchGenerationResult
        {
            TotalGenerated = completed,
            TotalFailed = failed,
            WasCancelled = cancellationToken.IsCancellationRequested,
            Results = results
        };
    }

    private DgScopeProfile? GenerateSingleProfile(BatchGenerationItem item, BatchGenerationOptions options)
    {
        // Determine video maps to use
        List<VideoMapInfo> videoMaps;
        if (options.AutoSelectVideoMaps)
        {
            // Use all available video maps for the TRACON
            // If an area is specified, filter to maps relevant to that area
            videoMaps = item.Tracon.AvailableVideoMaps;
            if (item.Area != null && !string.IsNullOrEmpty(item.Area.MapGroupId))
            {
                var areaMapGroupId = item.Area.MapGroupId;
                var filteredMaps = videoMaps
                    .Where(m => m.ButtonAssignments.Any(a => a.MapGroupId == areaMapGroupId))
                    .ToList();
                if (filteredMaps.Count > 0)
                    videoMaps = filteredMaps;
            }
        }
        else
        {
            videoMaps = item.SelectedVideoMaps ?? item.Tracon.AvailableVideoMaps;
        }

        if (videoMaps.Count == 0)
            return null;

        // Determine output directory
        var outputDir = Path.Combine(options.OutputDirectory, item.CrcProfile.ArtccCode);
        Directory.CreateDirectory(outputDir);

        // Determine profile name
        var profileName = item.ProfileName;
        if (string.IsNullOrWhiteSpace(profileName))
        {
            profileName = item.Area?.Name ?? item.Tracon.Id;
        }

        // Generate the profile
        return _generator.GenerateFromCrcWithMultipleMaps(
            item.CrcProfile,
            outputDir,
            videoMaps,
            options.CrcVideoMapFolder,
            item.Tracon,
            item.Area,
            profileName,
            options.DefaultSettings,
            item.PrefSet,
            item.FacilityIdOverride,
            options.ImportAtpaVolumes,
            options.ImportCaSuppression,
            options.ImportMsawVolumes);
    }
}

/// <summary>
/// Represents a single item to generate in a batch operation
/// </summary>
public class BatchGenerationItem
{
    public required CrcProfile CrcProfile { get; set; }
    public required CrcTracon Tracon { get; set; }
    public CrcArea? Area { get; set; }
    public CrcPrefSet? PrefSet { get; set; }
    public string? ProfileName { get; set; }
    public string? FacilityIdOverride { get; set; }
    public List<VideoMapInfo>? SelectedVideoMaps { get; set; }

    /// <summary>
    /// Display name for progress reporting
    /// </summary>
    public string DisplayName => Area != null
        ? $"{Tracon.Id} - {Area.Name}"
        : Tracon.Id;
}

/// <summary>
/// Options for batch generation
/// </summary>
public class BatchGenerationOptions
{
    public required string OutputDirectory { get; set; }
    public string? CrcVideoMapFolder { get; set; }
    public ProfileDefaultSettings? DefaultSettings { get; set; }
    public bool AutoSelectVideoMaps { get; set; } = true;
    public bool ImportAtpaVolumes { get; set; } = true;
    public bool ImportCaSuppression { get; set; } = true;
    public bool ImportMsawVolumes { get; set; } = false;
}

/// <summary>
/// Progress information for batch generation
/// </summary>
public class BatchProgressInfo
{
    public string CurrentItem { get; set; } = string.Empty;
    public int CurrentIndex { get; set; }
    public int TotalCount { get; set; }
    public BatchStatus Status { get; set; }

    public int PercentComplete => TotalCount > 0 ? (CurrentIndex * 100) / TotalCount : 0;
}

/// <summary>
/// Status of batch generation operation
/// </summary>
public enum BatchStatus
{
    NotStarted,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Result of batch generation operation
/// </summary>
public class BatchGenerationResult
{
    public int TotalGenerated { get; set; }
    public int TotalFailed { get; set; }
    public bool WasCancelled { get; set; }
    public List<GenerationItemResult> Results { get; set; } = new();
}

/// <summary>
/// Result for a single item in batch generation
/// </summary>
public class GenerationItemResult
{
    public required BatchGenerationItem Item { get; set; }
    public bool Success { get; set; }
    public DgScopeProfile? GeneratedProfile { get; set; }
    public string? ErrorMessage { get; set; }
}
