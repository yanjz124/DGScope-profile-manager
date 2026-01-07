using DGScopeProfileManager.Models;
using System.Windows;

namespace DGScopeProfileManager.Views;

/// <summary>
/// Window for selecting which video map to use when generating a profile
/// </summary>
public partial class VideoMapSelectionWindow : Window
{
    public VideoMapInfo? SelectedVideoMap { get; private set; }
    public string ProfileName { get; private set; } = string.Empty;
    private bool _isPlaceholder = true;

    public VideoMapSelectionWindow(List<VideoMapInfo> availableVideoMaps, string facilityId)
    {
        InitializeComponent();

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

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (VideoMapsList.SelectedItem is VideoMapDisplay selected)
        {
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

            SelectedVideoMap = selected.VideoMap;
            ProfileName = profileName;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please select a video map.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
        public string SourceFileName => VideoMap.SourceFileName;
        public string TagsDisplay => string.Join(", ", VideoMap.Tags);
        
        public VideoMapDisplay(VideoMapInfo videoMap)
        {
            VideoMap = videoMap;
        }
    }
}
