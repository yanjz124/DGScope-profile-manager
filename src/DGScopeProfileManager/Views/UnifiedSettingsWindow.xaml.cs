using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

/// <summary>
/// Unified settings window that operates in two modes:
/// 1. Default Settings Mode - Edit the default template and apply to profiles
/// 2. Profile Settings Mode - Edit an individual profile's settings
/// </summary>
public partial class UnifiedSettingsWindow : Window
{
    private readonly AppSettings _appSettings;
    private readonly PrefSetSettings _settings;
    private readonly DgScopeProfile? _profile;

    /// <summary>
    /// Constructor for Default Settings Mode
    /// </summary>
    public UnifiedSettingsWindow(AppSettings appSettings, PrefSetSettings defaultSettings)
    {
        InitializeComponent();

        _appSettings = appSettings;
        _settings = defaultSettings;
        _profile = null;

        // Initialize window position tracking
        WindowPositionService.InitializePositionTracking(this, appSettings, "UnifiedSettingsWindow");

        PopulateFontDropdowns();
        ConfigureForDefaultMode();
        LoadSettings();
    }

    /// <summary>
    /// Constructor for Profile Settings Mode
    /// </summary>
    public UnifiedSettingsWindow(AppSettings appSettings, DgScopeProfile profile, PrefSetSettings profileSettings)
    {
        InitializeComponent();

        _appSettings = appSettings;
        _settings = profileSettings;
        _profile = profile;

        // Initialize window position tracking
        WindowPositionService.InitializePositionTracking(this, appSettings, "UnifiedSettingsWindow");

        PopulateFontDropdowns();
        ConfigureForProfileMode();
        LoadSettings();
        LoadNexradStations();
    }

    /// <summary>
    /// Populate font dropdowns with installed system fonts
    /// </summary>
    private void PopulateFontDropdowns()
    {
        var fonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).Select(f => f.Source).ToList();

