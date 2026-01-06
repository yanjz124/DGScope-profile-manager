using System.Windows;
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
        
        _profile = profile;
        _facility = facility;
        
        LoadProfileData();
    }
    
    private void LoadProfileData()
    {
        ProfileNameText.Text = _profile.Name;
        FilePathText.Text = _profile.FilePath;
        VideoMapText.Text = _profile.VideoMapPaths.FirstOrDefault() ?? "None";
        
        // Load settings from AllSettings dictionary
        ScreenRotation.Text = _profile.ScreenRotation?.ToString() ?? _profile.AllSettings.GetValueOrDefault("ScreenRotation", "0");
        FontName.Text = _profile.FontName ?? _profile.AllSettings.GetValueOrDefault("FontName", "");
        FontSizeBox.Text = _profile.FontSize?.ToString() ?? _profile.AllSettings.GetValueOrDefault("FontSize", "");
        BackColor.Text = _profile.BackColor?.ToString() ?? _profile.AllSettings.GetValueOrDefault("BackColor", "");
        HomeLatitude.Text = _profile.AllSettings.GetValueOrDefault("HomeLatitude", "");
        HomeLongitude.Text = _profile.AllSettings.GetValueOrDefault("HomeLongitude", "");
        AltimeterStations.Text = _profile.AllSettings.GetValueOrDefault("AltimeterStations", "");
    }
    
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Update settings
            _profile.ScreenRotation = int.TryParse(ScreenRotation.Text, out var sr) ? sr : null;
            _profile.FontName = FontName.Text;
            _profile.FontSize = int.TryParse(FontSizeBox.Text, out var fs) ? fs : null;
            _profile.BackColor = int.TryParse(BackColor.Text, out var bc) ? bc : null;
            
            // Update AllSettings
            _profile.AllSettings["ScreenRotation"] = ScreenRotation.Text;
            _profile.AllSettings["FontName"] = FontName.Text;
            _profile.AllSettings["FontSize"] = FontSizeBox.Text;
            _profile.AllSettings["BackColor"] = BackColor.Text;
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
