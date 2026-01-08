using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;
using System.Windows;
using System.Windows.Input;

namespace DGScopeProfileManager.Views;

public partial class TraconSelectionWindow : Window
{
    public CrcTracon? SelectedTracon { get; private set; }

    public TraconSelectionWindow(CrcProfile profile)
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "TraconSelectionWindow");
        Title = $"Select TRACON to generate - {profile.ArtccCode}";
        TraconListBox.ItemsSource = profile.Tracons;
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
        
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
