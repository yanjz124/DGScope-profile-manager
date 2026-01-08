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

        var position = new WindowPosition
        {
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            IsMaximized = window.WindowState == WindowState.Maximized
        };

        settings.WindowPositions[windowKey] = position;
    }

    /// <summary>
    /// Restore window position from saved settings
    /// </summary>
    public static void RestorePosition(Window window, AppSettings settings, string windowKey)
    {
        if (window == null || settings == null)
            return;

        if (!settings.WindowPositions.TryGetValue(windowKey, out var position))
            return;

        // Ensure the window is within screen bounds
        var screenWidth = SystemParameters.VirtualScreenWidth;
        var screenHeight = SystemParameters.VirtualScreenHeight;

        if (position.Left < 0 || position.Left + position.Width > screenWidth)
            position.Left = (screenWidth - position.Width) / 2;

        if (position.Top < 0 || position.Top + position.Height > screenHeight)
            position.Top = (screenHeight - position.Height) / 2;

        window.Left = position.Left;
        window.Top = position.Top;
        window.Width = position.Width;
        window.Height = position.Height;

        if (position.IsMaximized)
            window.WindowState = WindowState.Maximized;
    }

    /// <summary>
    /// Initialize window position tracking
    /// Sets up event handlers to save position on close
    /// </summary>
    public static void InitializePositionTracking(Window window, AppSettings settings, string windowKey)
    {
        // Restore position when the window loads
        window.Loaded += (s, e) => RestorePosition(window, settings, windowKey);

        // Save position when the window closes
        window.Closed += (s, e) =>
        {
            SavePosition(window, settings, windowKey);
            // Save settings to disk
            var persistenceService = new SettingsPersistenceService();
            persistenceService.SaveSettings(settings);
        };
    }

    /// <summary>
    /// Initialize window position tracking for windows without direct AppSettings access
    /// Loads and saves settings automatically
    /// </summary>
    public static void InitializePositionTracking(Window window, string windowKey)
    {
        var persistenceService = new SettingsPersistenceService();

        // Restore position when the window loads
        window.Loaded += (s, e) =>
        {
            var settings = persistenceService.LoadSettings();
            RestorePosition(window, settings, windowKey);
        };

        // Save position when the window closes
        window.Closed += (s, e) =>
        {
            var settings = persistenceService.LoadSettings();
            SavePosition(window, settings, windowKey);
            persistenceService.SaveSettings(settings);
        };
    }
}
