using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DGScopeProfileManager.Models;

/// <summary>
/// Selectable wrapper for CrcProfile (ARTCC level)
/// </summary>
public class SelectableCrcProfile : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isExpanded = true; // ARTCC level expanded by default
    private bool _updatingFromChildren;

    public CrcProfile Profile { get; }
    public ObservableCollection<SelectableCrcTracon> Tracons { get; }

    public string ArtccCode => Profile.ArtccCode;
    public int TraconCount => Tracons.Count;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();

                // Cascade to children (unless we're updating from children)
                if (!_updatingFromChildren)
                {
                    foreach (var tracon in Tracons)
                    {
                        tracon.SetSelectionFromParent(value);
                    }
                }

                SelectionChanged?.Invoke(this, EventArgs.Empty);
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
    }

    private void OnChildSelectionChanged(object? sender, EventArgs e)
    {
        // Update our state based on children without cascading back down
        _updatingFromChildren = true;
        try
        {
            var allSelected = Tracons.Count > 0 && Tracons.All(t => t.IsSelected);
            var noneSelected = Tracons.All(t => !t.IsSelected && !t.HasSelectedChildren);

            if (allSelected)
                IsSelected = true;
            else if (noneSelected)
                IsSelected = false;
            else
            {
                // Partial selection - just notify without changing our state
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            _updatingFromChildren = false;
        }
    }

    public bool HasSelectedChildren => Tracons.Any(t => t.IsSelected || t.HasSelectedChildren);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SelectionChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Selectable wrapper for CrcTracon (Facility level)
/// </summary>
public class SelectableCrcTracon : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isExpanded; // Facility level collapsed by default
    private bool _updatingFromChildren;
    private bool _updatingFromParent;

    public CrcTracon Tracon { get; }
    public SelectableCrcProfile Parent { get; }
    public ObservableCollection<SelectableCrcArea> Areas { get; }

    public string Id => Tracon.Id;
    public string Name => Tracon.Name;
    public string Type => Tracon.Type;
    public int AreaCount => Areas.Count;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();

                // Cascade to children (unless we're updating from children or parent)
                if (!_updatingFromChildren && !_updatingFromParent)
                {
                    foreach (var area in Areas)
                    {
                        area.SetSelectionFromParent(value);
                    }
                }

                SelectionChanged?.Invoke(this, EventArgs.Empty);
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
    }

    internal void SetSelectionFromParent(bool value)
    {
        _updatingFromParent = true;
        try
        {
            IsSelected = value;
            // Also cascade to children
            foreach (var area in Areas)
            {
                area.SetSelectionFromParent(value);
            }
        }
        finally
        {
            _updatingFromParent = false;
        }
    }

    private void OnChildSelectionChanged(object? sender, EventArgs e)
    {
        // Update our state based on children without cascading back down
        _updatingFromChildren = true;
        try
        {
            var allSelected = Areas.Count > 0 && Areas.All(a => a.IsSelected);
            var noneSelected = Areas.All(a => !a.IsSelected);

            if (allSelected)
                IsSelected = true;
            else if (noneSelected)
                IsSelected = false;
            else
            {
                // Partial selection - just notify without changing our state
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            _updatingFromChildren = false;
        }
    }

    public bool HasSelectedChildren => Areas.Any(a => a.IsSelected);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SelectionChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Selectable wrapper for CrcArea (Area/PrefSet level)
/// </summary>
public class SelectableCrcArea : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _updatingFromParent;

    public CrcArea Area { get; }
    public SelectableCrcTracon Parent { get; }

    public string Id => Area.Id;
    public string Name => Area.Name;
    public string AirportsDisplay => Area.AirportsDisplay;

    /// <summary>
    /// Display name for selection summary
    /// </summary>
    public string DisplayName => $"{Parent.Id} - {Name}";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();

                if (!_updatingFromParent)
                {
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    public SelectableCrcArea(CrcArea area, SelectableCrcTracon parent)
    {
        Area = area;
        Parent = parent;
    }

    internal void SetSelectionFromParent(bool value)
    {
        _updatingFromParent = true;
        try
        {
            IsSelected = value;
        }
        finally
        {
            _updatingFromParent = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SelectionChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
