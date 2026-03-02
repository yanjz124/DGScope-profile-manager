using System.Globalization;
using System.Windows;
using System.Xml.Linq;
using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

public partial class ProfileEditorWindow : Window
{
    private readonly DgScopeProfile _profile;
    private readonly Facility _facility;

    public ProfileEditorWindow(DgScopeProfile profile, Facility facility)
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "ProfileEditorWindow");

        _profile = profile;
        _facility = facility;

        LoadProfileData();
    }

    private void LoadProfileData()
    {
        ProfileNameText.Text = _profile.Name;
        FilePathText.Text = _profile.FilePath;
        VideoMapText.Text = _profile.VideoMapPaths.FirstOrDefault() ?? "None";

        // Load simple mode settings
        Brightness.Text = _profile.AllSettings.GetValueOrDefault("Brightness", "");
        ScreenCenterPoint.Text = _profile.AllSettings.GetValueOrDefault("ScreenCenterPoint", "");
        ScreenRotation.Text = _profile.ScreenRotation?.ToString() ?? _profile.AllSettings.GetValueOrDefault("ScreenRotation", "0");
        FontName.Text = _profile.FontName ?? _profile.AllSettings.GetValueOrDefault("FontName", "");
        FontSizeBox.Text = _profile.FontSize?.ToString() ?? _profile.AllSettings.GetValueOrDefault("FontSize", "");

        // Load detailed mode settings
        BackColor.Text = _profile.BackColor?.ToString() ?? _profile.AllSettings.GetValueOrDefault("BackColor", "");
        OwnedDataBlockPosition.Text = _profile.AllSettings.GetValueOrDefault("OwnedDataBlockPosition", "");
        PreviewAreaLocation.Text = _profile.AllSettings.GetValueOrDefault("PreviewAreaLocation", "");
        HomeLatitude.Text = _profile.AllSettings.GetValueOrDefault("HomeLatitude", "");
        HomeLongitude.Text = _profile.AllSettings.GetValueOrDefault("HomeLongitude", "");
        AltimeterStations.Text = _profile.AllSettings.GetValueOrDefault("AltimeterStations", "");

        // Load ATPA info from profile XML
        LoadAtpaInfo();
    }

    /// <summary>
    /// Read ATPA volume info from the profile XML and populate the UI
    /// </summary>
    private void LoadAtpaInfo()
    {
        try
        {
            var doc = XDocument.Load(_profile.FilePath);
            var root = doc.Root;
            if (root == null) return;

            // ATPA Active status
            var atpaActive = root.Element("ATPAActive")?.Value ?? "false";
            AtpaActiveCheckBox.IsChecked = atpaActive.Equals("true", StringComparison.OrdinalIgnoreCase);

            // Count and list ATPA volumes
            var atpaVolumes = root.Element("ATPAVolumes");
            var volumeElements = atpaVolumes?.Elements("ATPAVolume").ToList() ?? new();

            AtpaVolumeCountText.Text = volumeElements.Count.ToString();

            AtpaVolumeList.Items.Clear();
            foreach (var vol in volumeElements)
            {
                var volumeId = vol.Element("VolumeId")?.Value ?? "?";
                var name = vol.Element("Name")?.Value ?? "";
                var destination = vol.Element("Destination")?.Value ?? "";
                AtpaVolumeList.Items.Add($"{volumeId} - {name} ({destination})");
            }
        }
        catch (Exception ex)
        {
            AtpaActiveCheckBox.IsChecked = false;
            AtpaVolumeCountText.Text = ex.Message;
        }
    }

    /// <summary>
    /// Import ATPA volumes from VNAS API into the profile XML.
    /// Only modifies the ATPAVolumes element — all other profile settings are preserved.
    /// </summary>
    private async void ImportAtpaVolumes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var artccCode = _facility.ArtccCode;
            if (string.IsNullOrWhiteSpace(artccCode))
            {
                MessageBox.Show("Cannot determine ARTCC code for this profile.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var vnasService = new VnasApiService();
            var volumesByFacility = await vnasService.FetchAtpaVolumesByFacilityAsync(artccCode);

            // Filter by facility ID from profile name (e.g., "PCT_MTV N" → "PCT")
            var facilityId = VnasApiService.GetFacilityIdFromProfileName(_profile.Name);
            List<VnasAtpaVolume> volumes;
            if (volumesByFacility.TryGetValue(facilityId, out var facilityVolumes))
                volumes = facilityVolumes;
            else
                volumes = volumesByFacility.Values.SelectMany(v => v).ToList();

            if (volumes.Count == 0)
            {
                MessageBox.Show($"No ATPA volumes found for {artccCode} (facility {facilityId}) on the VNAS API.",
                    "No Volumes", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Load existing profile XML and update only ATPAVolumes
            var doc = XDocument.Load(_profile.FilePath);
            var root = doc.Root;
            if (root == null) return;

            // Enable ATPA globally
            var atpaActiveEl = root.Element("ATPAActive");
            if (atpaActiveEl != null)
                atpaActiveEl.Value = "true";
            else
                root.Add(new XElement("ATPAActive", "true"));

            var atpaVolumesElement = root.Element("ATPAVolumes");
            if (atpaVolumesElement == null)
            {
                var separationTable = root.Element("ATPASeparationTable");
                atpaVolumesElement = new XElement("ATPAVolumes");
                if (separationTable != null)
                    separationTable.AddAfterSelf(atpaVolumesElement);
                else
                    root.Add(atpaVolumesElement);
            }
            else
            {
                atpaVolumesElement.RemoveAll();
            }

            foreach (var vol in volumes)
            {
                var scratchpadFilters = new XElement("ScratchpadFilters");
                foreach (var sp in vol.Scratchpads)
                {
                    scratchpadFilters.Add(new XElement("ScratchpadFilter",
                        new XElement("ScratchpadValue", sp.Entry),
                        new XElement("ScratchpadNum", sp.ScratchpadNumber),
                        new XElement("ScratchpadFilterType", sp.FilterType)
                    ));
                }

                atpaVolumesElement.Add(new XElement("ATPAVolume",
                    new XElement("VolumeId", vol.VolumeId),
                    new XElement("Name", vol.Name),
                    new XElement("Active", "true"),
                    new XElement("Draw", "true"),
                    new XElement("RunwayThreshold",
                        new XElement("Latitude", vol.ThresholdLatitude.ToString(CultureInfo.InvariantCulture)),
                        new XElement("Longitude", vol.ThresholdLongitude.ToString(CultureInfo.InvariantCulture))
                    ),
                    new XElement("TrueHeading", vol.MagneticHeading),
                    new XElement("MaxHeadingDeviation", vol.MaximumHeadingDeviation),
                    new XElement("Ceiling", vol.Ceiling),
                    new XElement("Floor", vol.Floor),
                    new XElement("Length", vol.Length.ToString(CultureInfo.InvariantCulture)),
                    new XElement("WidthLeft", vol.WidthLeft),
                    new XElement("WidthRight", vol.WidthRight),
                    new XElement("TwoPointFiveEnabled", vol.TwoPointFiveApproachEnabled.ToString().ToLowerInvariant()),
                    new XElement("TwoPointFiveActive", "false"),
                    new XElement("TwoPointFiveDistance", vol.TwoPointFiveApproachDistance.ToString(CultureInfo.InvariantCulture)),
                    new XElement("Destination", vol.AirportId),
                    new XElement("LeaderFilters"),
                    scratchpadFilters,
                    new XElement("TcpDisplay"),
                    new XElement("TcpExclusion")
                ));
            }

            doc.Save(_profile.FilePath);

            // Refresh the display
            LoadAtpaInfo();

            MessageBox.Show($"Imported {volumes.Count} ATPA volumes from VNAS for {artccCode}.\nAll other profile settings were preserved.",
                "ATPA Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error importing ATPA volumes: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Clear all ATPA volumes from the profile XML
    /// </summary>
    private void ClearAtpaVolumes_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Remove all ATPA volumes from this profile?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var doc = XDocument.Load(_profile.FilePath);
            var root = doc.Root;
            if (root == null) return;

            var atpaVolumes = root.Element("ATPAVolumes");
            if (atpaVolumes != null)
            {
                atpaVolumes.RemoveAll();
            }

            doc.Save(_profile.FilePath);
            LoadAtpaInfo();

            MessageBox.Show("ATPA volumes cleared.", "Done",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error clearing ATPA volumes: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AtpaActive_Changed(object sender, RoutedEventArgs e)
    {
        try
        {
            var doc = XDocument.Load(_profile.FilePath);
            var root = doc.Root;
            if (root == null) return;

            var value = AtpaActiveCheckBox.IsChecked == true ? "true" : "false";
            var atpaActiveEl = root.Element("ATPAActive");
            if (atpaActiveEl != null)
                atpaActiveEl.Value = value;
            else
                root.Add(new XElement("ATPAActive", value));

            doc.Save(_profile.FilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating ATPA active: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DetailedMode_Changed(object sender, RoutedEventArgs e)
    {
        var isDetailed = DetailedModeCheckBox.IsChecked == true;
        DetailedSettingsPanel.Visibility = isDetailed ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        LocationSettingsPanel.Visibility = isDetailed ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        OtherSettingsPanel.Visibility = isDetailed ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update simple mode settings
            _profile.ScreenRotation = int.TryParse(ScreenRotation.Text, out var sr) ? sr : null;
            _profile.FontName = FontName.Text;
            _profile.FontSize = int.TryParse(FontSizeBox.Text, out var fs) ? fs : null;
            _profile.BackColor = int.TryParse(BackColor.Text, out var bc) ? bc : null;

            // Update AllSettings for simple mode
            _profile.AllSettings["Brightness"] = Brightness.Text;
            _profile.AllSettings["ScreenCenterPoint"] = ScreenCenterPoint.Text;
            _profile.AllSettings["ScreenRotation"] = ScreenRotation.Text;
            _profile.AllSettings["FontName"] = FontName.Text;
            _profile.AllSettings["FontSize"] = FontSizeBox.Text;

            // Update AllSettings for detailed mode
            _profile.AllSettings["BackColor"] = BackColor.Text;
            _profile.AllSettings["OwnedDataBlockPosition"] = OwnedDataBlockPosition.Text;
            _profile.AllSettings["PreviewAreaLocation"] = PreviewAreaLocation.Text;
            _profile.AllSettings["HomeLatitude"] = HomeLatitude.Text;
            _profile.AllSettings["HomeLongitude"] = HomeLongitude.Text;
            _profile.AllSettings["AltimeterStations"] = AltimeterStations.Text;

            // Save to file
            var service = new DgScopeProfileService(_facility.Path);
            service.SaveProfile(_profile);

            MessageBox.Show("Profile saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving profile: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
