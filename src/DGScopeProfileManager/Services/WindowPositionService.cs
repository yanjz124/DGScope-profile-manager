using System.Windows;
using DGScopeProfileManager.Models;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Service for saving and restoring window positions
/// </summary>
public static class WindowPositionService
{
    /// <summary>
    /// Save the current window position
    /// </summary>
    public static void SavePosition(Window window, AppSettings settings, string windowKey)
    {
        if (window == null || settings == null)
            return;

        // Don't save if window was never shown or is in an invalid state
        if (double.IsNaN(window.Left) || double.IsNaN(window.Top) ||
            double.IsNaN(window.Width) || double.IsNaN(window.Height))
        {
            System.Diagnostics.Debug.WriteLine($"Skipping save for {windowKey} - invalid position");
            return;
        }

        var position = new WindowPosition
        {
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            IsMaximized = window.WindowState == WindowState.Maximized
        };

        settings.WindowPositions[windowKey] = position;
        System.Diagnostics.Debug.WriteLine($"✓ Saved position for {windowKey}: ({position.Left:F0}, {position.Top:F0}) {position.Width:F0}x{position.Height:F0} Maximized={position.IsMaximized}");
    }

    /// <summary>
    /// Restore window position from saved settings
    /// </summary>
    public static void RestorePosition(Window window, AppSettings settings, string windowKey)
    {
        if (window == null || settings == null)
        {
            System.Diagnostics.Debug.WriteLine($"Cannot restore {windowKey} - window or settings null");
            return;
        }

        if (!settings.WindowPositions.TryGetValue(windowKey, out var position))
        {
            System.Diagnostics.Debug.WriteLine($"No saved position for {windowKey}");
            return;
        }

        // Ensure the window is within screen bounds
        var screenWidth = SystemParameters.VirtualScreenWidth;
        var screenHeight = SystemParameters.VirtualScreenHeight;

        var adjustedLeft = position.Left;
        var adjustedTop = position.Top;

        if (position.Left < 0 || position.Left + position.Width > screenWidth)
            adjustedLeft = (screenWidth - position.Width) / 2;

        if (position.Top < 0 || position.Top + position.Height > screenHeight)
            adjustedTop = (screenHeight - position.Height) / 2;

        window.Left = adjustedLeft;
        window.Top = adjustedTop;
        window.Width = position.Width;
        window.Height = position.Height;

        if (position.IsMaximized)
            window.WindowState = WindowState.Maximized;

        System.Diagnostics.Debug.WriteLine($"✓ Restored position for {windowKey}: ({adjustedLeft:F0}, {adjustedTop:F0}) {position.Width:F0}x{position.Height:F0} Maximized={position.IsMaximized}");
    }

    /// <summary>
    /// Initialize window position tracking
    /// Sets up event handlers to save position on close
    /// </summary>
    public static void InitializePositionTracking(Window window, AppSettings settings, string windowKey)
    {
        System.Diagnostics.Debug.WriteLine($"Initializing position tracking for {windowKey}");

        // Restore position when the window loads
        window.Loaded += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"{windowKey} Loaded event - restoring position");
            RestorePosition(window, settings, windowKey);
        };

        // Save position when the window closes
        window.Closed += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"{windowKey} Closed event - saving position");
            SavePosition(window, settings, windowKey);
            // Save settings to disk
            var persistenceService = new SettingsPersistenceService();
            persistenceService.SaveSettings(settings);
            System.Diagnostics.Debug.WriteLine($"{windowKey} Settings saved to disk");
        };
    }

    /// <summary>
    /// Initialize window position tracking for windows without direct AppSettings access
    /// Loads and saves settings automatically
    /// </summary>
    public static void InitializePositionTracking(Window window, string windowKey)
    {
        System.Diagnostics.Debug.WriteLine($"Initializing position tracking (auto-load) for {windowKey}");
        var persistenceService = new SettingsPersistenceService();

        // Restore position when the window loads
        window.Loaded += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"{windowKey} Loaded event - loading and restoring position");
            var settings = persistenceService.LoadSettings();
            RestorePosition(window, settings, windowKey);
        };

        // Save position when the window closes
        window.Closed += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"{windowKey} Closed event - loading, saving position, and persisting");
            var settings = persistenceService.LoadSettings();
            SavePosition(window, settings, windowKey);
            persistenceService.SaveSettings(settings);
            System.Diagnostics.Debug.WriteLine($"{windowKey} Settings saved to disk");
        };
    }
}
