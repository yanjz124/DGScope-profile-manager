using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DGScopeProfileManager.Models;

/// <summary>
/// Base class for selectable tree items with checkbox support
/// </summary>
public abstract class SelectableTreeItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isExpanded;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                OnSelectionChanged();
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SelectionChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Selectable wrapper for CrcProfile (ARTCC level)
/// </summary>
public class SelectableCrcProfile : SelectableTreeItem
{
    public CrcProfile Profile { get; }
    public ObservableCollection<SelectableCrcTracon> Tracons { get; }

    public string ArtccCode => Profile.ArtccCode;
    public int TraconCount => Tracons.Count;

    public SelectableCrcProfile(CrcProfile profile)
    {
        Profile = profile;
        Tracons = new ObservableCollection<SelectableCrcTracon>();

        foreach (var tracon in profile.Tracons)
        {
            var selectableTracon = new SelectableCrcTracon(tracon, this);
            selectableTracon.SelectionChanged += OnChildSelectionChanged;
            Tracons.Add(selectableTracon);
        }

        // ARTCC level expanded by default
        IsExpanded = true;
    }

    protected override void OnSelectionChanged()
    {
        // Cascade selection to all children
        foreach (var tracon in Tracons)
        {
            tracon.SetSelectionWithoutCascade(IsSelected);
        }
        base.OnSelectionChanged();
    }

    private void OnChildSelectionChanged(object? sender, EventArgs e)
    {
        // Update parent selection state based on children
        // Don't trigger cascade back down
        var allSelected = Tracons.All(t => t.IsSelected);
        var anySelected = Tracons.Any(t => t.IsSelected || t.HasSelectedChildren);

        if (allSelected)
            SetSelectionWithoutCascade(true);
        else if (!anySelected)
            SetSelectionWithoutCascade(false);

        // Notify parent (MainWindow) of selection change
        base.OnSelectionChanged();
    }

    internal void SetSelectionWithoutCascade(bool value)
    {
        if (_isSelectedInternal != value)
        {
            _isSelectedInternal = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    private bool _isSelectedInternal;
    public new bool IsSelected
    {
        get => _isSelectedInternal;
        set
        {
            if (_isSelectedInternal != value)
            {
                _isSelectedInternal = value;
                OnPropertyChanged();
                OnSelectionChanged();
            }
        }
    }

    /// <summary>
    /// Check if any descendant is selected
    /// </summary>
    public bool HasSelectedChildren => Tracons.Any(t => t.IsSelected || t.HasSelectedChildren);
}

/// <summary>
/// Selectable wrapper for CrcTracon (Facility level)
/// </summary>
public class SelectableCrcTracon : SelectableTreeItem
{
    public CrcTracon Tracon { get; }
    public SelectableCrcProfile Parent { get; }
    public ObservableCollection<SelectableCrcArea> Areas { get; }

    public string Id => Tracon.Id;
    public string Name => Tracon.Name;
    public string Type => Tracon.Type;
    public int AreaCount => Areas.Count;

    public SelectableCrcTracon(CrcTracon tracon, SelectableCrcProfile parent)
    {
        Tracon = tracon;
        Parent = parent;
        Areas = new ObservableCollection<SelectableCrcArea>();

        foreach (var area in tracon.Areas)
        {
            var selectableArea = new SelectableCrcArea(area, this);
            selectableArea.SelectionChanged += OnChildSelectionChanged;
            Areas.Add(selectableArea);
        }

        // Facility level collapsed by default
        IsExpanded = false;
    }

    protected override void OnSelectionChanged()
    {
        // Cascade selection to all children
        foreach (var area in Areas)
        {
            area.SetSelectionWithoutCascade(IsSelected);
        }
        base.OnSelectionChanged();
    }

    private void OnChildSelectionChanged(object? sender, EventArgs e)
    {
        // Update selection state based on children
        var allSelected = Areas.Count > 0 && Areas.All(a => a.IsSelected);
        var anySelected = Areas.Any(a => a.IsSelected);

        if (allSelected)
            SetSelectionWithoutCascade(true);
        else if (!anySelected)
            SetSelectionWithoutCascade(false);

        // Notify parent
        base.OnSelectionChanged();
    }

    internal void SetSelectionWithoutCascade(bool value)
    {
        if (_isSelectedInternal != value)
        {
            _isSelectedInternal = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    private bool _isSelectedInternal;
    public new bool IsSelected
    {
        get => _isSelectedInternal;
        set
        {
            if (_isSelectedInternal != value)
            {
                _isSelectedInternal = value;
                OnPropertyChanged();
                OnSelectionChanged();
            }
        }
    }

    /// <summary>
    /// Check if any child area is selected
    /// </summary>
    public bool HasSelectedChildren => Areas.Any(a => a.IsSelected);
}

/// <summary>
/// Selectable wrapper for CrcArea (Area/PrefSet level)
/// </summary>
public class SelectableCrcArea : SelectableTreeItem
{
    public CrcArea Area { get; }
    public SelectableCrcTracon Parent { get; }

    public string Id => Area.Id;
    public string Name => Area.Name;
    public string AirportsDisplay => Area.AirportsDisplay;

    /// <summary>
    /// Display name for selection summary
    /// </summary>
    public string DisplayName => $"{Parent.Id} - {Name}";

    public SelectableCrcArea(CrcArea area, SelectableCrcTracon parent)
    {
        Area = area;
        Parent = parent;
    }

    internal void SetSelectionWithoutCascade(bool value)
    {
        if (_isSelectedInternal != value)
        {
            _isSelectedInternal = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    private bool _isSelectedInternal;
    public new bool IsSelected
    {
        get => _isSelectedInternal;
        set
        {
            if (_isSelectedInternal != value)
            {
                _isSelectedInternal = value;
                OnPropertyChanged();
                OnSelectionChanged();
            }
        }
    }
}

/// <summary>
/// Wrapper for VideoMapInfo with selection support for the configuration panel
/// </summary>
public class SelectableVideoMap : INotifyPropertyChanged
{
    private bool _isSelected;

    public VideoMapInfo VideoMap { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public string DisplayName => !string.IsNullOrWhiteSpace(VideoMap.Name)
        ? VideoMap.Name
        : VideoMap.SourceFileName;

    public string ShortName => VideoMap.ShortName ?? string.Empty;

    public string Details
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(VideoMap.StarsId))
                parts.Add($"Map #{VideoMap.StarsId}");
            if (!string.IsNullOrWhiteSpace(VideoMap.DcbButton))
                parts.Add($"DCB: {VideoMap.DcbButton}");
            if (!string.IsNullOrWhiteSpace(VideoMap.StarsBrightnessCategory))
                parts.Add($"Brightness: {VideoMap.StarsBrightnessCategory}");
            return string.Join(" | ", parts);
        }
    }

    public SelectableVideoMap(VideoMapInfo videoMap)
    {
        VideoMap = videoMap;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
