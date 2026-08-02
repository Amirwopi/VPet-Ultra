namespace VPet.Cognition.Inference.Models;

/// <summary>
/// The official curated model registry. Each entry is a known-good GGUF model
/// that can be downloaded and used directly. New models can be added here without
/// changing any inference code — the provider abstraction handles everything.
/// </summary>
public static class ModelRegistry
{
    /// <summary>
    /// All officially supported models. Users can search, download, and install
    /// any of these from within the app.
    /// </summary>
    public static readonly IReadOnlyList<RegistryEntry> Entries = new List<RegistryEntry>
    {
        // === Text Models ===
        new()
        {
            Id = "qwen2.5-3b-instruct-q4_k_m",
            Name = "Qwen2.5-3B Instruct",
            Description = "Compact multilingual chat model. Good balance of quality and speed. Supports tool calling.",
            Category = ModelCategory.Text,
            HuggingFaceRepo = "Qwen/Qwen2.5-3B-Instruct-GGUF",
            FileName = "qwen2.5-3b-instruct-q4_k_m.gguf",
            FileSizeBytes = 2_000_000_000,
            ContextWindow = 32768,
            SupportsTools = true,
            SupportsStreaming = true,
            SupportsJson = true,
            Quantization = "Q4_K_M",
            License = "Apache-2.0",
            MinRamGb = "4GB",
            Tags = { "multilingual", "chat", "tools", "persian", "chinese", "english" },
        },
        new()
        {
            Id = "qwen2.5-7b-instruct-q4_k_m",
            Name = "Qwen2.5-7B Instruct",
            Description = "Higher quality multilingual chat model. Requires more RAM.",
            Category = ModelCategory.Text,
            HuggingFaceRepo = "Qwen/Qwen2.5-7B-Instruct-GGUF",
            FileName = "qwen2.5-7b-instruct-q4_k_m.gguf",
            FileSizeBytes = 4_700_000_000,
            ContextWindow = 32768,
            SupportsTools = true,
            SupportsStreaming = true,
            SupportsJson = true,
            Quantization = "Q4_K_M",
            License = "Apache-2.0",
            MinRamGb = "8GB",
            Tags = { "multilingual", "chat", "tools", "high-quality" },
        },
        new()
        {
            Id = "qwen2.5-1.5b-instruct-q4_k_m",
            Name = "Qwen2.5-1.5B Instruct",
            Description = "Ultra-light chat model for very weak hardware.",
            Category = ModelCategory.Text,
            HuggingFaceRepo = "Qwen/Qwen2.5-1.5B-Instruct-GGUF",
            FileName = "qwen2.5-1.5b-instruct-q4_k_m.gguf",
            FileSizeBytes = 1_000_000_000,
            ContextWindow = 32768,
            SupportsTools = true,
            SupportsStreaming = true,
            SupportsJson = true,
            Quantization = "Q4_K_M",
            License = "Apache-2.0",
            MinRamGb = "2GB",
            Tags = { "multilingual", "chat", "lightweight", "low-ram" },
        },
        new()
        {
            Id = "llama-3.2-3b-instruct-q4_k_m",
            Name = "Llama 3.2 3B Instruct",
            Description = "Meta's compact instruction-tuned model. Good English quality.",
            Category = ModelCategory.Text,
            HuggingFaceRepo = "bartowski/Llama-3.2-3B-Instruct-GGUF",
            FileName = "Llama-3.2-3B-Instruct-Q4_K_M.gguf",
            FileSizeBytes = 2_000_000_000,
            ContextWindow = 131072,
            SupportsTools = true,
            SupportsStreaming = true,
            SupportsJson = true,
            Quantization = "Q4_K_M",
            License = "Llama-3.2-License",
            MinRamGb = "4GB",
            Tags = { "english", "chat", "tools", "long-context" },
        },

        // === Vision Models ===
        new()
        {
            Id = "smolvlm-500m-q4_k_m",
            Name = "SmolVLM 500M",
            Description = "Compact vision-language model. Can describe images and answer questions about them.",
            Category = ModelCategory.Vision,
            HuggingFaceRepo = "bartowski/SmolVLM-500M-Instruct-GGUF",
            FileName = "SmolVLM-500M-Instruct-Q4_K_M.gguf",
            FileSizeBytes = 500_000_000,
            ContextWindow = 8192,
            SupportsVision = true,
            SupportsStreaming = true,
            Quantization = "Q4_K_M",
            License = "Apache-2.0",
            MinRamGb = "2GB",
            Tags = { "vision", "image", "lightweight", "multimodal" },
        },
        new()
        {
            Id = "smolvlm-2.2b-q4_k_m",
            Name = "SmolVLM 2.2B",
            Description = "Higher quality vision-language model. Better image understanding.",
            Category = ModelCategory.Vision,
            HuggingFaceRepo = "bartowski/SmolVLM-2.2B-Instruct-GGUF",
            FileName = "SmolVLM-2.2B-Instruct-Q4_K_M.gguf",
            FileSizeBytes = 1_500_000_000,
            ContextWindow = 8192,
            SupportsVision = true,
            SupportsStreaming = true,
            Quantization = "Q4_K_M",
            License = "Apache-2.0",
            MinRamGb = "4GB",
            Tags = { "vision", "image", "multimodal", "high-quality" },
        },
        new()
        {
            Id = "llava-v1.6-7b-q4_k_m",
            Name = "LLaVA v1.6 7B",
            Description = "Full vision-language model with high-quality image understanding.",
            Category = ModelCategory.Vision,
            HuggingFaceRepo = "bartowski/llava-v1.6-vicuna-7B-GGUF",
            FileName = "llava-v1.6-vicuna-7B-Q4_K_M.gguf",
            FileSizeBytes = 4_300_000_000,
            ContextWindow = 4096,
            SupportsVision = true,
            SupportsStreaming = true,
            Quantization = "Q4_K_M",
            License = "Apache-2.0",
            MinRamGb = "8GB",
            Tags = { "vision", "image", "multimodal", "high-quality" },
        },

        // === Embedding Models ===
        new()
        {
            Id = "bge-small-en-v1.5-q8_0",
            Name = "BGE Small EN v1.5",
            Description = "Compact English embedding model for semantic search and memory.",
            Category = ModelCategory.Embedding,
            HuggingFaceRepo = "CompendiumLabs/bge-small-en-v1.5-gguf",
            FileName = "bge-small-en-v1.5-q8_0.gguf",
            FileSizeBytes = 130_000_000,
            ContextWindow = 512,
            SupportsEmbedding = true,
            Quantization = "Q8_0",
            License = "MIT",
            MinRamGb = "1GB",
            Tags = { "embedding", "english", "semantic-search", "memory" },
        },
        new()
        {
            Id = "bge-m3-q8_0",
            Name = "BGE M3",
            Description = "Multilingual embedding model. Supports 100+ languages including Persian.",
            Category = ModelCategory.Embedding,
            HuggingFaceRepo = "CompendiumLabs/bge-m3-gguf",
            FileName = "bge-m3-q8_0.gguf",
            FileSizeBytes = 600_000_000,
            ContextWindow = 8192,
            SupportsEmbedding = true,
            Quantization = "Q8_0",
            License = "MIT",
            MinRamGb = "2GB",
            Tags = { "embedding", "multilingual", "persian", "semantic-search", "memory" },
        },
    };

    /// <summary>
    /// Search the registry by text query (matches name, description, tags).
    /// </summary>
    public static IReadOnlyList<RegistryEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Entries.ToList();

        var lower = query.ToLowerInvariant();
        return Entries
            .Where(e => e.Name.ToLowerInvariant().Contains(lower)
                     || e.Description.ToLowerInvariant().Contains(lower)
                     || e.Tags.Any(t => t.Contains(lower, StringComparison.OrdinalIgnoreCase))
                     || e.Category.ToString().ToLowerInvariant().Contains(lower))
            .ToList();
    }

    /// <summary>
    /// Get all models in a category.
    /// </summary>
    public static IReadOnlyList<RegistryEntry> GetByCategory(ModelCategory category)
        => Entries.Where(e => e.Category == category).ToList();

    /// <summary>
    /// Get a registry entry by id.
    /// </summary>
    public static RegistryEntry? GetById(string id)
        => Entries.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// Get the HuggingFace download URL for a registry entry.
    /// </summary>
    public static string GetDownloadUrl(RegistryEntry entry)
        => $"https://huggingface.co/{entry.HuggingFaceRepo}/resolve/main/{entry.FileName}";
}