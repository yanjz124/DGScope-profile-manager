using System.Windows;
using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

public partial class DefaultSettingsWindow : Window
{
    private readonly AppSettings _appSettings;
    private readonly ProfileDefaultSettings _defaults;

    public DefaultSettingsWindow(AppSettings appSettings)
    {
        InitializeComponent();

        _appSettings = appSettings;
        _defaults = appSettings.DefaultSettings;

        LoadSettings();
    }

    private void LoadSettings()
    {
        BrightnessBox.Text = _defaults.Brightness ?? "";
        ScreenCenterPointBox.Text = _defaults.ScreenCenterPoint ?? "";
        OwnedDataBlockPositionBox.Text = _defaults.OwnedDataBlockPosition ?? "";
        PreviewAreaLocationBox.Text = _defaults.PreviewAreaLocation ?? "";
        FontNameBox.Text = _defaults.FontName ?? "";
        FontSizeBox.Text = _defaults.FontSize ?? "";
        ScreenRotationBox.Text = _defaults.ScreenRotation ?? "";
        BackColorBox.Text = _defaults.BackColor ?? "";
        HomeLatitudeBox.Text = _defaults.HomeLatitude ?? "";
        HomeLongitudeBox.Text = _defaults.HomeLongitude ?? "";
        AltimeterStationsBox.Text = _defaults.AltimeterStations ?? "";
    }

    private void SaveTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update the template
            _defaults.Brightness = BrightnessBox.Text;
            _defaults.ScreenCenterPoint = ScreenCenterPointBox.Text;
            _defaults.OwnedDataBlockPosition = OwnedDataBlockPositionBox.Text;
            _defaults.PreviewAreaLocation = PreviewAreaLocationBox.Text;
            _defaults.FontName = FontNameBox.Text;
            _defaults.FontSize = FontSizeBox.Text;
            _defaults.ScreenRotation = ScreenRotationBox.Text;
            _defaults.BackColor = BackColorBox.Text;
            _defaults.HomeLatitude = HomeLatitudeBox.Text;
            _defaults.HomeLongitude = HomeLongitudeBox.Text;
            _defaults.AltimeterStations = AltimeterStationsBox.Text;

            // Save to persistent storage
            var persistenceService = new SettingsPersistenceService();
            persistenceService.SaveSettings(_appSettings);

            MessageBox.Show("Default settings template saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving template: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyToAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will apply the current template settings to ALL profiles in your DGScope folder.\n\nDo you want to continue?",
            "Apply to All Profiles",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            // First save the template
            SaveTemplate_Click(sender, e);

            // Then apply to all profiles
            if (string.IsNullOrWhiteSpace(_appSettings.DgScopeFolderPath))
            {
                MessageBox.Show("Please configure DGScope folder path in Settings first.",
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var scanner = new FacilityScanner();
            var facilities = scanner.ScanFacilities(_appSettings.DgScopeFolderPath);

            int appliedCount = 0;
            foreach (var facility in facilities)
            {
                var service = new DgScopeProfileService(facility.Path);
                foreach (var profile in facility.Profiles)
                {
                    _defaults.ApplyToProfile(profile);
                    service.SaveProfile(profile);
                    appliedCount++;
                }
            }

            MessageBox.Show($"Successfully applied default settings to {appliedCount} profiles!",
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyToSelected_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // First save the template
            SaveTemplate_Click(sender, e);

            // Show profile selection dialog
            if (string.IsNullOrWhiteSpace(_appSettings.DgScopeFolderPath))
            {
                MessageBox.Show("Please configure DGScope folder path in Settings first.",
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var scanner = new FacilityScanner();
            var facilities = scanner.ScanFacilities(_appSettings.DgScopeFolderPath);

            var selectionWindow = new ProfileSelectionWindow(facilities);
            if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedProfile != null)
            {
                var selectedProfile = selectionWindow.SelectedProfile;
                var facility = facilities.FirstOrDefault(f => f.Profiles.Contains(selectedProfile));

                if (facility != null)
                {
                    var service = new DgScopeProfileService(facility.Path);
                    _defaults.ApplyToProfile(selectedProfile);
                    service.SaveProfile(selectedProfile);

                    MessageBox.Show($"Successfully applied default settings to '{selectedProfile.Name}'!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
