using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Views;

public partial class AreaSelectionWindow : Window
{
    public CrcArea? SelectedArea { get; private set; }

    public AreaSelectionWindow(List<CrcArea> areas)
    {
        InitializeComponent();
        AreasList.ItemsSource = areas;

        if (areas.Count > 0)
        {
            AreasList.SelectedIndex = 0;
        }
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