        FontNameBox.ItemsSource = fonts;
        DCBFontNameBox.ItemsSource = fonts;
    }

    /// <summary>
    /// Numeric-only validation for brightness textboxes (0-100)
    /// </summary>
    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow only digits
        e.Handled = !e.Text.All(char.IsDigit);

        // Additional check: ensure value will be between 0-100
        if (!e.Handled && sender is System.Windows.Controls.TextBox textBox)
        {
            var proposedText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            if (int.TryParse(proposedText, out var value))
            {
                if (value < 0 || value > 100)
                {
                    e.Handled = true;
                }
            }
        }
    }

    // Numeric spinner button handlers
    private void IncrementTextBox(System.Windows.Controls.TextBox textBox, int min = 0, int max = 100)
    {
        if (int.TryParse(textBox.Text, out var value))
        {
            if (value < max)
            {
                textBox.Text = (value + 1).ToString();
            }
        }
    }

    private void DecrementTextBox(System.Windows.Controls.TextBox textBox, int min = 0, int max = 100)
    {
        if (int.TryParse(textBox.Text, out var value))
        {
            if (value > min)
            {
                textBox.Text = (value - 1).ToString();
            }
        }
    }

    // Font Size spinners
    private void FontSizeUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(FontSizeBox, 1, 72);
    private void FontSizeDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(FontSizeBox, 1, 72);
    private void DCBFontSizeUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(DCBFontSizeBox, 1, 72);
    private void DCBFontSizeDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(DCBFontSizeBox, 1, 72);

    // Brightness spinners
    private void BrightnessDCBUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessDCBBox);
    private void BrightnessDCBDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessDCBBox);
    private void BrightnessBackgroundUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessBackgroundBox);
    private void BrightnessBackgroundDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessBackgroundBox);
    private void BrightnessMapAUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessMapABox);
    private void BrightnessMapADown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessMapABox);
    private void BrightnessMapBUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessMapBBox);
    private void BrightnessMapBDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessMapBBox);
    private void BrightnessFullDataBlocksUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessFullDataBlocksBox);
    private void BrightnessFullDataBlocksDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessFullDataBlocksBox);
    private void BrightnessListsUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessListsBox);
    private void BrightnessListsDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessListsBox);
    private void BrightnessPositionSymbolsUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessPositionSymbolsBox);
    private void BrightnessPositionSymbolsDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessPositionSymbolsBox);
    private void BrightnessLimitedDataBlocksUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessLimitedDataBlocksBox);
    private void BrightnessLimitedDataBlocksDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessLimitedDataBlocksBox);
    private void BrightnessOtherFDBsUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessOtherFDBsBox);
    private void BrightnessOtherFDBsDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessOtherFDBsBox);
    private void BrightnessToolsUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessToolsBox);
    private void BrightnessToolsDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessToolsBox);
    private void BrightnessRangeRingsUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessRangeRingsBox);
    private void BrightnessRangeRingsDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessRangeRingsBox);
    private void BrightnessCompassUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessCompassBox);
    private void BrightnessCompassDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessCompassBox);
    private void BrightnessBeaconTargetsUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessBeaconTargetsBox);
    private void BrightnessBeaconTargetsDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessBeaconTargetsBox);
    private void BrightnessPrimaryTargetsUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessPrimaryTargetsBox);
    private void BrightnessPrimaryTargetsDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessPrimaryTargetsBox);
    private void BrightnessHistoryUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessHistoryBox);
    private void BrightnessHistoryDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessHistoryBox);
    private void BrightnessWeatherUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessWeatherBox);
    private void BrightnessWeatherDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessWeatherBox);
    private void BrightnessWeatherContrastUp_Click(object sender, RoutedEventArgs e) => IncrementTextBox(BrightnessWeatherContrastBox);
    private void BrightnessWeatherContrastDown_Click(object sender, RoutedEventArgs e) => DecrementTextBox(BrightnessWeatherContrastBox);
    // Video Map File Browser
    private void BrowseVideoMap_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Video Map File",
            Filter = "GeoJSON Files (*.geojson)|*.geojson|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        // Set initial directory to CRC VideoMaps folder if available
        if (!string.IsNullOrWhiteSpace(_appSettings.CrcVideoMapFolderPath) && 
            System.IO.Directory.Exists(_appSettings.CrcVideoMapFolderPath))
        {
            dialog.InitialDirectory = _appSettings.CrcVideoMapFolderPath;
        }

        if (dialog.ShowDialog() == true)
        {
            VideoMapPathBox.Text = dialog.FileName;
        }
    }


    private void ConfigureForDefaultMode()
    {
        Title = "DGScope Default Settings";
        InstructionsText.Text =
            "Edit the default settings below. These values will be used when generating new profiles or applying defaults to existing profiles.";

        // Show default mode buttons
        SaveDefaultButton.Visibility = Visibility.Visible;
        ApplyToAllButton.Visibility = Visibility.Visible;

        // Hide profile mode buttons
        SaveProfileButton.Visibility = Visibility.Collapsed;
        SaveAsDefaultButton.Visibility = Visibility.Collapsed;
        SaveAsDefaultApplyAllButton.Visibility = Visibility.Collapsed;

        // Hide reset button (only for profile mode)
        ResetToHomeLocationButton.Visibility = Visibility.Collapsed;

        // Hide NEXRAD dropdown (only for profile mode)
        NexradPanel.Visibility = Visibility.Collapsed;
    }

    private void ConfigureForProfileMode()
    {
        Title = $"Profile Settings - {_profile?.Name}";
        InstructionsText.Text =
            $"Edit settings for profile '{_profile?.Name}'. You can save to this profile only, or save as the default template and optionally apply to all profiles.";
        // Show profile information panel
        ProfileInfoPanel.Visibility = Visibility.Visible;
        if (_profile != null)
        {
            ProfileNameText.Text = _profile.Name;
            ProfileFilePathText.Text = _profile.FilePath;
            VideoMapPathBox.Text = _profile.VideoMapFilename ?? string.Empty;
        }


        // Hide default mode buttons
        SaveDefaultButton.Visibility = Visibility.Collapsed;
        ApplyToAllButton.Visibility = Visibility.Collapsed;

        // Show reset button (only for profile mode)
        ResetToHomeLocationButton.Visibility = Visibility.Visible;

        // Show NEXRAD dropdown (only for profile mode)
        NexradPanel.Visibility = Visibility.Visible;

        // Show profile mode buttons
        SaveProfileButton.Visibility = Visibility.Visible;
        SaveAsDefaultButton.Visibility = Visibility.Visible;
        SaveAsDefaultApplyAllButton.Visibility = Visibility.Visible;
    }

    private void LoadNexradStations()
    {
        if (_profile == null)
            return;

        try
        {
            // Load NEXRAD stations
            var nexradService = new NexradService();
            var nexradPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nexrad-stations.txt");
            nexradService.LoadStations(nexradPath);

            // Get profile center location
            var centerLat = _profile.HomeLocationLatitude ?? 0;
            var centerLon = _profile.HomeLocationLongitude ?? 0;

            // If home location is not set, use screen center point
            if (centerLat == 0 && centerLon == 0)
            {
                centerLat = _settings.ScreenCenterPointLatitude;
                centerLon = _settings.ScreenCenterPointLongitude;
            }

            // Get all stations with distances
            var stationsWithDistance = nexradService.GetAllStationsWithDistance(centerLat, centerLon);

            // Create display items
            var nexradItems = stationsWithDistance.Select(item => new NexradDisplayItem
            {
                Station = item.Station,
                Distance = item.Distance,
                DisplayText = $"{item.Station.Icao} - {item.Station.Name} ({item.Distance:F1} NM)"
            }).ToList();

            NexradComboBox.ItemsSource = nexradItems;
            NexradComboBox.DisplayMemberPath = "DisplayText";

            // Select current NEXRAD if it exists in the profile
            // This would require reading the NEXRAD from the profile
            // For now, select the closest one
            if (nexradItems.Any())
            {
                NexradComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading NEXRAD stations: {ex.Message}");
        }
    }

    // Helper class for NEXRAD dropdown display
    private class NexradDisplayItem
    {
        public NexradStation Station { get; set; } = null!;
        public double Distance { get; set; }
        public string DisplayText { get; set; } = string.Empty;
    }

    private void LoadSettings()
    {
        // Font Settings
        FontNameBox.Text = _settings.FontName;
        FontSizeBox.Text = _settings.FontSize.ToString();
        DCBFontNameBox.Text = _settings.DCBFontName;
        DCBFontSizeBox.Text = _settings.DCBFontSize.ToString();

        // Screen Position
        ScreenCenterLatBox.Text = _settings.ScreenCenterPointLatitude.ToString("F6");
        ScreenCenterLonBox.Text = _settings.ScreenCenterPointLongitude.ToString("F6");

        // Brightness Settings
        BrightnessDCBBox.Text = _settings.Brightness.DCB.ToString();
        BrightnessBackgroundBox.Text = _settings.Brightness.Background.ToString();
        BrightnessMapABox.Text = _settings.Brightness.MapA.ToString();
        BrightnessMapBBox.Text = _settings.Brightness.MapB.ToString();
        BrightnessFullDataBlocksBox.Text = _settings.Brightness.FullDataBlocks.ToString();
        BrightnessListsBox.Text = _settings.Brightness.Lists.ToString();
        BrightnessPositionSymbolsBox.Text = _settings.Brightness.PositionSymbols.ToString();
        BrightnessLimitedDataBlocksBox.Text = _settings.Brightness.LimitedDataBlocks.ToString();
        BrightnessOtherFDBsBox.Text = _settings.Brightness.OtherFDBs.ToString();
        BrightnessToolsBox.Text = _settings.Brightness.Tools.ToString();
        BrightnessRangeRingsBox.Text = _settings.Brightness.RangeRings.ToString();
        BrightnessCompassBox.Text = _settings.Brightness.Compass.ToString();
        BrightnessBeaconTargetsBox.Text = _settings.Brightness.BeaconTargets.ToString();
        BrightnessPrimaryTargetsBox.Text = _settings.Brightness.PrimaryTargets.ToString();
        BrightnessHistoryBox.Text = _settings.Brightness.History.ToString();
        BrightnessWeatherBox.Text = _settings.Brightness.Weather.ToString();
        BrightnessWeatherContrastBox.Text = _settings.Brightness.WeatherContrast.ToString();

        // Range and Scope
        RangeBox.Text = _settings.Range.ToString();
        ScopeCenteredCheck.IsChecked = _settings.ScopeCentered;
        RangeRingsDisplayedCheck.IsChecked = _settings.RangeRingsDisplayed;
        RangeRingsCenteredCheck.IsChecked = _settings.RangeRingsCentered;

        // Range Ring Details
        RangeRingSpacingBox.Text = _settings.RangeRingSpacing.ToString();
        RangeRingLatBox.Text = _settings.RangeRingLocationLatitude.ToString("F6");
        RangeRingLonBox.Text = _settings.RangeRingLocationLongitude.ToString("F6");

        // Data Block Settings
        DCBLocationBox.Text = _settings.DCBLocation;
        DCBVisibleCheck.IsChecked = _settings.DCBVisible;
        OwnedDataBlockPosBox.Text = _settings.OwnedDataBlockPosition;
        UnownedDataBlockPosBox.Text = _settings.UnownedDataBlockPosition;
        UnassociatedDataBlockPosBox.Text = _settings.UnassociatedDataBlockPosition;

        // PTL and History
        PTLLengthBox.Text = _settings.PTLLength.ToString();
        LeaderLengthBox.Text = _settings.LeaderLength.ToString();
        PTLOwnCheck.IsChecked = _settings.PTLOwn;
        PTLAllCheck.IsChecked = _settings.PTLAll;
        HistoryNumBox.Text = _settings.HistoryNum.ToString();
        HistoryRateBox.Text = _settings.HistoryRate.ToString("F1");

        // Altitude Filters
        AltFilterAssocMaxBox.Text = _settings.AltitudeFilterAssociatedMax.ToString();
        AltFilterAssocMinBox.Text = _settings.AltitudeFilterAssociatedMin.ToString();
        AltFilterUnassocMaxBox.Text = _settings.AltitudeFilterUnAssociatedMax.ToString();
        AltFilterUnassocMinBox.Text = _settings.AltitudeFilterUnAssociatedMin.ToString();

        // Preview and Status Areas
        PreviewAreaXBox.Text = _settings.PreviewAreaLocationX.ToString("F6");
        PreviewAreaYBox.Text = _settings.PreviewAreaLocationY.ToString("F6");
        StatusAreaXBox.Text = _settings.StatusAreaLocationX.ToString("F6");
        StatusAreaYBox.Text = _settings.StatusAreaLocationY.ToString("F6");
    }

    private bool SaveSettingsFromUI()
    {
        try
        {
            // Font Settings
            _settings.FontName = FontNameBox.Text;
            _settings.FontSize = int.Parse(FontSizeBox.Text);
            _settings.DCBFontName = DCBFontNameBox.Text;
            _settings.DCBFontSize = int.Parse(DCBFontSizeBox.Text);

            // Screen Position
            _settings.ScreenCenterPointLatitude = double.Parse(ScreenCenterLatBox.Text);
            _settings.ScreenCenterPointLongitude = double.Parse(ScreenCenterLonBox.Text);

            // Brightness Settings
            _settings.Brightness.DCB = int.Parse(BrightnessDCBBox.Text);
            _settings.Brightness.Background = int.Parse(BrightnessBackgroundBox.Text);
            _settings.Brightness.MapA = int.Parse(BrightnessMapABox.Text);
            _settings.Brightness.MapB = int.Parse(BrightnessMapBBox.Text);
            _settings.Brightness.FullDataBlocks = int.Parse(BrightnessFullDataBlocksBox.Text);
            _settings.Brightness.Lists = int.Parse(BrightnessListsBox.Text);
            _settings.Brightness.PositionSymbols = int.Parse(BrightnessPositionSymbolsBox.Text);
            _settings.Brightness.LimitedDataBlocks = int.Parse(BrightnessLimitedDataBlocksBox.Text);
            _settings.Brightness.OtherFDBs = int.Parse(BrightnessOtherFDBsBox.Text);
            _settings.Brightness.Tools = int.Parse(BrightnessToolsBox.Text);
            _settings.Brightness.RangeRings = int.Parse(BrightnessRangeRingsBox.Text);
            _settings.Brightness.Compass = int.Parse(BrightnessCompassBox.Text);
            _settings.Brightness.BeaconTargets = int.Parse(BrightnessBeaconTargetsBox.Text);
            _settings.Brightness.PrimaryTargets = int.Parse(BrightnessPrimaryTargetsBox.Text);
            _settings.Brightness.History = int.Parse(BrightnessHistoryBox.Text);
            _settings.Brightness.Weather = int.Parse(BrightnessWeatherBox.Text);
            _settings.Brightness.WeatherContrast = int.Parse(BrightnessWeatherContrastBox.Text);

            // Range and Scope
            _settings.Range = int.Parse(RangeBox.Text);
            _settings.ScopeCentered = ScopeCenteredCheck.IsChecked ?? false;
            _settings.RangeRingsDisplayed = RangeRingsDisplayedCheck.IsChecked ?? false;
            _settings.RangeRingsCentered = RangeRingsCenteredCheck.IsChecked ?? false;

            // Range Ring Details
            _settings.RangeRingSpacing = int.Parse(RangeRingSpacingBox.Text);
            _settings.RangeRingLocationLatitude = double.Parse(RangeRingLatBox.Text);
            _settings.RangeRingLocationLongitude = double.Parse(RangeRingLonBox.Text);

            // Data Block Settings
            _settings.DCBLocation = DCBLocationBox.Text;
            _settings.DCBVisible = DCBVisibleCheck.IsChecked ?? false;
            _settings.OwnedDataBlockPosition = OwnedDataBlockPosBox.Text;
            _settings.UnownedDataBlockPosition = UnownedDataBlockPosBox.Text;
            _settings.UnassociatedDataBlockPosition = UnassociatedDataBlockPosBox.Text;

            // PTL and History
            _settings.PTLLength = int.Parse(PTLLengthBox.Text);
            _settings.LeaderLength = int.Parse(LeaderLengthBox.Text);
            _settings.PTLOwn = PTLOwnCheck.IsChecked ?? false;
            _settings.PTLAll = PTLAllCheck.IsChecked ?? false;
            _settings.HistoryNum = int.Parse(HistoryNumBox.Text);
            _settings.HistoryRate = double.Parse(HistoryRateBox.Text);

            // Altitude Filters
            _settings.AltitudeFilterAssociatedMax = int.Parse(AltFilterAssocMaxBox.Text);
            _settings.AltitudeFilterAssociatedMin = int.Parse(AltFilterAssocMinBox.Text);
            _settings.AltitudeFilterUnAssociatedMax = int.Parse(AltFilterUnassocMaxBox.Text);
            _settings.AltitudeFilterUnAssociatedMin = int.Parse(AltFilterUnassocMinBox.Text);

            // Preview and Status Areas
            _settings.PreviewAreaLocationX = double.Parse(PreviewAreaXBox.Text);
            _settings.PreviewAreaLocationY = double.Parse(PreviewAreaYBox.Text);
            _settings.StatusAreaLocationX = double.Parse(StatusAreaXBox.Text);
            _settings.StatusAreaLocationY = double.Parse(StatusAreaYBox.Text);

            // Validate
            if (!_settings.Validate(out string error))
            {
                MessageBox.Show(error, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error parsing settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    // ===== Default Mode Buttons =====

    private void SaveDefault_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveSettingsFromUI())
            return;

        try
        {
            // Update the app settings default template with ALL settings from _settings
            _appSettings.DefaultSettings.UpdateFromPrefSetSettings(_settings);

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
            "This will apply the current settings to ALL profiles in your DGScope folder.\n\nDo you want to continue?",
            "Apply to All Profiles",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        if (!SaveSettingsFromUI())
            return;

        try
        {
            // First persist these settings as the default template (without closing the window)
            _appSettings.DefaultSettings.UpdateFromPrefSetSettings(_settings);
            var persistenceService = new SettingsPersistenceService();
            persistenceService.SaveSettings(_appSettings);

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
                    service.ApplyPrefSetSettings(profile, _settings);
                    appliedCount++;
                }
            }

            MessageBox.Show($"Successfully applied settings to {appliedCount} profiles!",
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===== Profile Mode Buttons =====

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveSettingsFromUI())
            return;

        try
        {
            if (_profile == null)
                return;

            // Update profile's CurrentPrefSet
            _profile.CurrentPrefSet = _settings;

            // Update video map path if changed
            if (!string.IsNullOrWhiteSpace(VideoMapPathBox.Text))
            {
                _profile.VideoMapFilename = VideoMapPathBox.Text;
            }

            // Save profile to XML (writes CurrentPrefSet subtree and root fonts; also updates VideoMapFilename)
            var profileService = new DgScopeProfileService(_appSettings.DgScopeFolderPath);
            profileService.ApplyPrefSetSettings(_profile, _settings);

            MessageBox.Show($"Profile settings saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving profile: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveAsDefault_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveSettingsFromUI())
            return;

        try
        {
            // Update the app settings default template with ALL settings from _settings
            _appSettings.DefaultSettings.UpdateFromPrefSetSettings(_settings);

            // Save to persistent storage
            var persistenceService = new SettingsPersistenceService();
            persistenceService.SaveSettings(_appSettings);

            MessageBox.Show("Settings saved as default template!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving as default: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveAsDefaultApplyAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will save these settings as the default template AND apply them to ALL profiles.\n\nDo you want to continue?",
            "Save as Default and Apply to All",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        if (!SaveSettingsFromUI())
            return;

        try
        {
            // Persist defaults
            _appSettings.DefaultSettings.UpdateFromPrefSetSettings(_settings);
            var persistenceService = new SettingsPersistenceService();
            persistenceService.SaveSettings(_appSettings);

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
                    service.ApplyPrefSetSettings(profile, _settings);
                    appliedCount++;
                }
            }

            MessageBox.Show($"Saved defaults and applied to {appliedCount} profiles!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving and applying settings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetToHomeLocation_Click(object sender, RoutedEventArgs e)
    {
        // Only available in profile mode
        if (_profile == null)
        {
            MessageBox.Show("Reset to home location is only available when editing a profile.",
                "Not Available", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Check if profile has home location coordinates
        if (!_profile.HomeLocationLatitude.HasValue || !_profile.HomeLocationLongitude.HasValue)
        {
            MessageBox.Show("This profile does not have a home location defined.",
                "No Home Location", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Reset screen center to home location
        ScreenCenterLatBox.Text = _profile.HomeLocationLatitude.Value.ToString("F7");
        ScreenCenterLonBox.Text = _profile.HomeLocationLongitude.Value.ToString("F7");

        MessageBox.Show($"Screen center reset to airport/radar center:\nLat: {_profile.HomeLocationLatitude.Value:F7}\nLon: {_profile.HomeLocationLongitude.Value:F7}",
            "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
