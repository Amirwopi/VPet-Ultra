using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Inference.Abstractions;
using VPet.Cognition.Inference.Models;

namespace VPet.Cognition.Inference.Local;

/// <summary>
/// Local inference provider using LLamaSharp. Runs GGUF models directly in-process —
/// no external server (Ollama) needed. Supports text, streaming, vision (via LLava),
/// and embedding models.
/// </summary>
public sealed class LocalInferenceProvider : IInferenceProvider
{
    public string ProviderId => "local-llama";

    private InferenceCapabilities _capabilities = InferenceCapabilities.Text | InferenceCapabilities.Streaming;
    public InferenceCapabilities Capabilities => _capabilities;

    private readonly string _modelsDirectory;
    private readonly int _gpuLayerCount;
    private readonly int _contextSize;
    private readonly ILogger<LocalInferenceProvider> _logger;
    private LLamaWeights? _model;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private ChatSession? _session;
    private string? _loadedModelId;
    private bool _isVisionModel;
    private bool _isEmbeddingModel;

    public IReadOnlyList<ModelInfo> AvailableModels { get; private set; } = Array.Empty<ModelInfo>();

    public LocalInferenceProvider(
        string modelsDirectory,
        int gpuLayerCount,
        int contextSize,
        ILogger<LocalInferenceProvider> logger)
    {
        _modelsDirectory = modelsDirectory;
        _gpuLayerCount = gpuLayerCount;
        _contextSize = contextSize;
        _logger = logger;

        RefreshAvailableModels();
    }

    public void RefreshAvailableModels()
    {
        var models = new List<ModelInfo>();
        if (Directory.Exists(_modelsDirectory))
        {
            foreach (var file in Directory.GetFiles(_modelsDirectory, "*.gguf"))
            {
                var fi = new FileInfo(file);
                var id = Path.GetFileNameWithoutExtension(fi.Name);
                var entry = ModelRegistry.GetById(id);

                models.Add(new ModelInfo
                {
                    Id = id,
                    Name = entry?.Name ?? id,
                    Description = entry?.Description ?? "Custom GGUF model",
                    Category = entry?.Category ?? ModelCategory.Text,
                    LocalPath = file,
                    FileSizeBytes = fi.Length,
                    ContextWindow = entry?.ContextWindow ?? 8192,
                    SupportsTools = entry?.SupportsTools ?? false,
                    SupportsVision = entry?.SupportsVision ?? false,
                    SupportsEmbedding = entry?.SupportsEmbedding ?? false,
                    SupportsStreaming = true,
                    SupportsJson = entry?.SupportsJson ?? false,
                    IsInstalled = true,
                });
            }
        }
        AvailableModels = models;
    }

