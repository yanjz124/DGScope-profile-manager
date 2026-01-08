using System.Windows;
using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

public partial class ProfileSelectionWindow : Window
{
    public DgScopeProfile? SelectedProfile { get; private set; }

    public ProfileSelectionWindow(List<Facility> facilities)
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "ProfileSelectionWindow");
        ProfilesTree.ItemsSource = facilities;
    }

    private void ProfilesTree_SelectionChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        SelectedProfile = ProfilesTree.SelectedItem as DgScopeProfile;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProfile == null)
        {
            MessageBox.Show("Please select a profile.", "No Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
