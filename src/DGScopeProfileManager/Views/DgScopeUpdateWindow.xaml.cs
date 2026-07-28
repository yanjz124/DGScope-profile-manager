using System.Windows;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

/// <summary>
/// Dialog for notifying the user about an available DGScope update and applying it in place.
/// </summary>
public partial class DgScopeUpdateWindow : Window
{
    private readonly DgScopeUpdateInfo _updateInfo;
    private readonly DgScopeUpdateService _updateService;

    /// <summary>
    /// True if the user checked "Don't check for DGScope updates on startup".
    /// </summary>
    public bool DontRemindAgain => DontRemindCheckBox.IsChecked == true;

    public DgScopeUpdateWindow(DgScopeUpdateInfo updateInfo)
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "DgScopeUpdateWindow");

        _updateInfo = updateInfo;
        _updateService = new DgScopeUpdateService();

        CurrentVersionText.Text = updateInfo.InstalledVersion;
        NewVersionText.Text = updateInfo.LatestVersion;

        if (!string.IsNullOrWhiteSpace(updateInfo.ReleaseNotes))
        {
            ReleaseNotesText.Text = updateInfo.ReleaseNotes.Trim();
            ReleaseNotesPanel.Visibility = Visibility.Visible;
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        DontRemindCheckBox.IsEnabled = false;
        QuestionText.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;

        var progress = new Progress<int>(percent =>
        {
            DownloadProgress.Value = percent;
            ProgressText.Text = percent >= 100
                ? "Installing DGScope..."
                : $"Downloading DGScope... {percent}%";
        });

        var success = await _updateService.DownloadAndApplyAsync(_updateInfo, progress);

        if (success)
        {
            MessageBox.Show(
                $"DGScope was updated to {_updateInfo.LatestVersion}.",
                "DGScope Updated",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show(
                "Failed to update DGScope. Please try again later or download it manually from GitHub.",
                "Update Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            UpdateButton.IsEnabled = true;
            LaterButton.IsEnabled = true;
            DontRemindCheckBox.IsEnabled = true;
            QuestionText.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
