// Quick test to verify CRC location parsing
using System.Text.Json;

var json = File.ReadAllText(@"C:\Users\yanjz\AppData\Local\CRC\ARTCCs\ZDC.json");
var doc = JsonDocument.Parse(json);
var root = doc.RootElement;

// Get facility -> childFacilities -> ACY
var facility = root.GetProperty("facility");
var childFacilities = facility.GetProperty("childFacilities");

foreach (var child in childFacilities.EnumerateArray())
{
    if (child.TryGetProperty("id", out var id) && id.GetString() == "ACY")
    {
        Console.WriteLine("Found ACY facility");
        
        // Get starsConfiguration
        if (child.TryGetProperty("starsConfiguration", out var starsConfig))
        {
            Console.WriteLine("  Has starsConfiguration");
            
            // Get areas
            if (starsConfig.TryGetProperty("areas", out var areas))
            {
                var areasArray = areas.EnumerateArray().ToList();
                Console.WriteLine($"  Areas count: {areasArray.Count}");
                
                if (areasArray.Count > 0)
                {
                    var firstArea = areasArray[0];
                    
                    // Get visibilityCenter
                    if (firstArea.TryGetProperty("visibilityCenter", out var visCenter))
                    {
                        Console.WriteLine($"  visibilityCenter ValueKind: {visCenter.ValueKind}");
                        
                        if (visCenter.ValueKind == JsonValueKind.Object)
                        {
                            Console.WriteLine("    It's an object!");
                            if (visCenter.TryGetProperty("lat", out var lat) && lat.TryGetDouble(out var latVal))
                                Console.WriteLine($"    lat: {latVal}");
                            if (visCenter.TryGetProperty("lon", out var lon) && lon.TryGetDouble(out var lonVal))
                                Console.WriteLine($"    lon: {lonVal}");
                        }
                    }
                }
            }
            
            // Get videoMapIds
            if (starsConfig.TryGetProperty("videoMapIds", out var videoMapIds))
            {
                var mapIds = videoMapIds.EnumerateArray().Select(m => m.GetString()).ToList();
                Console.WriteLine($"  Video Map IDs count: {mapIds.Count}");
                foreach (var mapId in mapIds.Take(3))
                {
                    Console.WriteLine($"    - {mapId}");
                }
            }
        }
        
        break;
    }
}
