using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

public partial class AreaSelectionWindow : Window
{
    public CrcArea? SelectedArea { get; private set; }

    public AreaSelectionWindow(List<CrcArea> areas)
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "AreaSelectionWindow");
        AreasList.ItemsSource = areas;

        if (areas.Count > 0)
        {
            AreasList.SelectedIndex = 0;
        }
    }

    private void AreasList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click to select and confirm
        OkButton_Click(sender, e);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (AreasList.SelectedItem is CrcArea area)
        {
            SelectedArea = area;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please select an area.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
