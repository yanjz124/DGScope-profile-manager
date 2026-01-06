namespace DGScopeProfileManager.Models;

/// <summary>
/// Represents a facility folder containing DGScope profiles
/// </summary>
public class Facility
{
    public string Name { get; set; } = string.Empty;
    public string ArtccCode { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public List<DgScopeProfile> Profiles { get; set; } = new();
    
    public override string ToString() => $"{ArtccCode}/{Name}";
}
