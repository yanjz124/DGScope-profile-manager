using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DGScopeProfileManager.Views;

public partial class ProfileConfigDialog : Window
{
    public ProfileConfigDialog(string facilityName)
    {
        InitializeComponent();
        FacilityName.Text = facilityName;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
