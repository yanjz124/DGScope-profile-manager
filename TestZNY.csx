#r "src/DGScopeProfileManager/bin/Release/net10.0-windows/DGScopeProfileManager.dll"

using DGScopeProfileManager.Services;
using System;
using System.IO;
using System.Linq;

var crcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CRC", "ARTCCs");
var znyPath = Path.Combine(crcPath, "ZNY.json");

Console.WriteLine($"Loading ZNY from: {znyPath}");
Console.WriteLine($"File exists: {File.Exists(znyPath)}");

var reader = new CrcProfileReader(crcPath);
var znyProfile = reader.LoadProfile(znyPath);

Console.WriteLine($"\nProfile: {znyProfile.ArtccCode}");
Console.WriteLine($"TRACONs count: {znyProfile.Tracons.Count}");
Console.WriteLine($"\nFirst 5 TRACONs:");
foreach (var tracon in znyProfile.Tracons.Take(5))
{
    Console.WriteLine($"  - {tracon.Id} ({tracon.Name}) Type={tracon.Type} IsControlled={tracon.IsControlledFacility()}");
}
