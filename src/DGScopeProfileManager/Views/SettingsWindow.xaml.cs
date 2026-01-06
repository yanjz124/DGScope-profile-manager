using System.Windows;
using Microsoft.Win32;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Views;

public partial class SettingsWindow : Window
{
    public AppSettings Settings { get; private set; }
    
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        Settings = settings;

        // Load current settings
        CrcFolderPath.Text = settings.CrcFolderPath;
        DgScopeFolderPath.Text = settings.DgScopeFolderPath;
    }

    private void BrowseCrc_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select CRC Root Folder"
        };
        
        if (dialog.ShowDialog() == true)
        {
            CrcFolderPath.Text = dialog.FolderName;
        }
    }
    
    private void BrowseDgScope_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select DGScope Root Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            DgScopeFolderPath.Text = dialog.FolderName;
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        Settings.CrcFolderPath = CrcFolderPath.Text;
        Settings.DgScopeFolderPath = DgScopeFolderPath.Text;

        DialogResult = true;
        Close();
    }
}
