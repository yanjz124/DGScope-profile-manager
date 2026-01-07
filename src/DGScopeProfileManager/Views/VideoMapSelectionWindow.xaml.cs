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

    public VideoMapSelectionWindow(List<VideoMapInfo> availableVideoMaps, string facilityId)
    {
        InitializeComponent();

        // Set prefix label and clear textbox (facility ID will be added as prefix automatically)
        ProfilePrefixLabel.Text = $"{facilityId}_";
        ProfileNameBox.Text = string.Empty;

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
