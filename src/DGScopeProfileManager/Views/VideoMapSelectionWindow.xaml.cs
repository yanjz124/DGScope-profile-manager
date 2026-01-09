using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;
using System.Windows;

namespace DGScopeProfileManager.Views;

/// <summary>
/// Window for selecting which video map to use when generating a profile
/// </summary>
public partial class VideoMapSelectionWindow : Window
{
    public List<VideoMapInfo> SelectedVideoMaps { get; private set; } = new();
    public string ProfileName { get; private set; } = string.Empty;
    private bool _isPlaceholder = true;

    public VideoMapSelectionWindow(List<VideoMapInfo> availableVideoMaps, string facilityId)
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "VideoMapSelectionWindow");

        // Set prefix label - textbox already has "default" placeholder
        ProfilePrefixLabel.Text = $"{facilityId}_";
        _isPlaceholder = true;

        // Add display text property to each video map
        foreach (var map in availableVideoMaps)
        {
            // Create a wrapper with a computed TagsDisplay property
            var displayMap = new VideoMapDisplay(map);
            VideoMapsList.Items.Add(displayMap);
        }

        // Select the first item by default
        if (VideoMapsList.Items.Count > 0)
        {
            VideoMapsList.SelectedIndex = 0;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position window to the right of owner
        if (Owner != null)
        {
            Left = Owner.Left + Owner.Width + 10;
            Top = Owner.Top;
            
            // Make sure window is on screen
            var workingArea = SystemParameters.WorkArea;
            if (Left + Width > workingArea.Right)
            {
                Left = Owner.Left - Width - 10;
            }
            if (Top + Height > workingArea.Bottom)
            {
                Top = workingArea.Bottom - Height;
            }
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        VideoMapsList.SelectAll();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (VideoMapsList.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select at least one video map.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate profile name
        var profileName = ProfileNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(profileName))
        {
            MessageBox.Show("Please enter a profile name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check for invalid filename characters
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        if (profileName.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show("Profile name contains invalid characters.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Collect all selected video maps
        SelectedVideoMaps.Clear();
        foreach (VideoMapDisplay selected in VideoMapsList.SelectedItems)
        {
            SelectedVideoMaps.Add(selected.VideoMap);
        }

        ProfileName = profileName;
        DialogResult = true;
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void ProfileNameBox_GotFocus(object sender, RoutedEventArgs e)
    {
        // Clear placeholder text when user clicks in the box
        if (_isPlaceholder)
        {
            ProfileNameBox.Text = string.Empty;
            ProfileNameBox.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
        }
    }

    private void ProfileNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Restore placeholder if box is empty
        if (string.IsNullOrWhiteSpace(ProfileNameBox.Text))
        {
            ProfileNameBox.Text = "default";
            ProfileNameBox.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#999999"));
            _isPlaceholder = true;
        }
    }

    private void ProfileNameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Once user types anything, it's no longer a placeholder
        if (_isPlaceholder && ProfileNameBox.Text != "default")
        {
            _isPlaceholder = false;
            ProfileNameBox.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
        }
    }

    /// <summary>
    /// Wrapper class to add display properties to VideoMapInfo
    /// </summary>
    private class VideoMapDisplay
    {
        public VideoMapInfo VideoMap { get; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(VideoMap.Name)
                ? VideoMap.Name
                : VideoMap.SourceFileName;

        public string DisplayShortName
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(VideoMap.ShortName))
                    parts.Add($"Short: {VideoMap.ShortName}");
                if (!string.IsNullOrWhiteSpace(VideoMap.StarsId))
                    parts.Add($"Map #{VideoMap.StarsId}");
                return string.Join("  •  ", parts);
            }
        }

        public string DisplayDetails
        {
            get
            {
                var parts = new List<string>();

                // Show DCB button assignment if available
                if (!string.IsNullOrWhiteSpace(VideoMap.DcbButton))
                {
                    var dcbInfo = $"DCB: {VideoMap.DcbButton}";
                    if (VideoMap.DcbButtonPosition.HasValue)
                        dcbInfo += $" (Position {VideoMap.DcbButtonPosition.Value})";
                    parts.Add(dcbInfo);
                }
                else if (VideoMap.DcbButtonPosition.HasValue)
                {
                    parts.Add($"DCB Position: {VideoMap.DcbButtonPosition.Value}");
                }

                if (!string.IsNullOrWhiteSpace(VideoMap.StarsBrightnessCategory))
                    parts.Add($"Brightness: {VideoMap.StarsBrightnessCategory}");
                if (!string.IsNullOrWhiteSpace(VideoMap.SourceFileName))
                    parts.Add($"File: {VideoMap.SourceFileName}");
                return string.Join("  •  ", parts);
            }
        }
        
        public VideoMapDisplay(VideoMapInfo videoMap)
        {
            VideoMap = videoMap;
        }
    }
}
