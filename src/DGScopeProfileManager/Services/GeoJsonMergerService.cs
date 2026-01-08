using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Merges multiple GeoJSON files into a single GeoJSON file
/// </summary>
public class GeoJsonMergerService
{
    /// <summary>
    /// Merge multiple GeoJSON files into a single output file
    /// </summary>
    public static bool MergeGeoJsonFiles(List<string> sourceFiles, string outputPath)
    {
        try
        {
            var features = new List<JsonNode>();
            int totalFeatures = 0;

            // Read all GeoJSON files and extract features
            foreach (var sourceFile in sourceFiles)
            {
                if (!File.Exists(sourceFile))
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Source GeoJSON file not found: {sourceFile}");
                    continue;
                }

                try
                {
                    var fileContent = File.ReadAllText(sourceFile);
                    var doc = JsonNode.Parse(fileContent);

                    if (doc == null)
                        continue;

                    var obj = doc.AsObject();

                    // Extract features from this GeoJSON
                    if (obj != null && obj.ContainsKey("features"))
                    {
                        var featuresArray = obj["features"];
                        if (featuresArray != null && featuresArray.GetValueKind() == JsonValueKind.Array)
                        {
                            foreach (var feature in featuresArray.AsArray())
                            {
                                if (feature != null)
                                {
                                    features.Add(feature.DeepClone());
                                    totalFeatures++;
                                }
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"✓ Extracted {totalFeatures} features from {Path.GetFileName(sourceFile)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Error reading GeoJSON file {sourceFile}: {ex.Message}");
                }
            }

            if (features.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("✗ No features found in any GeoJSON file");
                return false;
            }

            // Create merged GeoJSON FeatureCollection
            var mergedGeoJson = new JsonObject
            {
                ["type"] = "FeatureCollection",
                ["features"] = new JsonArray(features.ToArray())
            };

            // Write merged file
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = mergedGeoJson.ToJsonString(options);
            File.WriteAllText(outputPath, jsonString);

            System.Diagnostics.Debug.WriteLine($"✓ Merged {sourceFiles.Count} GeoJSON files into {totalFeatures} features");
            System.Diagnostics.Debug.WriteLine($"✓ Output saved to: {outputPath}");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ Error merging GeoJSON files: {ex.Message}");
            return false;
        }
    }
}
