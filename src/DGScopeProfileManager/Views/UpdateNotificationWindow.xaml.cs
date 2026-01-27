using System.Windows;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

/// <summary>
/// Dialog for notifying user about available updates
/// </summary>
public partial class UpdateNotificationWindow : Window
{
    private readonly UpdateInfo _updateInfo;
    private readonly UpdateService _updateService;

    /// <summary>
    /// True if user checked "Don't remind me again"
    /// </summary>
    public bool DontRemindAgain => DontRemindCheckBox.IsChecked == true;

    public UpdateNotificationWindow(UpdateInfo updateInfo)
    {
        InitializeComponent();

        _updateInfo = updateInfo;
        _updateService = new UpdateService();

        // Set version text
        CurrentVersionText.Text = updateInfo.CurrentVersion;
        NewVersionText.Text = updateInfo.LatestVersion;
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        // Disable buttons and show progress
        UpdateButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        DontRemindCheckBox.IsEnabled = false;
        QuestionText.Visibility = Visibility.Collapsed;
        ProgressPanel.Visibility = Visibility.Visible;

        var progress = new Progress<int>(percent =>
        {
            DownloadProgress.Value = percent;
            ProgressText.Text = $"Downloading update... {percent}%";
        });

        var success = await _updateService.DownloadAndInstallAsync(_updateInfo, progress);

        if (!success)
        {
            MessageBox.Show(
                "Failed to download the update. Please try again later or download manually from GitHub.",
                "Update Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Re-enable buttons
            UpdateButton.IsEnabled = true;
            LaterButton.IsEnabled = true;
            DontRemindCheckBox.IsEnabled = true;
            QuestionText.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
        }
        // If success, the app will exit in DownloadAndInstallAsync
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
