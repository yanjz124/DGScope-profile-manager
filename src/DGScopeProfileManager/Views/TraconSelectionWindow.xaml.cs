using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;
using System.Windows;
using System.Windows.Input;

namespace DGScopeProfileManager.Views;

public partial class TraconSelectionWindow : Window
{
    public CrcTracon? SelectedTracon { get; private set; }
    public bool AutoSelectVideoMaps { get; private set; }
    public string? FacilityIdOverride { get; private set; }

    public TraconSelectionWindow(CrcProfile profile, CrcTracon? preselectedTracon = null)
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "TraconSelectionWindow");
        Title = $"Select Facility to generate - {profile.ArtccCode}";
        TraconListBox.ItemsSource = profile.Tracons;

        // Auto-fill facility ID when TRACON selection changes
        TraconListBox.SelectionChanged += (s, e) =>
        {
            if (TraconListBox.SelectedItem is CrcTracon tracon)
            {
                FacilityIdBox.Text = tracon.Id;
            }
        };

        // Preselect the TRACON if specified
        if (preselectedTracon != null && profile.Tracons.Contains(preselectedTracon))
        {
            TraconListBox.SelectedItem = preselectedTracon;
        }
    }

    private void TraconListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click to select and confirm
        Generate_Click(sender, e);
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        SelectedTracon = TraconListBox.SelectedItem as CrcTracon;
        if (SelectedTracon == null)
        {
            MessageBox.Show("Please select a TRACON", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Capture checkbox states
        AutoSelectVideoMaps = AutoSelectVideoMapsCheckBox.IsChecked == true;

        // Capture facility ID override (only if user changed it from default)
        var facilityId = FacilityIdBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(facilityId) && facilityId != SelectedTracon.Id)
        {
            FacilityIdOverride = facilityId;
        }

        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