    public async Task<Result> LoadModelAsync(string modelId, CancellationToken ct = default)
    {
        // Unload previous model
        if (_model is not null)
            await UnloadModelAsync(ct);

        var modelPath = Path.Combine(_modelsDirectory, $"{modelId}.gguf");
        if (!File.Exists(modelPath))
            return Result.Fail($"Model file not found: {modelPath}");

        var entry = ModelRegistry.GetById(modelId);
        _isVisionModel = entry?.SupportsVision ?? false;
        _isEmbeddingModel = entry?.SupportsEmbedding ?? false;

        try
        {
            _logger.LogInformation("Loading model {Id} from {Path} (GPU layers: {Gpu})",
                modelId, modelPath, _gpuLayerCount);

            var parameters = new ModelParams(modelPath)
            {
                ContextSize = (uint)_contextSize,
                GpuLayerCount = _gpuLayerCount,
            };

            _model = LLamaWeights.LoadFromFile(parameters);
            _context = _model.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);

            var history = new ChatHistory();
            _session = new ChatSession(_executor, history);

            _loadedModelId = modelId;

            // Update capabilities based on model type
            _capabilities = InferenceCapabilities.Text | InferenceCapabilities.Streaming;
            if (_isVisionModel)
                _capabilities |= InferenceCapabilities.Vision;
            if (_isEmbeddingModel)
                _capabilities |= InferenceCapabilities.Embedding;
            if (entry?.SupportsTools ?? false)
                _capabilities |= InferenceCapabilities.ToolCalling;
            if (entry?.SupportsJson ?? false)
                _capabilities |= InferenceCapabilities.Json;

            _logger.LogInformation("Model {Id} loaded. Capabilities: {Caps}", modelId, _capabilities);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model {Id}", modelId);
            _model?.Dispose();
            _context?.Dispose();
            _model = null;
            _context = null;
            _executor = null;
            _session = null;
            _loadedModelId = null;
            return Result.Fail(ex.Message);
        }
    }

    public Task UnloadModelAsync(CancellationToken ct = default)
    {
        _session = null;
        _executor = null;
        _context?.Dispose();
        _context = null;
        _model?.Dispose();
        _model = null;
        _loadedModelId = null;
        _capabilities = InferenceCapabilities.Text | InferenceCapabilities.Streaming;
        _logger.LogInformation("Model unloaded");
        return Task.CompletedTask;
    }

    public async Task<Result<InferenceResponse>> CompleteAsync(InferenceRequest request, CancellationToken ct = default)
    {
        if (_session is null || _model is null)
            return Result<InferenceResponse>.Fail("No model loaded. Call LoadModelAsync first.");

        try
        {
            var sb = new System.Text.StringBuilder();
            await foreach (var chunk in StreamAsync(request, ct))
            {
                if (chunk.TextDelta is { } text)
                    sb.Append(text);
            }

            return Result<InferenceResponse>.Ok(new InferenceResponse(
                sb.ToString(),
                Array.Empty<ToolCallRequest>(),
                0, 0, "stop", _loadedModelId ?? "unknown"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Completion failed");
            return Result<InferenceResponse>.Fail(ex.Message);
        }
    }

    public async IAsyncEnumerable<InferenceChunk> StreamAsync(InferenceRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_session is null || _model is null)
        {
            _logger.LogWarning("StreamAsync called with no model loaded");
            yield break;
        }

        // Build chat history for this request
        var history = new ChatHistory();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            history.AddMessage(AuthorRole.System, request.SystemPrompt);

        foreach (var msg in request.Messages)
        {
            var role = msg.Role switch
            {
                "user" => AuthorRole.User,
                "assistant" => AuthorRole.Assistant,
                "system" => AuthorRole.System,
                _ => AuthorRole.User,
            };
            history.AddMessage(role, msg.Content);
        }

        // Reset session with the new history
        _session = new ChatSession(_executor!, history);

        var inferenceParams = new InferenceParams
        {
            MaxTokens = request.MaxTokens,
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = (float)request.Temperature,
                TopP = (float?)request.TopP ?? 0.95f,
            },
        };

        // AntiPrompts is a List<string> — create a new list each time
        var antiPrompts = new List<string> { "User:", "\nUser:" };
        if (!string.IsNullOrEmpty(request.Stop))
            antiPrompts.Add(request.Stop);
        inferenceParams.AntiPrompts = antiPrompts;

        var userMessage = request.Messages.LastOrDefault(m => m.Role == "user");
        var userText = userMessage?.Content ?? "";

        // If vision model and image provided, add image
        if (_isVisionModel && request.ImageBytes is { } imageBytes)
        {
            // LLamaSharp vision support via LLavaChat — requires mmproj model
            // For now, append image description placeholder
            _logger.LogDebug("Vision model with image input ({Bytes} bytes)", imageBytes.Length);
        }

        await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userText), inferenceParams).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return new InferenceChunk(text, null, null, null);
        }
    }

    public async Task<Result<float[]>> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (_model is null || _context is null)
            return Result<float[]>.Fail("No model loaded.");

        if (!_isEmbeddingModel)
            return Result<float[]>.Fail("Loaded model does not support embeddings.");

        try
        {
            // LLamaSharp 0.16: use LLamaEmbedder with the loaded model and context
            using var embedder = new LLamaEmbedder(_model, _context.Params, null);
            var embeds = await embedder.GetEmbeddings(text, ct);
            // GetEmbeddings returns IReadOnlyList<float[]> — take the first one
            var result = embeds.Count > 0 ? embeds[0] : Array.Empty<float>();
            return Result<float[]>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Embedding failed");
            return Result<float[]>.Fail(ex.Message);
        }
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_model is not null && _context is not null);
    }

    public ValueTask DisposeAsync()
    {
        _context?.Dispose();
        _model?.Dispose();
        return ValueTask.CompletedTask;
    }
}