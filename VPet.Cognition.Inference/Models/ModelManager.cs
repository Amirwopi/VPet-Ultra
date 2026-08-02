using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Inference.Abstractions;
using VPet.Cognition.Inference.Models;

namespace VPet.Cognition.Inference.Models;

/// <summary>
/// Manages model files on disk: search registry, download from HuggingFace,
/// install, update, delete, and list installed models.
/// </summary>
public sealed class ModelManager : IAsyncDisposable
{
    private readonly string _modelsDirectory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ModelManager> _logger;
    private readonly SemaphoreSlim _downloadLock = new(1, 1);

    /// <summary>Raised when a download progresses.</summary>
    public event Action<DownloadProgressEvent>? DownloadProgress;

    public ModelManager(string modelsDirectory, ILogger<ModelManager> logger, HttpClient? httpClient = null)
    {
        _modelsDirectory = modelsDirectory;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromHours(1) };

        if (!Directory.Exists(_modelsDirectory))
            Directory.CreateDirectory(_modelsDirectory);
    }

    /// <summary>
    /// Search the official registry for models.
    /// </summary>
    public IReadOnlyList<RegistryEntry> SearchRegistry(string query)
        => ModelRegistry.Search(query);

    /// <summary>
    /// Get all models in a category from the registry.
    /// </summary>
    public IReadOnlyList<RegistryEntry> GetRegistryByCategory(ModelCategory category)
        => ModelRegistry.GetByCategory(category);

    /// <summary>
    /// List all installed models on disk.
    /// </summary>
    public IReadOnlyList<ModelInfo> ListInstalled()
    {
        var result = new List<ModelInfo>();
        if (!Directory.Exists(_modelsDirectory))
            return result;

        foreach (var file in Directory.GetFiles(_modelsDirectory, "*.gguf"))
        {
            var fi = new FileInfo(file);
            var id = Path.GetFileNameWithoutExtension(fi.Name);
            var registryEntry = ModelRegistry.GetById(id);

            var info = new ModelInfo
            {
                Id = id,
                Name = registryEntry?.Name ?? id,
                Description = registryEntry?.Description ?? "Custom model",
                Category = registryEntry?.Category ?? ModelCategory.Text,
                LocalPath = file,
                FileSizeBytes = fi.Length,
                ContextWindow = registryEntry?.ContextWindow ?? 8192,
                SupportsTools = registryEntry?.SupportsTools ?? false,
                SupportsVision = registryEntry?.SupportsVision ?? false,
                SupportsEmbedding = registryEntry?.SupportsEmbedding ?? false,
                SupportsStreaming = true,
                SupportsJson = registryEntry?.SupportsJson ?? false,
                Quantization = registryEntry?.Quantization,
                License = registryEntry?.License,
                IsInstalled = true,
                LastUpdated = fi.LastWriteTimeUtc,
            };
            result.Add(info);
        }

        return result;
    }

    /// <summary>
    /// Check if a model is installed.
    /// </summary>
    public bool IsInstalled(string modelId)
    {
        var path = GetModelPath(modelId);
        return File.Exists(path);
    }

    /// <summary>
    /// Get the full path for a model file.
    /// </summary>
    public string GetModelPath(string modelId)
        => Path.Combine(_modelsDirectory, $"{modelId}.gguf");

    /// <summary>
    /// Download and install a model from the registry.
    /// Reports progress via <see cref="DownloadProgress"/>.
    /// </summary>
    public async Task<Result> DownloadAsync(string modelId, CancellationToken ct = default)
    {
        var entry = ModelRegistry.GetById(modelId);
        if (entry is null)
            return Result.Fail($"Model '{modelId}' not found in registry.");

        await _downloadLock.WaitAsync(ct);
        try
        {
            var targetPath = GetModelPath(modelId);
            if (File.Exists(targetPath))
            {
                _logger.LogInformation("Model {Id} already installed at {Path}", modelId, targetPath);
                return Result.Ok();
            }

            var url = ModelRegistry.GetDownloadUrl(entry);
            _logger.LogInformation("Downloading model {Id} from {Url}", modelId, url);

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return Result.Fail($"Download failed: HTTP {(int)response.StatusCode}");

            var totalBytes = response.Content.Headers.ContentLength ?? entry.FileSizeBytes;
            long downloadedBytes = 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(targetPath);

            var buffer = new byte[81920];
            int read;
            var sw = Stopwatch.StartNew();
            while ((read = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                downloadedBytes += read;

                if (sw.ElapsedMilliseconds > 200)
                {
                    sw.Restart();
                    var pct = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;
                    DownloadProgress?.Invoke(new DownloadProgressEvent(modelId, downloadedBytes, totalBytes, pct, false, null));
                }
            }

            DownloadProgress?.Invoke(new DownloadProgressEvent(modelId, downloadedBytes, totalBytes, 100, true, null));
            _logger.LogInformation("Model {Id} downloaded: {Bytes} bytes", modelId, downloadedBytes);

            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            // Clean up partial download
            var path = GetModelPath(modelId);
            if (File.Exists(path)) File.Delete(path);
            _logger.LogInformation("Download of {Id} cancelled", modelId);
            return Result.Fail("Download cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download model {Id}", modelId);
            DownloadProgress?.Invoke(new DownloadProgressEvent(modelId, 0, 0, 0, false, ex.Message));
            return Result.Fail(ex.Message);
        }
        finally
        {
            _downloadLock.Release();
        }
    }

    /// <summary>
    /// Delete an installed model from disk.
    /// </summary>
    public Task<Result> DeleteAsync(string modelId, CancellationToken ct = default)
    {
        var path = GetModelPath(modelId);
        if (!File.Exists(path))
            return Task.FromResult(Result.Fail($"Model '{modelId}' is not installed."));

        try
        {
            File.Delete(path);
            _logger.LogInformation("Model {Id} deleted", modelId);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete model {Id}", modelId);
            return Task.FromResult(Result.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Update a model — delete the old version and download the latest.
    /// </summary>
    public async Task<Result> UpdateAsync(string modelId, CancellationToken ct = default)
    {
        var deleteResult = await DeleteAsync(modelId, ct);
        if (!deleteResult.IsSuccess)
            return deleteResult;

        return await DownloadAsync(modelId, ct);
    }

    /// <summary>
    /// Get the total disk space used by installed models.
    /// </summary>
    public long GetTotalDiskUsage()
    {
        if (!Directory.Exists(_modelsDirectory))
            return 0;

        return Directory.GetFiles(_modelsDirectory, "*.gguf")
            .Sum(f => new FileInfo(f).Length);
    }

    /// <summary>
    /// Get the disk space used by a specific model.
    /// </summary>
    public long GetModelDiskUsage(string modelId)
    {
        var path = GetModelPath(modelId);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        _downloadLock.Dispose();
        return ValueTask.CompletedTask;
    }
}