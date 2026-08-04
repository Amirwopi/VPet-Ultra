using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VPet.Cognition.Host.Update;

/// <summary>
/// Checks for cognition plugin updates from GitHub releases.
/// Compares local version against the latest release and downloads
/// only the Plugin zip (not the full 914MB package) when an update is available.
/// </summary>
public sealed class UpdateChecker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateChecker> _logger;
    private readonly string _repoOwner;
    private readonly string _repoName;
    private readonly string _localVersionFile;

    public UpdateChecker(
        HttpClient httpClient,
        ILogger<UpdateChecker> logger,
        string repoOwner = "Amirwopi",
        string repoName = "VPet-Ultra",
        string localVersionFile = "mod/0000_cognition/version.txt")
    {
        _httpClient = httpClient;
        _logger = logger;
        _repoOwner = repoOwner;
        _repoName = repoName;
        _localVersionFile = localVersionFile;
    }

    /// <summary>Get the local installed version. Returns "0.0.0" if not found.</summary>
    public string GetLocalVersion()
    {
        try
        {
            if (File.Exists(_localVersionFile))
                return File.ReadAllText(_localVersionFile).Trim();
        }
        catch { /* ignore */ }
        return "0.0.0";
    }

    /// <summary>Check the latest release version from GitHub.</summary>
    public async Task<ReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "VPet-Ultra-UpdateChecker");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "v0.0.0";
            var version = tagName.TrimStart('v');

            var assets = new List<ReleaseAsset>();
            foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
            {
                assets.Add(new ReleaseAsset(
                    asset.GetProperty("name").GetString() ?? "",
                    asset.GetProperty("browser_download_url").GetString() ?? "",
                    asset.GetProperty("size").GetInt64()
                ));
            }

            return new ReleaseInfo(version, assets);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check for updates.");
            return null;
        }
    }

    /// <summary>Check if an update is available.</summary>
    public async Task<bool> IsUpdateAvailableAsync(CancellationToken ct = default)
    {
        var local = GetLocalVersion();
        var latest = await GetLatestReleaseAsync(ct);
        if (latest is null) return false;

        var comparison = CompareVersions(local, latest.Version);
        _logger.LogInformation("Update check: local={Local}, latest={Latest}, updateAvailable={Update}",
            local, latest.Version, comparison < 0);

        return comparison < 0;
    }

    /// <summary>
    /// Download the plugin-only update zip and extract it to the mod folder.
    /// Returns true if successful.
    /// </summary>
    public async Task<bool> DownloadAndUpdateAsync(string modPath, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        var latest = await GetLatestReleaseAsync(ct);
        if (latest is null) return false;

        var pluginAsset = latest.Assets.FirstOrDefault(a => a.Name.Contains("Plugin", StringComparison.OrdinalIgnoreCase));
        if (pluginAsset is null)
        {
            _logger.LogWarning("No plugin zip found in latest release.");
            return false;
        }

        try
        {
            _logger.LogInformation("Downloading update: {Url} ({Size} bytes)", pluginAsset.Url, pluginAsset.Size);

            var request = new HttpRequestMessage(HttpMethod.Get, pluginAsset.Url);
            request.Headers.Add("User-Agent", "VPet-Ultra-UpdateChecker");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var tempZip = Path.Combine(Path.GetTempPath(), $"vpet_cognition_update_{Guid.NewGuid():N}.zip");
            using (var fs = File.Create(tempZip))
            {
                await response.Content.CopyToAsync(fs, ct);
            }

            // Extract to temp directory
            var tempExtract = Path.Combine(Path.GetTempPath(), $"vpet_cognition_extract_{Guid.NewGuid():N}");
            System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, tempExtract, true);

            // Copy files to mod path
            var sourceMod = Path.Combine(tempExtract, "mod", "0000_cognition");
            if (!Directory.Exists(sourceMod))
                sourceMod = tempExtract; // fallback: files are at root

            foreach (var file in Directory.GetFiles(sourceMod, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceMod, file);
                var dest = Path.Combine(modPath, relative);
                var dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.Copy(file, dest, true);
            }

            // Update version file
            File.WriteAllText(Path.Combine(modPath, "version.txt"), latest.Version);

            // Cleanup
            try { File.Delete(tempZip); } catch { }
            try { Directory.Delete(tempExtract, true); } catch { }

            _logger.LogInformation("Update to {Version} completed successfully.", latest.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update download/extract failed.");
            return false;
        }
    }

    /// <summary>Compare two version strings. Returns -1, 0, or 1.</summary>
    private static int CompareVersions(string a, string b)
    {
        var partsA = a.Split('.').Select(s => int.TryParse(s, out var v) ? v : 0).ToArray();
        var partsB = b.Split('.').Select(s => int.TryParse(s, out var v) ? v : 0).ToArray();

        for (int i = 0; i < Math.Max(partsA.Length, partsB.Length); i++)
        {
            var va = i < partsA.Length ? partsA[i] : 0;
            var vb = i < partsB.Length ? partsB[i] : 0;
            if (va < vb) return -1;
            if (va > vb) return 1;
        }
        return 0;
    }
}

/// <summary>Info about a GitHub release.</summary>
public sealed record ReleaseInfo(string Version, IReadOnlyList<ReleaseAsset> Assets);

/// <summary>A downloadable asset in a release.</summary>
public sealed record ReleaseAsset(string Name, string Url, long Size);