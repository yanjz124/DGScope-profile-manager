using System.Diagnostics;
using System.Net.Http;

namespace DGScopeProfileManager.Services;

/// <summary>
/// A selectable dSTARS data source (radar update server).
/// </summary>
public class DataSourcePreset
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;

    public override string ToString() => Name;
}

/// <summary>
/// Health state of a data source server.
/// </summary>
public enum ServerHealthState
{
    Unknown,
    Checking,
    Up,      // Reachable and streaming data
    NoData,  // Reachable (HTTP 200) but no aircraft data within the probe window
    Down     // Unreachable, timed out, or non-success status
}

/// <summary>
/// Result of a server health probe.
/// </summary>
public class ServerHealthResult
{
    public ServerHealthState State { get; set; } = ServerHealthState.Unknown;
    public int? LatencyMs { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Central configuration for the dSTARS data source (radar update server).
///
/// Every receiver URL follows the pattern <c>{baseUrl}/dstars/{FACILITY}/updates</c>.
/// The active base URL is app-global (all profiles share one server), so it is held
/// statically and initialized from settings at startup.
/// </summary>
public static class DataSourceService
{
    public const string OfficialBaseUrl = "https://dstars.graiani.com";
    public const string VncrccBaseUrl = "https://swim.vncrcc.org";

    /// <summary>Built-in server presets shown in the data source picker.</summary>
    public static readonly DataSourcePreset[] Presets =
    {
        new() { Name = "Official (graiani)", BaseUrl = OfficialBaseUrl },
        new() { Name = "VNCRCC (swim)", BaseUrl = VncrccBaseUrl },
    };

    /// <summary>
    /// The base URL used when generating new profiles. Set from settings at startup
    /// and whenever the user switches servers. Defaults to the official server for
    /// backward compatibility.
    /// </summary>
    public static string ActiveBaseUrl { get; set; } = OfficialBaseUrl;

    // Short-lived client; health probes use ResponseHeadersRead so they never buffer
    // the (infinite) update stream.
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan // per-call timeout is handled via CancellationToken
    };

    static DataSourceService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DGScope-Profile-Manager");
    }

    /// <summary>
    /// Normalize a base URL (trim trailing slashes/whitespace).
    /// </summary>
    public static string NormalizeBaseUrl(string baseUrl)
        => (baseUrl ?? string.Empty).Trim().TrimEnd('/');

    /// <summary>
    /// Build the receiver updates URL for a facility using the active base URL.
    /// </summary>
    public static string BuildUpdatesUrl(string facilityId)
        => BuildUpdatesUrl(ActiveBaseUrl, facilityId);

    /// <summary>
    /// Build the receiver updates URL for a facility against a specific base URL.
    /// </summary>
    public static string BuildUpdatesUrl(string baseUrl, string facilityId)
        => $"{NormalizeBaseUrl(baseUrl)}/dstars/{facilityId}/updates";

    /// <summary>
    /// Find the preset matching a base URL, or null if it's a custom URL.
    /// </summary>
    public static DataSourcePreset? FindPreset(string baseUrl)
    {
        var normalized = NormalizeBaseUrl(baseUrl);
        return Array.Find(Presets, p => string.Equals(NormalizeBaseUrl(p.BaseUrl), normalized,
            StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Probe a data source for availability and live data.
    ///
    /// The updates endpoint is an infinite streaming response, so this reads only
    /// the response headers plus the first chunk of the body, then disposes the stream.
    /// </summary>
    /// <param name="baseUrl">Server base URL.</param>
    /// <param name="facilityId">A facility to probe (e.g. "PCT").</param>
    /// <param name="timeoutMs">Overall probe timeout.</param>
    public static async Task<ServerHealthResult> CheckHealthAsync(
        string baseUrl, string facilityId, int timeoutMs = 8000, CancellationToken cancellationToken = default)
    {
        var url = BuildUpdatesUrl(baseUrl, facilityId);
        var sw = Stopwatch.StartNew();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            using var response = await _httpClient.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            sw.Stop();
            var latency = (int)sw.ElapsedMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                return new ServerHealthResult
                {
                    State = ServerHealthState.Down,
                    LatencyMs = latency,
                    Message = $"HTTP {(int)response.StatusCode}"
                };
            }

            // Read up to the first chunk of the stream to confirm data is flowing.
            using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            var buffer = new byte[1024];
            int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), timeoutCts.Token);

            if (read > 0)
            {
                return new ServerHealthResult
                {
                    State = ServerHealthState.Up,
                    LatencyMs = latency,
                    Message = $"Up ({latency} ms)"
                };
            }

            return new ServerHealthResult
            {
                State = ServerHealthState.NoData,
                LatencyMs = latency,
                Message = "Connected, no data"
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Caller cancelled (e.g. window closing) - bubble up as Unknown.
            return new ServerHealthResult { State = ServerHealthState.Unknown, Message = "Cancelled" };
        }
        catch (OperationCanceledException)
        {
            // Our timeout fired. A 200 stream that never sends a chunk also lands here;
            // treat it as down for the user's purposes (no usable data).
            return new ServerHealthResult { State = ServerHealthState.Down, Message = "Timed out" };
        }
        catch (Exception ex)
        {
            return new ServerHealthResult { State = ServerHealthState.Down, Message = ex.Message };
        }
    }
}
