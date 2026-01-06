using System.IO;
using System.Windows;
using System.Windows.Controls;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Views;

public partial class GenerateProfileDialog : Window
{
    private readonly CrcProfile _crcProfile;
    private readonly string _rootPath;
    
    public string ArtccCode => ArtccCodeBox.Text;
    public string FacilityCode => FacilityCodeBox.Text;
    
    public GenerateProfileDialog(CrcProfile crcProfile, string dgScopeRootPath)
    {
        InitializeComponent();
        
        _crcProfile = crcProfile;
        _rootPath = dgScopeRootPath;
        
        ProfileName.Text = crcProfile.Name;
        ArtccCodeBox.Text = crcProfile.ArtccCode;
        FacilityCodeBox.Text = crcProfile.ArtccCode; // Default to ARTCC code
        
        ArtccCodeBox.TextChanged += UpdatePreview;
        FacilityCodeBox.TextChanged += UpdatePreview;
        
        UpdatePreview(null, null);
    }
    
    private void UpdatePreview(object? sender, TextChangedEventArgs? e)
    {
        if (string.IsNullOrWhiteSpace(ArtccCodeBox.Text) || string.IsNullOrWhiteSpace(FacilityCodeBox.Text))
        {
            OutputPathPreview.Text = "Please enter both ARTCC and Facility codes";
        }
        else
        {
            var path = Path.Combine(_rootPath, ArtccCodeBox.Text, FacilityCodeBox.Text, $"{_crcProfile.ArtccCode}.xml");
            OutputPathPreview.Text = path;
        }
    }
    
    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ArtccCode))
        {
            MessageBox.Show("Please enter ARTCC code", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(FacilityCode))
        {
            MessageBox.Show("Please enter Facility code", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        DialogResult = true;
        Close();
    }
}
