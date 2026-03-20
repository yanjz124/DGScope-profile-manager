using System.Collections.ObjectModel;
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

    // Raw CRC profiles (for internal use)
    private List<CrcProfile> _crcProfiles = new();

    // Selectable wrappers for checkbox TreeView
    private ObservableCollection<SelectableCrcProfile> _selectableProfiles = new();

    private List<Facility> _facilities = new();

    // Current selection tracking
    private List<SelectableCrcArea> _selectedAreas = new();
    private SelectableCrcTracon? _currentTracon;
    private SelectableCrcProfile? _currentProfile;

    // Video maps for configuration panel
    private ObservableCollection<SelectableVideoMap> _selectableVideoMaps = new();

    // PrefSets for configuration panel
    private List<CrcPrefSet> _availablePrefSets = new();

    // Batch generation cancellation
    private CancellationTokenSource? _batchCts;

    public MainWindow()
    {
        InitializeComponent();

        // Set window title with version from assembly (MinVer/git tag)
        Title = $"DGScope Profile Manager v{UpdateService.GetCurrentVersion()}";

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

        // Initialize with empty collections
        CrcProfilesTree.ItemsSource = _selectableProfiles;
        FacilitiesTree.ItemsSource = _facilities;
        VideoMapsListBox.ItemsSource = _selectableVideoMaps;

        // Disable buttons initially
        GenerateButton.IsEnabled = false;
        EditProfileButton.IsEnabled = false;
        DeleteProfileButton.IsEnabled = false;
        LaunchDGScopeButton.IsEnabled = false;

        UpdateStatus("Ready. Click Settings to configure paths, then Refresh to load profiles.");

        // Auto-refresh on launch if paths are configured
        if (!string.IsNullOrWhiteSpace(_settings.CrcFolderPath) ||
            !string.IsNullOrWhiteSpace(_settings.DgScopeFolderPath))
        {
            Loaded += (s, e) => LoadFolders();
        }

        // Check for updates on startup (after window is loaded)
        Loaded += async (s, e) => await CheckForUpdatesAsync();
    }

    #region Data Loading

    private async Task CheckForUpdatesAsync()
    {
        if (_settings.SkipUpdateCheck)
            return;

        try
        {
            var updateService = new UpdateService();
            var updateInfo = await updateService.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                var updateWindow = new UpdateNotificationWindow(updateInfo) { Owner = this };
                updateWindow.ShowDialog();

                if (updateWindow.DontRemindAgain)
                {
                    _settings.SkipUpdateCheck = true;
                    _persistenceService.SaveSettings(_settings);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    private async void LoadFolders()
    {
        try
        {
            UpdateStatus("Scanning folders...");

            _crcProfiles.Clear();
            _selectableProfiles.Clear();
            _facilities.Clear();

            // Force UI update
            CrcProfilesTree.ItemsSource = null;
            FacilitiesTree.ItemsSource = null;

            int crcCount = 0;
            int profileCount = 0;

            // Run scanning on background thread with lower priority to avoid UI lag
            await Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

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

            // Create selectable wrappers for CRC profiles
            foreach (var profile in _crcProfiles)
            {
                var selectable = new SelectableCrcProfile(profile);
                selectable.SelectionChanged += OnCrcSelectionChanged;
                _selectableProfiles.Add(selectable);
            }

            // Refresh UI bindings on UI thread
            CrcProfilesTree.ItemsSource = _selectableProfiles;
            FacilitiesTree.ItemsSource = _facilities;

            // Reset configuration panel
            UpdateConfigurationPanel();

            if (crcCount == 0 && profileCount == 0)
            {
                UpdateStatus("No profiles found. Check that paths are correct.");
            }
            else
            {
                UpdateStatus($"Loaded {crcCount} CRC profiles and {profileCount} DGScope profiles");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus("Error scanning folders");
            MessageBox.Show($"Error scanning folders:\n\n{ex.Message}", "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Selection Handling

    private void OnCrcSelectionChanged(object? sender, EventArgs e)
    {
        // Collect all selected areas
        _selectedAreas.Clear();
        foreach (var profile in _selectableProfiles)
        {
            foreach (var tracon in profile.Tracons)
            {
                foreach (var area in tracon.Areas.Where(a => a.IsSelected))
                {
                    _selectedAreas.Add(area);
                }
            }
        }

        UpdateConfigurationPanel();
    }

    private void CrcProfilesTree_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // Track the currently focused item for single-select operations
        if (e.NewValue is SelectableCrcProfile profile)
        {
            _currentProfile = profile;
            _currentTracon = null;
        }
        else if (e.NewValue is SelectableCrcTracon tracon)
        {
            _currentProfile = tracon.Parent;
            _currentTracon = tracon;
        }
        else if (e.NewValue is SelectableCrcArea area)
        {
            _currentTracon = area.Parent;
            _currentProfile = area.Parent.Parent;
        }
    }

    private void UpdateConfigurationPanel()
    {
        var selectedCount = _selectedAreas.Count;

        if (selectedCount == 0)
        {
            // Empty state
            EmptyStatePanel.Visibility = Visibility.Visible;
            SelectionSummaryPanel.Visibility = Visibility.Collapsed;
            OptionsPanel.Visibility = Visibility.Collapsed;
            PrefSetPanel.Visibility = Visibility.Collapsed;
            VideoMapsExpander.Visibility = Visibility.Collapsed;
            NamingPanel.Visibility = Visibility.Collapsed;
            GenerateButton.Visibility = Visibility.Visible;
            GenerateButton.IsEnabled = false;
            BatchGenerateButton.Visibility = Visibility.Collapsed;
        }
        else if (selectedCount == 1)
        {
            // Single selection - full options
            var selected = _selectedAreas[0];
            var tracon = selected.Parent.Tracon;
            var profile = selected.Parent.Parent.Profile;

            EmptyStatePanel.Visibility = Visibility.Collapsed;
            SelectionSummaryPanel.Visibility = Visibility.Visible;
            OptionsPanel.Visibility = Visibility.Visible;
            FacilityIdPanel.Visibility = Visibility.Visible;

            // Update selection summary
            SelectionCountText.Text = "1 item selected";
            SelectionPreviewList.ItemsSource = _selectedAreas.Take(5).ToList();
            MoreItemsText.Visibility = Visibility.Collapsed;

            // Set facility ID
            FacilityIdBox.Text = tracon.Id;

            // Load PrefSets for selected facility
            LoadPrefSetsForTracon(tracon.Id);
            PrefSetPanel.Visibility = _availablePrefSets.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            // Load video maps
            LoadVideoMapsForTracon(tracon);
            VideoMapsExpander.Visibility = Visibility.Visible;

            // Profile naming
            NamingPanel.Visibility = Visibility.Visible;
            ProfilePrefixText.Text = $"{tracon.Id}_";
            ProfileNameBox.Text = selected.Name ?? "default";
            BatchNamingHint.Visibility = Visibility.Collapsed;

            // Buttons
            GenerateButton.Visibility = Visibility.Visible;
            GenerateButton.IsEnabled = true;
            GenerateButton.Content = "Generate Profile";
            BatchGenerateButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Multi-selection - batch mode
            EmptyStatePanel.Visibility = Visibility.Collapsed;
            SelectionSummaryPanel.Visibility = Visibility.Visible;
            OptionsPanel.Visibility = Visibility.Visible;
            FacilityIdPanel.Visibility = Visibility.Collapsed; // Hide in batch mode

            // Update selection summary
            SelectionCountText.Text = $"{selectedCount} items selected";
            SelectionPreviewList.ItemsSource = _selectedAreas.Take(5).ToList();
            MoreItemsText.Visibility = selectedCount > 5 ? Visibility.Visible : Visibility.Collapsed;
            MoreItemsText.Text = $"...and {selectedCount - 5} more";

            // Hide single-select options
            PrefSetPanel.Visibility = Visibility.Collapsed;
            VideoMapsExpander.Visibility = Visibility.Collapsed;

            // Profile naming
            NamingPanel.Visibility = Visibility.Visible;
            ProfilePrefixText.Text = "";
            ProfileNameBox.Text = "";
            BatchNamingHint.Visibility = Visibility.Visible;

            // Buttons
            GenerateButton.Visibility = Visibility.Collapsed;
            BatchGenerateButton.Visibility = Visibility.Visible;
            BatchGenerateButton.Content = $"Generate {selectedCount} Profiles";
            BatchGenerateButton.IsEnabled = true;
        }
    }

    private async void LoadPrefSetsForTracon(string traconId)
    {
        _availablePrefSets.Clear();
        PrefSetComboBox.ItemsSource = null;

        if (string.IsNullOrWhiteSpace(_settings.CrcFolderPath))
            return;

        try
        {
            var crcFolder = _settings.CrcFolderPath;
            var prefSets = await Task.Run(() =>
            {
                var prefSetReader = new CrcPrefSetReader(crcFolder);
                return prefSetReader.GetPrefSets(traconId);
            });

            _availablePrefSets = prefSets;
            PrefSetComboBox.ItemsSource = _availablePrefSets;
        }
        catch
        {
            // Ignore errors loading PrefSets
        }
    }

    private void LoadVideoMapsForTracon(CrcTracon tracon)
    {
        _selectableVideoMaps.Clear();

        foreach (var map in tracon.AvailableVideoMaps)
        {
            var selectable = new SelectableVideoMap(map) { IsSelected = true };
            _selectableVideoMaps.Add(selectable);
        }

        UpdateVideoMapCountText();
    }

    private void UpdateVideoMapCountText()
    {
        var selectedCount = _selectableVideoMaps.Count(m => m.IsSelected);
        VideoMapCountText.Text = $"{selectedCount} of {_selectableVideoMaps.Count} maps selected";
    }

    #endregion

    #region Configuration Panel Events

    private void ClearPrefSet_Click(object sender, RoutedEventArgs e)
    {
        PrefSetComboBox.SelectedItem = null;
    }

    private void SelectAllMaps_Click(object sender, RoutedEventArgs e)
    {
        foreach (var map in _selectableVideoMaps)
            map.IsSelected = true;
        VideoMapsListBox.SelectAll();
        UpdateVideoMapCountText();
    }

    private void ClearMaps_Click(object sender, RoutedEventArgs e)
    {
        foreach (var map in _selectableVideoMaps)
            map.IsSelected = false;
        VideoMapsListBox.UnselectAll();
        UpdateVideoMapCountText();
    }

    private void VideoMapsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sync selection with SelectableVideoMap
        foreach (SelectableVideoMap map in _selectableVideoMaps)
        {
            map.IsSelected = VideoMapsListBox.SelectedItems.Contains(map);
        }
        UpdateVideoMapCountText();
    }

    #endregion

    #region Batch Menu Actions

    private void SelectAllCurrentArtcc_Click(object sender, RoutedEventArgs e)
    {
        if (_currentProfile != null)
        {
            _currentProfile.IsSelected = true;
        }
        else if (_selectableProfiles.Count > 0)
        {
            _selectableProfiles[0].IsSelected = true;
        }
    }

    private void SelectAllFacilities_Click(object sender, RoutedEventArgs e)
    {
        foreach (var profile in _selectableProfiles)
        {
            profile.IsSelected = true;
        }
    }

    private void ClearAllSelections_Click(object sender, RoutedEventArgs e)
    {
        foreach (var profile in _selectableProfiles)
        {
            profile.IsSelected = false;
        }
    }

    #endregion

    #region Profile Generation

    private async void GenerateProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAreas.Count != 1)
        {
            MessageBox.Show("Please select exactly one area to generate a profile.",
                "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var selectedArea = _selectedAreas[0];
            var tracon = selectedArea.Parent.Tracon;
            var crcProfile = selectedArea.Parent.Parent.Profile;

            // Get selected video maps
            List<VideoMapInfo> videoMaps;
            if (AutoSelectVideoMapsCheck.IsChecked == true)
            {
                videoMaps = tracon.AvailableVideoMaps;
            }
            else
            {
                videoMaps = _selectableVideoMaps
                    .Where(m => m.IsSelected)
                    .Select(m => m.VideoMap)
                    .ToList();
            }

            if (videoMaps.Count == 0)
            {
                MessageBox.Show("Please select at least one video map.",
                    "No Video Maps", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get optional settings
            var prefSet = PrefSetComboBox.SelectedItem as CrcPrefSet;
            var profileName = string.IsNullOrWhiteSpace(ProfileNameBox.Text)
                ? selectedArea.Name ?? "default"
                : ProfileNameBox.Text;
            var facilityIdOverride = FacilityIdBox.Text != tracon.Id ? FacilityIdBox.Text : null;

            // Generate profile on background thread to avoid UI freeze
            var outputDir = Path.Combine(_settings.DgScopeFolderPath, "profiles", crcProfile.ArtccCode);
            var importAtpa = ImportAtpaVolumesCheck.IsChecked == true;
            var crcVideoMapFolder = _settings.CrcVideoMapFolderPath;
            var defaultSettings = _settings.DefaultSettings;

            GenerateButton.IsEnabled = false;
            UpdateStatus("Generating profile...");

            var profile = await Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                Directory.CreateDirectory(outputDir);
                var generator = new ProfileGeneratorService();
                return generator.GenerateFromCrcWithMultipleMaps(
                    crcProfile,
                    outputDir,
                    videoMaps,
                    crcVideoMapFolder,
                    tracon,
                    selectedArea.Area,
                    profileName,
                    defaultSettings,
                    prefSet,
                    facilityIdOverride,
                    importAtpa);
            });

            if (profile != null)
            {
                UpdateStatus($"Generated profile: {profile.Name}");
                MessageBox.Show($"Profile generated successfully:\n{profile.Name}\n\nPath: {outputDir}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadFolders();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating profile: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GenerateButton.IsEnabled = true;
        }
    }

    private async void BatchGenerateProfiles_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAreas.Count == 0)
        {
            MessageBox.Show("Please select at least one area to generate profiles.",
                "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_settings.DgScopeFolderPath))
        {
            MessageBox.Show("Please configure DGScope folder path in Settings first.",
                "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Build batch items
        var items = _selectedAreas.Select(area => new BatchGenerationItem
        {
            CrcProfile = area.Parent.Parent.Profile,
            Tracon = area.Parent.Tracon,
            Area = area.Area,
            ProfileName = area.Name
        }).ToList();

        // Show progress panel
        ProgressPanel.Visibility = Visibility.Visible;
        GenerationProgressBar.Maximum = items.Count;
        GenerationProgressBar.Value = 0;

        // Disable UI during generation
        CrcProfilesTree.IsEnabled = false;
        BatchGenerateButton.IsEnabled = false;
        GenerateButton.IsEnabled = false;

        _batchCts = new CancellationTokenSource();

        var progress = new Progress<BatchProgressInfo>(p =>
        {
            ProgressStatusText.Text = $"Generating {p.CurrentItem}...";
            ProgressDetailText.Text = $"{p.CurrentIndex + 1} of {p.TotalCount}";
            GenerationProgressBar.Value = p.CurrentIndex;
        });

        var options = new BatchGenerationOptions
        {
            AutoSelectVideoMaps = AutoSelectVideoMapsCheck.IsChecked == true,
            DefaultSettings = _settings.DefaultSettings,
            OutputDirectory = Path.Combine(_settings.DgScopeFolderPath, "profiles"),
            CrcVideoMapFolder = _settings.CrcVideoMapFolderPath,
            ImportAtpaVolumes = ImportAtpaVolumesCheck.IsChecked == true
        };

        try
        {
            var batchService = new BatchGenerationService();
            var result = await batchService.GenerateBatchAsync(items, options, progress, _batchCts.Token);

            // Show summary
            var message = result.WasCancelled
                ? $"Batch cancelled. Generated {result.TotalGenerated} profiles."
                : $"Generated {result.TotalGenerated} profiles successfully.";

            if (result.TotalFailed > 0)
                message += $"\n{result.TotalFailed} profiles failed to generate.";

            UpdateStatus(message);
            MessageBox.Show(message, "Batch Generation Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during batch generation: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable UI
            ProgressPanel.Visibility = Visibility.Collapsed;
            CrcProfilesTree.IsEnabled = true;
            BatchGenerateButton.IsEnabled = true;
            GenerateButton.IsEnabled = true;
            _batchCts = null;

            // Refresh to show new profiles
            LoadFolders();
        }
    }

    private void CancelBatch_Click(object sender, RoutedEventArgs e)
    {
        _batchCts?.Cancel();
        ProgressStatusText.Text = "Cancelling...";
        CancelBatchButton.IsEnabled = false;
    }

    #endregion

    #region Menu Handlers

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings) { Owner = this };
        if (settingsWindow.ShowDialog() == true)
        {
            _settings = settingsWindow.Settings;
            _persistenceService.SaveSettings(_settings);
            LoadFolders();
        }
    }

    private void RefreshAll_Click(object sender, RoutedEventArgs e)
    {
        LoadFolders();
    }

    private void OpenProfilesFolder_Click(object sender, RoutedEventArgs e)
    {
        var profilesDir = Path.Combine(_settings.DgScopeFolderPath, "profiles");
        if (Directory.Exists(profilesDir))
            Process.Start("explorer.exe", profilesDir);
        else if (Directory.Exists(_settings.DgScopeFolderPath))
            Process.Start("explorer.exe", _settings.DgScopeFolderPath);
        else
            MessageBox.Show("Profiles folder not found.", "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void DefaultSettings_Click(object sender, RoutedEventArgs e)
    {
        var prefSetSettings = _settings.DefaultSettings.ToPrefSetSettings();
        var unifiedWindow = new UnifiedSettingsWindow(_settings, prefSetSettings) { Owner = this };
        if (unifiedWindow.ShowDialog() == true)
        {
            _settings.DefaultSettings.UpdateFromPrefSetSettings(prefSetSettings);
            UpdateStatus("Default settings updated");
        }
    }

    private async void FixAllPaths_Click(object sender, RoutedEventArgs e)
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
                UpdateStatus("Fixing paths...");
                var facilities = _facilities.ToList();

                var fixedCount = await Task.Run(() =>
                {
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                    int count = 0;
                    foreach (var facility in facilities)
                    {
                        var service = new DgScopeProfileService(facility.Path);
                        foreach (var profile in facility.Profiles)
                        {
                            service.FixFilePaths(profile, makeAbsolute: true);
                            count++;
                        }
                    }
                    return count;
                });

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

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateStatus("Checking for updates...");
            var updateService = new UpdateService();
            var updateInfo = await updateService.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                var updateWindow = new UpdateNotificationWindow(updateInfo) { Owner = this };
                updateWindow.ShowDialog();

                if (updateWindow.DontRemindAgain)
                {
                    _settings.SkipUpdateCheck = true;
                    _persistenceService.SaveSettings(_settings);
                }
            }
            else
            {
                UpdateStatus("You are running the latest version.");
                MessageBox.Show(
                    $"You are running the latest version (v{UpdateService.GetCurrentVersion()}).",
                    "No Updates",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus("Update check failed.");
            MessageBox.Show($"Failed to check for updates:\n\n{ex.Message}", "Update Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            $"DGScope Profile Manager\nVersion {UpdateService.GetCurrentVersion()}\n\nManage DGScope profiles and import from CRC data.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region DGScope Profiles Panel

    private void EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (FacilitiesTree.SelectedItem is DgScopeProfile profile)
        {
            var facility = _facilities.FirstOrDefault(f => f.Profiles.Contains(profile));
            if (facility != null)
            {
                var profileSettings = profile.LoadPrefSetSettings();
                var editor = new UnifiedSettingsWindow(_settings, profile, profileSettings) { Owner = this };
                if (editor.ShowDialog() == true)
                {
                    UpdateStatus($"Profile {profile.Name} updated");
                }
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

    /// <summary>
    /// Import ATPA volumes from VNAS API into existing profiles.
    /// Uses checked profiles first; falls back to selected facility/profile in tree.
    /// </summary>
    private async void ImportAtpa_Click(object sender, RoutedEventArgs e)
    {
        // Collect checked profiles grouped by ARTCC
        var checkedByArtcc = new Dictionary<string, List<DgScopeProfile>>();
        foreach (var fac in _facilities)
        {
            var checkedProfiles = fac.Profiles.Where(p => p.IsSelected).ToList();
            if (checkedProfiles.Count > 0)
                checkedByArtcc[fac.ArtccCode] = checkedProfiles;
        }

        // If no checkboxes checked, fall back to tree selection (single facility or profile)
        if (checkedByArtcc.Count == 0)
        {
            if (FacilitiesTree.SelectedItem is Facility facility)
            {
                checkedByArtcc[facility.ArtccCode] = facility.Profiles;
            }
            else if (FacilitiesTree.SelectedItem is DgScopeProfile profile)
            {
                var parentFacility = _facilities.FirstOrDefault(f => f.Profiles.Contains(profile));
                if (parentFacility == null)
                {
                    MessageBox.Show("Cannot determine ARTCC for this profile.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                checkedByArtcc[parentFacility.ArtccCode] = new List<DgScopeProfile> { profile };
            }
            else
            {
                MessageBox.Show("Check profiles or select a facility first.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        var totalTargetProfiles = checkedByArtcc.Values.Sum(p => p.Count);
        if (totalTargetProfiles == 0) return;

        try
        {
            var vnasService = new VnasApiService();

            // Fetch all ATPA volumes first (needs to be on UI thread for status updates)
            var allVolumes = new Dictionary<string, Dictionary<string, List<VnasAtpaVolume>>>();
            foreach (var (artccCode, _) in checkedByArtcc)
            {
                UpdateStatus($"Fetching ATPA volumes for {artccCode}...");
                var volumesByFacility = await vnasService.FetchAtpaVolumesByFacilityAsync(artccCode);
                if (volumesByFacility.Count > 0)
                    allVolumes[artccCode] = volumesByFacility;
            }

            if (allVolumes.Count == 0)
            {
                MessageBox.Show("No ATPA volumes found on the VNAS API.",
                    "No Volumes", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateStatus("Ready.");
                return;
            }

            UpdateStatus($"Applying ATPA volumes to {totalTargetProfiles} profile(s)...");

            // Process profiles on background thread
            var profileData = checkedByArtcc
                .Where(kvp => allVolumes.ContainsKey(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(p => (p.Name, p.FilePath)).ToList());

            var (updated, totalVolumes) = await Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                int updatedCount = 0;
                int volCount = 0;

                foreach (var (artccCode, profiles) in profileData)
                {
                    var volumesByFacility = allVolumes[artccCode];

                    foreach (var (profName, profPath) in profiles)
                    {
                        try
                        {
                            var facilityId = VnasApiService.GetFacilityIdFromProfileName(profName);
                            if (!volumesByFacility.TryGetValue(facilityId, out var volumes) || volumes.Count == 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"No ATPA volumes for facility {facilityId} (profile {profName})");
                                continue;
                            }

                            var doc = System.Xml.Linq.XDocument.Load(profPath);
                            var root = doc.Root;
                            if (root == null) continue;

                            var atpaActiveEl = root.Element("ATPAActive");
                            if (atpaActiveEl != null) atpaActiveEl.Value = "true";
                            else root.Add(new System.Xml.Linq.XElement("ATPAActive", "true"));

                            var monitorConesEl = root.Element("DrawATPAMonitorCones");
                            if (monitorConesEl != null) monitorConesEl.Value = "true";
                            else root.Add(new System.Xml.Linq.XElement("DrawATPAMonitorCones", "true"));

                        var atpaEl = root.Element("ATPAVolumes");
                        if (atpaEl == null)
                        {
                            var sep = root.Element("ATPASeparationTable");
                            atpaEl = new System.Xml.Linq.XElement("ATPAVolumes");
                            if (sep != null) sep.AddAfterSelf(atpaEl);
                            else root.Add(atpaEl);
                        }
                        else
                        {
                            atpaEl.RemoveAll();
                        }

                        foreach (var vol in volumes)
                        {
                            var spFilters = new System.Xml.Linq.XElement("ScratchpadFilters");
                            foreach (var sp in vol.Scratchpads)
                            {
                                spFilters.Add(new System.Xml.Linq.XElement("ScratchpadFilter",
                                    new System.Xml.Linq.XElement("ScratchpadValue", sp.Entry),
                                    new System.Xml.Linq.XElement("ScratchpadNum", sp.ScratchpadNumber),
                                    new System.Xml.Linq.XElement("ScratchpadFilterType", sp.FilterType)
                                ));
                            }

                            atpaEl.Add(new System.Xml.Linq.XElement("ATPAVolume",
                                new System.Xml.Linq.XElement("VolumeId", vol.VolumeId),
                                new System.Xml.Linq.XElement("Name", vol.Name),
                                new System.Xml.Linq.XElement("Active", "true"),
                                new System.Xml.Linq.XElement("Draw", "false"),
                                new System.Xml.Linq.XElement("RunwayThreshold",
                                    new System.Xml.Linq.XElement("Latitude", vol.ThresholdLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                    new System.Xml.Linq.XElement("Longitude", vol.ThresholdLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture))
                                ),
                                new System.Xml.Linq.XElement("TrueHeading", vol.TrueHeading),
                                new System.Xml.Linq.XElement("MaxHeadingDeviation", vol.MaximumHeadingDeviation),
                                new System.Xml.Linq.XElement("Ceiling", vol.Ceiling),
                                new System.Xml.Linq.XElement("Floor", vol.Floor),
                                new System.Xml.Linq.XElement("Length", vol.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                new System.Xml.Linq.XElement("WidthLeft", vol.WidthLeft),
                                new System.Xml.Linq.XElement("WidthRight", vol.WidthRight),
                                new System.Xml.Linq.XElement("TwoPointFiveEnabled", vol.TwoPointFiveApproachEnabled.ToString().ToLowerInvariant()),
                                new System.Xml.Linq.XElement("TwoPointFiveActive", vol.TwoPointFiveApproachEnabled.ToString().ToLowerInvariant()),
                                new System.Xml.Linq.XElement("TwoPointFiveDistance", vol.TwoPointFiveApproachDistance.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                                new System.Xml.Linq.XElement("Destination", vol.AirportId),
                                new System.Xml.Linq.XElement("LeaderFilters"),
                                spFilters,
                                new System.Xml.Linq.XElement("TcpDisplay"),
                                new System.Xml.Linq.XElement("TcpExclusion")
                            ));
                        }

                            doc.Save(profPath);
                            updatedCount++;
                            volCount += volumes.Count;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to update ATPA for {profName}: {ex.Message}");
                        }
                    }
                }

                return (updatedCount, volCount);
            });

            UpdateStatus($"Imported ATPA volumes into {updated} profile(s)");
            MessageBox.Show($"Imported ATPA volumes into {updated} profile(s) ({totalVolumes} total volumes).\nEach profile received only its facility's volumes.",
                "ATPA Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error importing ATPA volumes: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Ready.");
        }
    }

    private void ToggleSelectAllProfiles_Click(object sender, RoutedEventArgs e)
    {
        // If any are unchecked, select all; otherwise deselect all
        bool anyUnchecked = _facilities.Any(f => f.Profiles.Any(p => !p.IsSelected));
        foreach (var facility in _facilities)
            foreach (var profile in facility.Profiles)
                profile.IsSelected = anyUnchecked;
        SelectAllButton.Content = anyUnchecked ? "Deselect All" : "Select All";
    }

    private void ExpandAllFacilities_Click(object sender, RoutedEventArgs e)
    {
        SetFacilitiesTreeExpansion(true);
    }

    private void CollapseAllFacilities_Click(object sender, RoutedEventArgs e)
    {
        SetFacilitiesTreeExpansion(false);
    }

    private void SetFacilitiesTreeExpansion(bool expand)
    {
        foreach (var item in FacilitiesTree.Items)
        {
            if (FacilitiesTree.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsExpanded = expand;
            }
        }
    }

    private void FacilityTree_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var selectedProfile = FacilitiesTree.SelectedItem as DgScopeProfile;
        EditProfileButton.IsEnabled = selectedProfile != null;
        DeleteProfileButton.IsEnabled = selectedProfile != null;
        LaunchDGScopeButton.IsEnabled = selectedProfile != null;
    }

    private void FacilitiesTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FacilitiesTree.SelectedItem is DgScopeProfile)
        {
            LaunchDGScope_Click(sender, e);
        }
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

        if (string.IsNullOrWhiteSpace(_settings.DgScopeExePath))
        {
            MessageBox.Show(
                "DGScope executable path is not configured.\n\nPlease go to Settings and set the path to DGScope.exe",
                "DGScope Not Configured",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

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
            MessageBox.Show($"Failed to launch DGScope:\n\n{ex.Message}", "Launch Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    private void UpdateStatus(string message)
    {
        StatusText.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
    }
}
