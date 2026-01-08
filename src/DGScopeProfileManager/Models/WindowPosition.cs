namespace DGScopeProfileManager.Models;

/// <summary>
/// Stores window position and size
/// </summary>
public class WindowPosition
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
}
