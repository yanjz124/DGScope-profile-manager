using System;
using System.IO;
using System.Linq;
using System.Reflection;

// Load the DLL
var dllPath = @"c:\Users\yanjz\Documents\VSCode Projects\DGScope-profile-manager\src\DGScopeProfileManager\bin\Release\net10.0-windows\DGScopeProfileManager.dll";
var assembly = Assembly.LoadFrom(dllPath);

Console.WriteLine($"Loaded: {assembly.FullName}");
Console.WriteLine($"Version: {assembly.GetName().Version}");
Console.WriteLine();

// Get the CrcProfileReader type
var readerType = assembly.GetType("DGScopeProfileManager.Services.CrcProfileReader");
if (readerType == null)
{
    Console.WriteLine("ERROR: Could not find CrcProfileReader type!");
    return;
}

// Create an instance
var crcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CRC", "ARTCCs");
var reader = Activator.CreateInstance(readerType, crcPath);

Console.WriteLine($"CRC Path: {crcPath}");
Console.WriteLine($"Exists: {Directory.Exists(crcPath)}");
Console.WriteLine();

// Load ZNY profile
var loadProfileMethod = readerType.GetMethod("LoadProfile");
var znyPath = Path.Combine(crcPath, "ZNY.json");
var profile = loadProfileMethod.Invoke(reader, new object[] { znyPath });

// Get properties
var profileType = profile.GetType();
var artccCode = profileType.GetProperty("ArtccCode").GetValue(profile);
var traconsProperty = profileType.GetProperty("Tracons");
var tracons = traconsProperty.GetValue(profile) as System.Collections.IList;

Console.WriteLine($"Profile: {artccCode}");
Console.WriteLine($"TRACONs count: {tracons.Count}");
Console.WriteLine();

if (tracons.Count > 0)
{
    Console.WriteLine("First 5 TRACONs:");
    for (int i = 0; i < Math.Min(5, tracons.Count); i++)
    {
        var tracon = tracons[i];
        var traconType = tracon.GetType();
        var id = traconType.GetProperty("Id").GetValue(tracon);
        var name = traconType.GetProperty("Name").GetValue(tracon);
        var type = traconType.GetProperty("Type").GetValue(tracon);
        Console.WriteLine($"  - {id} ({name}) Type={type}");
    }
}
else
{
    Console.WriteLine("ERROR: No TRACONs found!");
}
