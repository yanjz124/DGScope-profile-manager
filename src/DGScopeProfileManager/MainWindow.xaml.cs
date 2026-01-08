using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;
using DGScopeProfileManager.Views;

namespace DGScopeProfileManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private AppSettings _settings;
    private CrcProfileReader? _crcReader;
    private FacilityScanner _facilityScanner;
    private SettingsPersistenceService _persistenceService;
    private List<CrcProfile> _crcProfiles = new();
    private List<Facility> _facilities = new();
    
    public MainWindow()
    {
        InitializeComponent();
        _persistenceService = new SettingsPersistenceService();
        _settings = _persistenceService.LoadSettings();
        _facilityScanner = new FacilityScanner();

        // Initialize window position tracking
        WindowPositionService.InitializePositionTracking(this, _settings, "MainWindow");

        // Auto-detect DGScope.exe if not configured
        if (string.IsNullOrWhiteSpace(_settings.DgScopeExePath))
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var localScope = Path.Combine(appDir, "scope", "scope.exe");
            if (File.Exists(localScope))
            {
                _settings.DgScopeExePath = localScope;
                _persistenceService.SaveSettings(_settings);
            }
        }

        // Initialize with empty lists
        CrcProfilesList.ItemsSource = _crcProfiles;
        FacilitiesTree.ItemsSource = _facilities;

        // Disable buttons initially
        GenerateButton.IsEnabled = false;
        EditProfileButton.IsEnabled = false;
        DeleteProfileButton.IsEnabled = false;
        LaunchDGScopeButton.IsEnabled = false;

        UpdateStatus("Ready. Click Settings to configure paths, then Scan Folders to load profiles.");

        // Auto-refresh on launch if paths are configured
        if (!string.IsNullOrWhiteSpace(_settings.CrcFolderPath) ||
            !string.IsNullOrWhiteSpace(_settings.DgScopeFolderPath))
        {
            Loaded += (s, e) => LoadFolders();
        }
    }
    
    private async void LoadFolders()
    {
        try
        {
            UpdateStatus("Scanning folders...");
            
            _crcProfiles.Clear();
            _facilities.Clear();
            
            // Force UI update
            CrcProfilesList.ItemsSource = null;
            FacilitiesTree.ItemsSource = null;
            
            int crcCount = 0;
            int profileCount = 0;
            
            // Run scanning on background thread
            await Task.Run(() =>
            {
                // Load CRC profiles
                if (!string.IsNullOrWhiteSpace(_settings.CrcFolderPath) && 
                    !string.IsNullOrWhiteSpace(_settings.CrcArtccFolderPath) && 
                    Directory.Exists(_settings.CrcArtccFolderPath))
                {
                    try
                    {
                        _crcReader = new CrcProfileReader(_settings.CrcArtccFolderPath);
                        _crcProfiles = _crcReader.GetAllProfiles() ?? new List<CrcProfile>();
                        crcCount = _crcProfiles.Count;
                    }
                    catch
                    {
                        // Ignore CRC scan errors
                    }
                }
                
                // Load DGScope facilities
                if (!string.IsNullOrWhiteSpace(_settings.DgScopeFolderPath) && 
                    Directory.Exists(_settings.DgScopeFolderPath))
                {
                    try
                    {
                        _facilities = _facilityScanner.ScanFacilities(_settings.DgScopeFolderPath) ?? new List<Facility>();
                        profileCount = _facilities.Sum(f => f.Profiles?.Count ?? 0);
                    }
                    catch
                    {
                        // Ignore DGScope scan errors
                    }
                }
            });
            
            // Refresh UI bindings on UI thread
            CrcProfilesList.ItemsSource = _crcProfiles;
            FacilitiesTree.ItemsSource = _facilities;
            
            if (crcCount == 0 && profileCount == 0)
            {
                UpdateStatus("No profiles found. Check that paths are correct.");
            }
            else
            {
                UpdateStatus($"Loaded {crcCount} CRC profiles and {profileCount} DGScope profiles from {_facilities.Count} locations");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error scanning folders");
            MessageBox.Show($"Error scanning folders:\n\n{ex.Message}\n\nCheck that paths are correct and accessible.", "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings);
        if (settingsWindow.ShowDialog() == true)
        {
            _settings = settingsWindow.Settings;
            _persistenceService.SaveSettings(_settings);
            LoadFolders();
        }
    }
    
    private void CrcProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GenerateButton.IsEnabled = CrcProfilesList.SelectedItem != null;
    }
    private void RefreshAll_Click(object sender, RoutedEventArgs e)
    {
        LoadFolders();
    }

    private void DefaultSettings_Click(object sender, RoutedEventArgs e)
    {
        var prefSetSettings = _settings.DefaultSettings.ToPrefSetSettings();
        var unifiedWindow = new UnifiedSettingsWindow(_settings, prefSetSettings);
        if (unifiedWindow.ShowDialog() == true)
        {
            _settings.DefaultSettings.UpdateFromPrefSetSettings(prefSetSettings);
            UpdateStatus("Default settings updated");
        }
    }
    
    private void FixAllPaths_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show(
                "This will fix all video map paths in all profiles to use absolute paths.\n\nContinue?",
                "Fix All Paths",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                int fixedCount = 0;
                foreach (var facility in _facilities)
                {
                    var service = new DgScopeProfileService(facility.Path);
                    foreach (var profile in facility.Profiles)
                    {
                        service.FixFilePaths(profile, makeAbsolute: true);
                        fixedCount++;
                    }
                }

                UpdateStatus($"Fixed paths in {fixedCount} profiles");
                MessageBox.Show($"Successfully fixed paths in {fixedCount} profiles", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error fixing paths: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void CrcProfilesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click to generate profile
        GenerateProfile_Click(sender, e);
    }

    private void GenerateProfile_Click(object sender, RoutedEventArgs e)
    {
        if (CrcProfilesList.SelectedItem is not CrcProfile selectedCrc)
        {
            MessageBox.Show("Please select an ARTCC profile first.", 
                "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (selectedCrc.Tracons.Count == 0)
        {
            MessageBox.Show($"No TRACONs found in {selectedCrc.ArtccCode} profile.", 
                "No TRACONs", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            if (string.IsNullOrEmpty(_settings.DgScopeFolderPath))
            {
                MessageBox.Show("Please configure DGScope folder path in Settings first.", 
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Show TRACON selection window
            var traconWindow = new TraconSelectionWindow(selectedCrc);
            if (traconWindow.ShowDialog() != true)
                return;

            var selectedTracon = traconWindow.SelectedTracon;
            if (selectedTracon == null)
                return;

            // If TRACON has multiple areas, show area selection window
            CrcArea? selectedArea = null;
            if (selectedTracon.Areas.Count > 1)
            {
                var areaWindow = new AreaSelectionWindow(selectedTracon.Areas);
                if (areaWindow.ShowDialog() != true)
                    return;

                if (areaWindow.SelectedArea != null)
                {
                    selectedArea = areaWindow.SelectedArea;
                }
                else
                {
                    return;
                }
            }
            else if (selectedTracon.Areas.Count == 1)
            {
                // Only one area, use it automatically
                selectedArea = selectedTracon.Areas[0];
            }

            // Show video map selection window
            if (selectedTracon.AvailableVideoMaps.Count == 0)
            {
                MessageBox.Show($"No video maps available for {selectedTracon.Name}.",
                    "No Video Maps", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var videoMapWindow = new VideoMapSelectionWindow(selectedTracon.AvailableVideoMaps, selectedTracon.Id);
            if (videoMapWindow.ShowDialog() != true)
                return;

            var selectedVideoMap = videoMapWindow.SelectedVideoMap;
            var profileName = videoMapWindow.ProfileName;

            // Generate profile under profiles/ARTCC directory
            // All settings are now automatically configured from CRC data
            var outputDir = Path.Combine(_settings.DgScopeFolderPath, "profiles", selectedCrc.ArtccCode);
            Directory.CreateDirectory(outputDir);

            var generator = new ProfileGeneratorService();

            var profile = generator.GenerateFromCrc(
                selectedCrc,
                outputDir,
                selectedTracon,
                selectedVideoMap,
                _settings.CrcVideoMapFolderPath,
                selectedArea,
                profileName);
            
            if (profile != null)
            {
                UpdateStatus($"Generated profile: {profile.Name}");
                MessageBox.Show($"Profile generated successfully:\n{profile.Name}\n\nPath: {outputDir}", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                // Refresh the tree
                LoadFolders();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating profile: {ex.Message}\n\n{ex.StackTrace}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    
    private void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (FacilitiesTree.SelectedItem is DgScopeProfile profile)
        {
            var facility = _facilities.FirstOrDefault(f => f.Profiles.Contains(profile));
            if (facility != null)
            {
                // Load profile settings
                var profileSettings = profile.LoadPrefSetSettings();
                
                // Open unified settings window in profile mode
                var editor = new UnifiedSettingsWindow(_settings, profile, profileSettings);
                if (editor.ShowDialog() == true)
                {
                    // Settings were saved in the dialog
                    UpdateStatus($"Profile {profile.Name} updated");
                }
                
                // Refresh display
                FacilitiesTree.Items.Refresh();
            }
        }
    }
    
    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (FacilitiesTree.SelectedItem is DgScopeProfile profile)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete the profile '{profile.Name}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(profile.FilePath);
                    LoadFolders();
                    UpdateStatus($"Deleted profile: {profile.Name}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting profile: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    
    private void CrcProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        GenerateButton.IsEnabled = CrcProfilesList.SelectedItem != null;
    }
    
    private void FacilityTree_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var selectedProfile = FacilitiesTree.SelectedItem as DgScopeProfile;
        EditProfileButton.IsEnabled = selectedProfile != null;
        DeleteProfileButton.IsEnabled = selectedProfile != null;
        LaunchDGScopeButton.IsEnabled = selectedProfile != null;
    }
    
    private void LaunchDGScope_Click(object sender, RoutedEventArgs e)
    {
        var selectedProfile = FacilitiesTree.SelectedItem as DgScopeProfile;
        if (selectedProfile == null)
        {
            MessageBox.Show("Please select a profile first.", "No Profile Selected", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Check if DGScope path is configured
        if (string.IsNullOrWhiteSpace(_settings.DgScopeExePath))
        {
            MessageBox.Show(
                "DGScope executable path is not configured.\n\nPlease go to Settings and set the path to DGScope.exe",
                "DGScope Not Configured",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Check if DGScope executable exists
        if (!File.Exists(_settings.DgScopeExePath))
        {
            var result = MessageBox.Show(
                $"DGScope executable not found at:\n{_settings.DgScopeExePath}\n\nWould you like to update the path in Settings?",
                "DGScope Not Found",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                Settings_Click(sender, e);
            }
            return;
        }

        try
        {
            // Launch DGScope with the selected profile as command-line argument
            var startInfo = new ProcessStartInfo
            {
                FileName = _settings.DgScopeExePath,
                Arguments = $"\"{selectedProfile.FilePath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(_settings.DgScopeExePath)
            };

            Process.Start(startInfo);
            UpdateStatus($"Launched DGScope with profile: {selectedProfile.Name}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to launch DGScope:\n\n{ex.Message}",
                "Launch Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "DGScope Profile Manager\nVersion 1.0\n\nManage DGScope profiles and import from CRC data.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void UpdateStatus(string message)
    {
        StatusText.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
    }
    
    
}