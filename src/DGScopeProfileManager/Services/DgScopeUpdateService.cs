using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;

namespace DGScopeProfileManager.Services;

/// <summary>
/// Checks for and applies updates to the bundled DGScope (the "scope" folder), independently of
/// the Profile Manager's own version. DGScope is published as GitHub releases on yanjz124/scope;
/// its portable ZIP is extracted in place over the existing scope folder.
/// </summary>
public class DgScopeUpdateService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/yanjz124/scope/releases/latest";

    /// <summary>
    /// Filename of the version marker written into the scope folder. Records the exact release tag
    /// so alpha/pre-release versions can be compared reliably (scope.exe's file version cannot).
    /// </summary>
    public const string VersionMarkerFileName = ".dgscope-version";

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    static DgScopeUpdateService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DGScope-Profile-Manager");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    /// <summary>
    /// Read the installed DGScope version. Prefers the version marker (exact release tag); falls
    /// back to scope.exe's file version. Returns null if neither is available.
    /// </summary>
    public string? GetInstalledVersion(string scopeExePath)
    {
        try
        {
            var scopeDir = GetScopeFolder(scopeExePath);
            if (scopeDir != null)
            {
                var markerPath = Path.Combine(scopeDir, VersionMarkerFileName);
                if (File.Exists(markerPath))
                {
                    var marker = File.ReadAllText(markerPath).Trim();
                    if (!string.IsNullOrWhiteSpace(marker))
                        return marker;
                }
            }

            if (!string.IsNullOrWhiteSpace(scopeExePath) && File.Exists(scopeExePath))
            {
                var info = FileVersionInfo.GetVersionInfo(scopeExePath);
                var version = info.ProductVersion ?? info.FileVersion;
                if (!string.IsNullOrWhiteSpace(version))
                    return version.Trim();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading installed DGScope version: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Query GitHub for the latest DGScope release and decide whether it is newer than what is
    /// installed. Returns update info when an update is available, otherwise null (including when
    /// the installed version is unknown, so existing installs are not nagged with false prompts).
    /// </summary>
    public async Task<DgScopeUpdateInfo?> CheckForUpdatesAsync(string scopeExePath)
    {
        try
        {
            var response = await _httpClient.GetAsync(GitHubApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"scope releases API returned {response.StatusCode}");
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>();
            if (release == null || string.IsNullOrEmpty(release.TagName))
                return null;

            var latestTag = release.TagName;
            var installed = GetInstalledVersion(scopeExePath);

            // Unknown installed version -> can't tell if there's something new, so don't prompt.
            // The manual "Check for DGScope Updates" flow still surfaces the latest version.
            if (string.IsNullOrWhiteSpace(installed) || !IsNewerVersion(latestTag, installed))
                return null;

            // Prefer the portable ZIP; it extracts straight over the scope folder.
            var zipAsset = release.Assets?.FirstOrDefault(a =>
                a.Name?.Contains("portable", StringComparison.OrdinalIgnoreCase) == true &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                ?? release.Assets?.FirstOrDefault(a =>
                    a.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true);

            if (zipAsset?.BrowserDownloadUrl == null)
            {
                Debug.WriteLine("No portable ZIP asset found in scope release");
                return null;
            }

            return new DgScopeUpdateInfo
            {
                InstalledVersion = installed!,
                LatestVersion = latestTag,
                ReleaseNotes = release.Body ?? "",
                DownloadUrl = zipAsset.BrowserDownloadUrl,
                AssetFileName = zipAsset.Name ?? "DGScope-Portable.zip",
                ReleaseUrl = release.HtmlUrl ?? "",
                ScopeExePath = scopeExePath
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking for DGScope updates: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download the portable ZIP and extract it in place over the existing scope folder, closing
    /// any running DGScope first. Writes the version marker on success. Unlike the Profile Manager
    /// updater, this does not restart the app. Returns true on success.
    /// </summary>
    public async Task<bool> DownloadAndApplyAsync(DgScopeUpdateInfo updateInfo, IProgress<int>? progress = null)
    {
        string? tempZip = null;
        string? tempExtract = null;
        try
        {
            var scopeDir = GetScopeFolder(updateInfo.ScopeExePath);
            if (scopeDir == null)
            {
                Debug.WriteLine("Cannot determine scope folder for update");
                return false;
            }

            // Download the ZIP to a temp file with progress reporting.
            tempZip = Path.Combine(Path.GetTempPath(), updateInfo.AssetFileName);
            using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;
                    if (totalBytes > 0)
                        progress?.Report((int)((downloadedBytes * 100) / totalBytes));
                }
            }

            // Extract to a temp folder so we can locate the real content root before overwriting.
            tempExtract = Path.Combine(Path.GetTempPath(), "dgscope-update-" + Path.GetFileNameWithoutExtension(updateInfo.AssetFileName));
            if (Directory.Exists(tempExtract))
                Directory.Delete(tempExtract, recursive: true);
            ZipFile.ExtractToDirectory(tempZip, tempExtract);

            // The ZIP may wrap the files in a folder (e.g. "scope/"); find the dir containing scope.exe.
            var sourceRoot = FindContentRoot(tempExtract);
            if (sourceRoot == null)
            {
                Debug.WriteLine("Extracted DGScope archive did not contain scope.exe");
                return false;
            }

            // Close any running DGScope so its files aren't locked during the copy.
            CloseRunningDgScope(updateInfo.ScopeExePath);

            Directory.CreateDirectory(scopeDir);
            CopyDirectory(sourceRoot, scopeDir);

            // Record the exact installed tag for reliable future comparisons.
            File.WriteAllText(Path.Combine(scopeDir, VersionMarkerFileName), updateInfo.LatestVersion);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error applying DGScope update: {ex.Message}");
            return false;
        }
        finally
        {
            try { if (tempZip != null && File.Exists(tempZip)) File.Delete(tempZip); } catch { }
            try { if (tempExtract != null && Directory.Exists(tempExtract)) Directory.Delete(tempExtract, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Resolve the scope folder from a scope.exe path.
    /// </summary>
    private static string? GetScopeFolder(string scopeExePath)
    {
        if (string.IsNullOrWhiteSpace(scopeExePath))
            return null;
        return Path.GetDirectoryName(scopeExePath);
    }

    /// <summary>
    /// Find the directory within an extracted archive that contains scope.exe.
    /// </summary>
    private static string? FindContentRoot(string extractRoot)
    {
        if (File.Exists(Path.Combine(extractRoot, "scope.exe")))
            return extractRoot;

        var exe = Directory.EnumerateFiles(extractRoot, "scope.exe", SearchOption.AllDirectories).FirstOrDefault();
        return exe != null ? Path.GetDirectoryName(exe) : null;
    }

    /// <summary>
    /// Recursively copy the contents of one directory into another, overwriting existing files.
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }

    /// <summary>
    /// Close any running DGScope instance launched from the given executable so its files can be
    /// overwritten. Matches by process name, confirming the executable path when accessible.
    /// </summary>
    private static void CloseRunningDgScope(string scopeExePath)
    {
        try
        {
            var targetName = Path.GetFileNameWithoutExtension(scopeExePath);
            if (string.IsNullOrWhiteSpace(targetName))
                return;

            var fullTarget = Path.GetFullPath(scopeExePath);

            foreach (var proc in Process.GetProcessesByName(targetName))
            {
                try
                {
                    // If we can read the module path, only close the instance from our scope folder.
                    string? modulePath = null;
                    try { modulePath = proc.MainModule?.FileName; } catch { /* access denied - fall through */ }

                    if (modulePath != null &&
                        !string.Equals(Path.GetFullPath(modulePath), fullTarget, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    proc.CloseMainWindow();
                    if (!proc.WaitForExit(3000))
                    {
                        proc.Kill();
                        proc.WaitForExit(2000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to close DGScope process {proc.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error closing DGScope processes: {ex.Message}");
        }
    }

    /// <summary>
    /// SemVer-aware comparison of two release tags (e.g. "v0.0.3-alpha4" vs "v0.0.3-alpha3").
    /// Returns true if <paramref name="latest"/> is newer than <paramref name="installed"/>.
    /// Falls back to a plain string inequality when the tags cannot be parsed.
    /// </summary>
    public static bool IsNewerVersion(string latest, string installed)
    {
        var l = NormalizeTag(latest);
        var i = NormalizeTag(installed);

        if (string.Equals(l, i, StringComparison.OrdinalIgnoreCase))
            return false;

        var cmp = CompareSemVer(l, i);
        if (cmp.HasValue)
            return cmp.Value > 0;

        // Unparseable - any difference from the newest published release is treated as an update.
        return !string.Equals(l, i, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTag(string tag) => tag.Trim().TrimStart('v', 'V');

    /// <summary>
    /// Compare two SemVer-ish strings. Returns &gt;0 if a &gt; b, 0 if equal, &lt;0 if a &lt; b,
    /// or null if either could not be parsed into a numeric core.
    /// </summary>
    private static int? CompareSemVer(string a, string b)
    {
        var (coreA, preA) = SplitPrerelease(a);
        var (coreB, preB) = SplitPrerelease(b);

        if (!TryParseCore(coreA, out var va) || !TryParseCore(coreB, out var vb))
            return null;

        var coreCmp = va.CompareTo(vb);
        if (coreCmp != 0)
            return coreCmp;

        // Equal cores: a version with no prerelease outranks one with a prerelease (release > alpha).
        if (preA.Length == 0 && preB.Length == 0) return 0;
        if (preA.Length == 0) return 1;
        if (preB.Length == 0) return -1;

        return ComparePrerelease(preA, preB);
    }

    private static (string core, string pre) SplitPrerelease(string version)
    {
        var dash = version.IndexOf('-');
        return dash < 0
            ? (version, string.Empty)
            : (version[..dash], version[(dash + 1)..]);
    }

    private static bool TryParseCore(string core, out Version version)
    {
        // Pad to at least major.minor so "1" or "1.2" parse.
        var parts = core.Split('.');
        var padded = parts.Length switch
        {
            1 => core + ".0.0",
            2 => core + ".0",
            _ => core
        };
        return Version.TryParse(padded, out version!);
    }

    /// <summary>
    /// Compare dot-separated prerelease identifiers per SemVer precedence: numeric identifiers
    /// compare numerically, others lexically, and a larger set of fields wins when otherwise equal.
    /// </summary>
    private static int ComparePrerelease(string a, string b)
    {
        var fa = a.Split('.');
        var fb = b.Split('.');
        var n = Math.Max(fa.Length, fb.Length);

        for (int k = 0; k < n; k++)
        {
            if (k >= fa.Length) return -1;
            if (k >= fb.Length) return 1;

            var ia = SplitIdentifier(fa[k]);
            var ib = SplitIdentifier(fb[k]);

            // Compare the alphabetic prefix first (e.g. "alpha" vs "beta").
            var prefixCmp = string.Compare(ia.text, ib.text, StringComparison.OrdinalIgnoreCase);
            if (prefixCmp != 0) return Math.Sign(prefixCmp);

            if (ia.number != ib.number) return ia.number.CompareTo(ib.number);
        }

        return 0;
    }

    /// <summary>
    /// Split a prerelease identifier such as "alpha4" into its text prefix ("alpha") and trailing
    /// number (4), so "alpha10" sorts after "alpha4".
    /// </summary>
    private static (string text, long number) SplitIdentifier(string identifier)
    {
        int split = identifier.Length;
        while (split > 0 && char.IsDigit(identifier[split - 1]))
            split--;

        var text = identifier[..split];
        var numberPart = identifier[split..];
        long.TryParse(numberPart, out var number);
        return (text, number);
    }
}

/// <summary>
/// Information about an available DGScope update.
/// </summary>
public class DgScopeUpdateInfo
{
    public string InstalledVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string AssetFileName { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
    public string ScopeExePath { get; set; } = "";
}
