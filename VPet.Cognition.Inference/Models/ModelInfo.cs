using System.Text.Json;

namespace VPet.Cognition.Inference.Models;

/// <summary>
/// Describes a model available for inference (local or remote).
/// </summary>
public sealed class ModelInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ModelCategory Category { get; set; } = ModelCategory.Text;
    public string? LocalPath { get; set; }
    public long? FileSizeBytes { get; set; }
    public int ContextWindow { get; set; } = 8192;
    public bool SupportsTools { get; set; }
    public bool SupportsStreaming { get; set; } = true;
    public bool SupportsJson { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsEmbedding { get; set; }
    public string? DownloadUrl { get; set; }
    public string? HuggingFaceRepo { get; set; }
    public string? Quantization { get; set; }
    public string? License { get; set; }
    public bool IsInstalled { get; set; }
    public string? InstalledVersion { get; set; }
    public string? LatestVersion { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
}

/// <summary>
/// Category of a model — determines which module uses it.
/// </summary>
public enum ModelCategory
{
    Text = 0,
    Vision = 1,
    Embedding = 2,
    Audio = 3,
    Reasoning = 4,
}

/// <summary>
/// An entry in the official model registry — curated, known-good models.
/// </summary>
public sealed class RegistryEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ModelCategory Category { get; set; }
    public string HuggingFaceRepo { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int ContextWindow { get; set; } = 8192;
    public bool SupportsTools { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsEmbedding { get; set; }
    public bool SupportsJson { get; set; }
    public bool SupportsStreaming { get; set; } = true;
    public string Quantization { get; set; } = "Q4_K_M";
    public string License { get; set; } = "Unknown";
    public string MinRamGb { get; set; } = "4GB";
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Download progress event.
/// </summary>
public sealed record DownloadProgressEvent(string ModelId, long DownloadedBytes, long TotalBytes, double Percentage, bool IsComplete, string? Error);