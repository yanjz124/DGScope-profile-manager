using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Service for checking and applying application updates from GitHub releases
/// </summary>
public class UpdateService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/yanjz124/DGScope-profile-manager/releases/latest";

    // Version is pulled from the assembly, set by MinVer from git tags
    private static readonly string CurrentVersion = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "0.0.0";

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static UpdateService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DGScope-Profile-Manager");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    /// <summary>
    /// Check if a newer version is available on GitHub
    /// </summary>
    /// <returns>Update info if available, null if no update or error</returns>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(GitHubApiUrl);

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"GitHub API returned {response.StatusCode}");
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                return null;
            }

            // Parse version from tag (e.g., "v1.2.4" -> "1.2.4")
            var latestVersion = release.TagName.TrimStart('v');

            if (!IsNewerVersion(latestVersion, CurrentVersion))
            {
                Debug.WriteLine($"Current version {CurrentVersion} is up to date (latest: {latestVersion})");
                return null;
            }

            // Find the installer asset (bundle setup exe)
            var installerAsset = release.Assets?.FirstOrDefault(a =>
                a.Name?.Contains("-bundle-Setup.exe", StringComparison.OrdinalIgnoreCase) == true);

            if (installerAsset == null)
            {
                // Fallback to any setup exe
                installerAsset = release.Assets?.FirstOrDefault(a =>
                    a.Name?.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase) == true);
            }

            if (installerAsset == null)
            {
                Debug.WriteLine("No installer asset found in release");
                return null;
            }

            return new UpdateInfo
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = latestVersion,
                ReleaseNotes = release.Body ?? "",
                DownloadUrl = installerAsset.BrowserDownloadUrl ?? "",
                InstallerFileName = installerAsset.Name ?? "DGScope-Profile-Manager-Setup.exe",
                ReleaseUrl = release.HtmlUrl ?? ""
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking for updates: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download and run the installer, then exit the application
    /// </summary>
    public async Task<bool> DownloadAndInstallAsync(UpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        try
        {
            // Download to temp folder
            var tempPath = Path.Combine(Path.GetTempPath(), updateInfo.InstallerFileName);

            using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var downloadedBytes = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            var percentComplete = (int)((downloadedBytes * 100) / totalBytes);
                            progress?.Report(percentComplete);
                        }
                    }
                }
            }

            // Close DGScope if running (may interfere with installer)
            CloseRunningDgScope();

            // Launch installer
            var startInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);

            // Exit application to allow installer to run
            Environment.Exit(0);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading/installing update: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Close any running DGScope instances that could interfere with the installer
    /// </summary>
    private static void CloseRunningDgScope()
    {
        try
        {
            var dgScopeProcesses = Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        return p.ProcessName.Contains("DGScope", StringComparison.OrdinalIgnoreCase)
                            && !p.ProcessName.Contains("ProfileManager", StringComparison.OrdinalIgnoreCase);
                    }
                    catch { return false; }
                })
                .ToList();

            foreach (var proc in dgScopeProcesses)
            {
                try
                {
                    Debug.WriteLine($"Closing DGScope process: {proc.ProcessName} (PID {proc.Id})");
                    proc.CloseMainWindow();
                    if (!proc.WaitForExit(3000))
                    {
                        proc.Kill();
                        proc.WaitForExit(2000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to close process {proc.ProcessName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error closing DGScope processes: {ex.Message}");
        }
    }

    /// <summary>
    /// Compare two version strings (e.g., "1.2.4" vs "1.2.3")
    /// </summary>
    private bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        try
        {
            var latest = Version.Parse(latestVersion);
            var current = Version.Parse(currentVersion);
            return latest > current;
        }
        catch
        {
            // Fallback to string comparison
            return string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }

    /// <summary>
    /// Get the current application version
    /// </summary>
    public static string GetCurrentVersion() => CurrentVersion;
}

/// <summary>
/// Information about an available update
/// </summary>
public class UpdateInfo
{
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string InstallerFileName { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
}

/// <summary>
/// GitHub Release API response
/// </summary>
internal class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}

/// <summary>
/// GitHub Release Asset
/// </summary>
internal class GitHubAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}
