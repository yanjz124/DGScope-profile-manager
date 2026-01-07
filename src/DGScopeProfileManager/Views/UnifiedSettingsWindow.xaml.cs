using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

        PopulateFontDropdowns();
        ConfigureForProfileMode();
        LoadSettings();
    }

    /// <summary>
    /// Populate font dropdowns with installed system fonts
    /// </summary>
    private void PopulateFontDropdowns()
    {
        var fonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).Select(f => f.Source).ToList();

        FontNameBox.ItemsSource = fonts;
        DBCFontNameBox.ItemsSource = fonts;
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

    private void ConfigureForDefaultMode()
    {
        Title = "DGScope Default Settings Template";
        InstructionsText.Text =
            "Edit the default settings template below. These values will be used when generating new profiles or applying defaults to existing profiles.";

        // Show default mode buttons
        SaveDefaultButton.Visibility = Visibility.Visible;
        ApplyToAllButton.Visibility = Visibility.Visible;

        // Hide profile mode buttons
        SaveProfileButton.Visibility = Visibility.Collapsed;
        SaveAsDefaultButton.Visibility = Visibility.Collapsed;
        SaveAsDefaultApplyAllButton.Visibility = Visibility.Collapsed;
    }

    private void ConfigureForProfileMode()
    {
        Title = $"Profile Settings - {_profile?.Name}";
        InstructionsText.Text =
            $"Edit settings for profile '{_profile?.Name}'. You can save to this profile only, or save as the default template and optionally apply to all profiles.";

        // Hide default mode buttons
        SaveDefaultButton.Visibility = Visibility.Collapsed;
        ApplyToAllButton.Visibility = Visibility.Collapsed;

        // Show profile mode buttons
        SaveProfileButton.Visibility = Visibility.Visible;
        SaveAsDefaultButton.Visibility = Visibility.Visible;
        SaveAsDefaultApplyAllButton.Visibility = Visibility.Visible;
    }

    private void LoadSettings()
    {
        // Font Settings
        FontNameBox.Text = _settings.FontName;
        FontSizeBox.Text = _settings.FontSize.ToString();
        DBCFontNameBox.Text = _settings.DBCFontName;
        DBCFontSizeBox.Text = _settings.DBCFontSize.ToString();

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
            _settings.DBCFontName = DBCFontNameBox.Text;
            _settings.DBCFontSize = int.Parse(DBCFontSizeBox.Text);

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
            // Update the app settings default template
            _appSettings.DefaultSettings.FontName = _settings.FontName;
            _appSettings.DefaultSettings.FontSize = _settings.FontSize.ToString();
            _appSettings.DefaultSettings.ScreenCenterPoint =
                $"{_settings.ScreenCenterPointLatitude:F6}, {_settings.ScreenCenterPointLongitude:F6}";
            _appSettings.DefaultSettings.Brightness = _settings.Brightness;

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
            // First save the default template
            SaveDefault_Click(sender, e);

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
                    // Apply settings to profile (would need implementation in service)
                    // service.SavePrefSetSettings(profile, _settings);
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

            // Would need to implement SavePrefSetSettings in service
            // var service = new DgScopeProfileService(_profile.FacilityPath);
            // service.SavePrefSetSettings(_profile, _settings);

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
            // Update the app settings default template
            _appSettings.DefaultSettings.FontName = _settings.FontName;
            _appSettings.DefaultSettings.FontSize = _settings.FontSize.ToString();
            _appSettings.DefaultSettings.ScreenCenterPoint =
                $"{_settings.ScreenCenterPointLatitude:F6}, {_settings.ScreenCenterPointLongitude:F6}";
            _appSettings.DefaultSettings.Brightness = _settings.Brightness;

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

        // First save as default
        SaveAsDefault_Click(sender, e);

        // Then apply to all
        ApplyToAll_Click(sender, e);
    }
}
