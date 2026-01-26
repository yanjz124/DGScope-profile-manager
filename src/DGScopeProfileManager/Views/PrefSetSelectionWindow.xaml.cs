using System.Windows;
using System.Windows.Input;
using DGScopeProfileManager.Models;
using DGScopeProfileManager.Services;

namespace DGScopeProfileManager.Views;

public partial class PrefSetSelectionWindow : Window
{
    public CrcPrefSet? SelectedPrefSet { get; private set; }
    public string ProfileName { get; private set; } = "default";
    public bool Skipped { get; private set; }

    private readonly string _defaultProfileName;

    public PrefSetSelectionWindow(List<CrcPrefSet> prefSets, string defaultProfileName = "default")
    {
        InitializeComponent();
        WindowPositionService.InitializePositionTracking(this, "PrefSetSelectionWindow");

        _defaultProfileName = defaultProfileName;
        ProfileNameTextBox.Text = defaultProfileName;

        PrefSetsList.ItemsSource = prefSets;

        if (prefSets.Count > 0)
        {
            PrefSetsList.SelectedIndex = 0;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position window to the right of owner
        if (Owner != null)
        {
            Left = Owner.Left + Owner.Width + 10;
            Top = Owner.Top;

            // Make sure window is on screen
            var workingArea = SystemParameters.WorkArea;
            if (Left + Width > workingArea.Right)
            {
                Left = Owner.Left - Width - 10;
            }
            if (Top + Height > workingArea.Bottom)
            {
                Top = workingArea.Bottom - Height;
            }
        }
    }

    private void PrefSetsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // When user selects a PrefSet, update the profile name to match
        if (PrefSetsList.SelectedItem is CrcPrefSet prefSet)
        {
            ProfileNameTextBox.Text = prefSet.Name;
        }
    }

    private void PrefSetsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-click to select and confirm
        OkButton_Click(sender, e);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (PrefSetsList.SelectedItem is CrcPrefSet prefSet)
        {
            SelectedPrefSet = prefSet;
            ProfileName = string.IsNullOrWhiteSpace(ProfileNameTextBox.Text)
                ? prefSet.Name
                : ProfileNameTextBox.Text.Trim();
            Skipped = false;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please select a PrefSet or click 'Skip PrefSet' to continue without one.",
                "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        // User chose to skip PrefSet selection
        SelectedPrefSet = null;
        ProfileName = string.IsNullOrWhiteSpace(ProfileNameTextBox.Text)
            ? _defaultProfileName
            : ProfileNameTextBox.Text.Trim();
        Skipped = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
