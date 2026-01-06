using DGScopeProfileManager.Services;

// Test the CRC profile reader with actual CRC file
var crcPath = @"C:\Users\yanjz\AppData\Local\CRC\ARTCCs";
var reader = new CrcProfileReader(crcPath);

var zdcProfile = reader.LoadProfile(Path.Combine(crcPath, "ZDC.json"));

Console.WriteLine($"Profile: {zdcProfile.ArtccCode}");
Console.WriteLine($"Home Location: {zdcProfile.HomeLatitude}, {zdcProfile.HomeLongitude}");
Console.WriteLine($"Tracons: {zdcProfile.Tracons.Count}");

var acyTracon = zdcProfile.Tracons.FirstOrDefault(t => t.Id == "ACY");
if (acyTracon != null)
{
    Console.WriteLine($"\nACY TRACON:");
    Console.WriteLine($"  Name: {acyTracon.Name}");
    Console.WriteLine($"  Type: {acyTracon.Type}");
    Console.WriteLine($"  Latitude: {acyTracon.Latitude}");
    Console.WriteLine($"  Longitude: {acyTracon.Longitude}");
    Console.WriteLine($"  Available Video Maps: {acyTracon.AvailableVideoMaps.Count}");
    foreach (var map in acyTracon.AvailableVideoMaps.Take(3))
    {
        Console.WriteLine($"    - {map.SourceFileName} (Tags: {string.Join(", ", map.Tags)})");
    }
}
