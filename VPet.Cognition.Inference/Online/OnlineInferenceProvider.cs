using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Inference.Abstractions;
using VPet.Cognition.Inference.Models;

namespace VPet.Cognition.Inference.Online;

/// <summary>
/// Online inference provider — connects to any OpenAI-compatible or Ollama endpoint.
/// Can list available models, categorize them (text, vision, embedding), and select active model.
/// </summary>
public sealed class OnlineInferenceProvider : IInferenceProvider
{
    public string ProviderId { get; }

    public InferenceCapabilities Capabilities { get; private set; } =
        InferenceCapabilities.Text | InferenceCapabilities.Streaming | InferenceCapabilities.ToolCalling | InferenceCapabilities.Json;

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly TimeSpan _timeout;
    private readonly ILogger<OnlineInferenceProvider> _logger;
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = null };
    private string _activeModel = string.Empty;

    public IReadOnlyList<ModelInfo> AvailableModels { get; private set; } = Array.Empty<ModelInfo>();

    /// <summary>
    /// Create a new online provider.
    /// </summary>
    /// <param name="providerId">"ollama" or "openai-compatible".</param>
    /// <param name="endpoint">Base URL (e.g. "http://localhost:11434" or "https://api.openai.com/v1").</param>
    /// <param name="apiKey">API key (empty for Ollama).</param>
    /// <param name="timeout">Request timeout.</param>
    public OnlineInferenceProvider(
        string providerId,
        string endpoint,
        string apiKey,
        TimeSpan timeout,
        HttpClient httpClient,
        ILogger<OnlineInferenceProvider> logger)
    {
        ProviderId = providerId;
        _apiKey = apiKey;
        _timeout = timeout;
        _httpClient = httpClient;
        _logger = logger;

        if (!endpoint.EndsWith('/'))
            endpoint += '/';
        _httpClient.BaseAddress = new Uri(endpoint);
        _httpClient.Timeout = timeout;

        if (!string.IsNullOrEmpty(_apiKey))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    /// <summary>
    /// List available models from the server. Works for both Ollama (/api/tags) and
    /// OpenAI-compatible (/models). Categorizes models by their capabilities.
    /// </summary>
    public async Task<Result<IReadOnlyList<ModelInfo>>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            // Try Ollama format first
            if (ProviderId == "ollama")
            {
                return await ListOllamaModelsAsync(ct);
            }

            // OpenAI-compatible format
            return await ListOpenAiModelsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list models from {Provider}", ProviderId);
            return Result<IReadOnlyList<ModelInfo>>.Fail(ex.Message);
        }
    }

    private async Task<Result<IReadOnlyList<ModelInfo>>> ListOllamaModelsAsync(CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync("api/tags", ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<ModelInfo>>.Fail($"HTTP {(int)response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync(ct);
        var dto = JsonSerializer.Deserialize<OllamaTagsResponse>(body, _json);
        if (dto?.Models is null)
            return Result<IReadOnlyList<ModelInfo>>.Ok(Array.Empty<ModelInfo>());

        var models = dto.Models.Select(m => new ModelInfo
        {
            Id = m.Name ?? "unknown",
            Name = m.Name ?? "unknown",
            Description = m.Details?.Family ?? "Ollama model",
            Category = CategorizeModel(m.Name ?? "", m.Details?.Family),
            FileSizeBytes = m.Size,
            ContextWindow = 8192,
            SupportsTools = true,
            SupportsStreaming = true,
            SupportsJson = true,
            SupportsVision = IsVisionModel(m.Name ?? ""),
            SupportsEmbedding = IsEmbeddingModel(m.Name ?? ""),
            Quantization = m.Details?.QuantizationLevel,
            IsInstalled = false,
        }).ToList();

        AvailableModels = models;
        return Result<IReadOnlyList<ModelInfo>>.Ok(models);
    }

    private async Task<Result<IReadOnlyList<ModelInfo>>> ListOpenAiModelsAsync(CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync("models", ct);
        if (!response.IsSuccessStatusCode)
            return Result<IReadOnlyList<ModelInfo>>.Fail($"HTTP {(int)response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync(ct);
        var dto = JsonSerializer.Deserialize<OpenAiModelsResponse>(body, _json);
        if (dto?.Data is null)
            return Result<IReadOnlyList<ModelInfo>>.Ok(Array.Empty<ModelInfo>());

        var models = dto.Data.Select(m => new ModelInfo
        {
            Id = m.Id ?? "unknown",
            Name = m.Id ?? "unknown",
            Description = $"OpenAI-compatible model",
            Category = CategorizeModel(m.Id ?? "", null),
            ContextWindow = 8192,
            SupportsTools = true,
            SupportsStreaming = true,
            SupportsJson = true,
            SupportsVision = IsVisionModel(m.Id ?? ""),
            SupportsEmbedding = IsEmbeddingModel(m.Id ?? ""),
            IsInstalled = false,
        }).ToList();

        AvailableModels = models;
        return Result<IReadOnlyList<ModelInfo>>.Ok(models);
    }

    /// <summary>
    /// Set the active model for inference.
    /// </summary>
    public void SetActiveModel(string modelId)
    {
        _activeModel = modelId;
        _logger.LogInformation("Active model set to {Id}", modelId);
    }

    public async Task<Result> LoadModelAsync(string modelId, CancellationToken ct = default)
    {
        _activeModel = modelId;

        // For online providers, "loading" means verifying the model exists
        var modelsResult = await ListModelsAsync(ct);
        if (!modelsResult.IsSuccess)
            return Result.Fail(modelsResult.Error!);

        if (!modelsResult.Value!.Any(m => m.Id == modelId))
            return Result.Fail($"Model '{modelId}' not found on server.");

        // Update capabilities based on model
        var model = modelsResult.Value!.First(m => m.Id == modelId);
        Capabilities = InferenceCapabilities.Text | InferenceCapabilities.Streaming;
        if (model.SupportsTools) Capabilities |= InferenceCapabilities.ToolCalling;
        if (model.SupportsJson) Capabilities |= InferenceCapabilities.Json;
        if (model.SupportsVision) Capabilities |= InferenceCapabilities.Vision;
        if (model.SupportsEmbedding) Capabilities |= InferenceCapabilities.Embedding;

        _logger.LogInformation("Online model {Id} loaded. Capabilities: {Caps}", modelId, Capabilities);
        return Result.Ok();
    }

    public Task UnloadModelAsync(CancellationToken ct = default)
    {
        _activeModel = string.Empty;
        return Task.CompletedTask;
    }

    public async Task<Result<InferenceResponse>> CompleteAsync(InferenceRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_activeModel))
            return Result<InferenceResponse>.Fail("No model selected. Call LoadModelAsync first.");

        var body = BuildRequestBody(request, stream: false);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);

        try
        {
            var endpoint = ProviderId == "ollama" ? "api/chat" : "chat/completions";
            using var response = await _httpClient.PostAsJsonAsync(endpoint, body, _json, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cts.Token);
                return Result<InferenceResponse>.Fail($"HTTP {(int)response.StatusCode}: {err}");
            }

            if (ProviderId == "ollama")
            {
                var dto = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(_json, cts.Token);
                return Result<InferenceResponse>.Ok(new InferenceResponse(
                    dto?.Message?.Content ?? "",
                    Array.Empty<ToolCallRequest>(),
                    dto?.PromptEvalCount ?? 0,
                    dto?.EvalCount ?? 0,
                    "stop", _activeModel));
            }
            else
            {
                var dto = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(_json, cts.Token);
                var choice = dto?.Choices?.FirstOrDefault();
                return Result<InferenceResponse>.Ok(new InferenceResponse(
                    choice?.Message?.Content ?? "",
                    Array.Empty<ToolCallRequest>(),
                    dto?.Usage?.PromptTokens ?? 0,
                    dto?.Usage?.CompletionTokens ?? 0,
                    choice?.FinishReason ?? "stop", _activeModel));
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Result<InferenceResponse>.Fail("Request timed out.");
        }
        catch (Exception ex)
        {
            return Result<InferenceResponse>.Fail(ex.Message);
        }
    }

    public async IAsyncEnumerable<InferenceChunk> StreamAsync(InferenceRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_activeModel))
        {
            _logger.LogWarning("StreamAsync called with no active model");
            yield break;
        }

        var body = BuildRequestBody(request, stream: true);
        var jsonStr = JsonSerializer.Serialize(body, _json);
        using var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);

        var endpoint = ProviderId == "ollama" ? "api/chat" : "chat/completions";
        using var response = await _httpClient.PostAsync(endpoint, content, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Stream returned {Status}", (int)response.StatusCode);
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        if (ProviderId == "ollama")
        {
            // Ollama: newline-delimited JSON
            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cts.Token);
                if (line is null || string.IsNullOrWhiteSpace(line)) continue;

                string? delta = null;
                try
                {
                    var dto = JsonSerializer.Deserialize<OllamaChatResponse>(line, _json);
                    delta = dto?.Message?.Content;
                }
                catch { /* skip malformed */ }

                if (!string.IsNullOrEmpty(delta))
                    yield return new InferenceChunk(delta, null, null, null);
            }
        }
        else
        {
            // OpenAI: SSE format
            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cts.Token);
                if (line is null || !line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                string? delta = null;
                try
                {
                    var dto = JsonSerializer.Deserialize<OpenAiStreamChunk>(data, _json);
                    delta = dto?.Choices?.FirstOrDefault()?.Delta?.Content;
                }
                catch { /* skip malformed */ }

                if (!string.IsNullOrEmpty(delta))
                    yield return new InferenceChunk(delta, null, null, null);
            }
        }
    }

    public async Task<Result<float[]>> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_activeModel))
            return Result<float[]>.Fail("No model selected.");

        try
        {
            var endpoint = ProviderId == "ollama" ? "api/embeddings" : "embeddings";
            var body = ProviderId == "ollama"
                ? (object)new { model = _activeModel, prompt = text }
                : new { model = _activeModel, input = text };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeout);
            using var response = await _httpClient.PostAsJsonAsync(endpoint, body, _json, cts.Token);
            if (!response.IsSuccessStatusCode)
                return Result<float[]>.Fail($"HTTP {(int)response.StatusCode}");

            var bodyStr = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(bodyStr);

            // Ollama: { "embedding": [...] }
            // OpenAI: { "data": [{ "embedding": [...] }] }
            float[]? embed = null;
            if (doc.RootElement.TryGetProperty("embedding", out var embEl))
                embed = embEl.Deserialize<float[]>();
            else if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.GetArrayLength() > 0)
                embed = dataEl[0].GetProperty("embedding").Deserialize<float[]>();

            if (embed is null)
                return Result<float[]>.Fail("No embedding in response.");

            return Result<float[]>.Ok(embed);
        }
        catch (Exception ex)
        {
            return Result<float[]>.Fail(ex.Message);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            var endpoint = ProviderId == "ollama" ? "api/tags" : "models";
            using var response = await _httpClient.GetAsync(endpoint, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private object BuildRequestBody(InferenceRequest request, bool stream)
    {
        var model = string.IsNullOrEmpty(request.Model) ? _activeModel : request.Model;

        if (ProviderId == "ollama")
        {
            return new
            {
                model,
                messages = BuildMessages(request),
                stream,
                options = new
                {
                    num_predict = request.MaxTokens,
                    temperature = request.Temperature,
                    top_p = request.TopP,
                    seed = request.Seed,
                    stop = request.Stop,
                },
            };
        }

        return new
        {
            model,
            messages = BuildMessages(request),
            stream,
            max_tokens = request.MaxTokens,
            temperature = request.Temperature,
            top_p = request.TopP,
            seed = request.Seed,
            stop = request.Stop,
        };
    }

    private static List<object> BuildMessages(InferenceRequest request)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });

        foreach (var msg in request.Messages)
        {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        return messages;
    }

    /// <summary>
    /// Categorize a model by its name/family.
    /// </summary>
    private static ModelCategory CategorizeModel(string modelName, string? family)
    {
        var lower = modelName.ToLowerInvariant();
        if (IsEmbeddingModel(lower)) return ModelCategory.Embedding;
        if (IsVisionModel(lower)) return ModelCategory.Vision;
        return ModelCategory.Text;
    }

    private static bool IsVisionModel(string name) =>
        name.Contains("vlm", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("vision", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("llava", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("smolvlm", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("vl-", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmbeddingModel(string name) =>
        name.Contains("embed", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("bge", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("e5", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("nomic", StringComparison.OrdinalIgnoreCase);

    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }

    // === DTOs ===
    private sealed class OllamaTagsResponse
    {
        [JsonPropertyName("models")] public List<OllamaModel>? Models { get; set; }
    }
    private sealed class OllamaModel
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
        [JsonPropertyName("details")] public OllamaModelDetails? Details { get; set; }
    }
    private sealed class OllamaModelDetails
    {
        [JsonPropertyName("family")] public string? Family { get; set; }
        [JsonPropertyName("quantization_level")] public string? QuantizationLevel { get; set; }
    }
    private sealed class OpenAiModelsResponse
    {
        [JsonPropertyName("data")] public List<OpenAiModel>? Data { get; set; }
    }
    private sealed class OpenAiModel
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }
    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")] public OllamaChatMessage? Message { get; set; }
        [JsonPropertyName("prompt_eval_count")] public int? PromptEvalCount { get; set; }
        [JsonPropertyName("eval_count")] public int? EvalCount { get; set; }
    }
    private sealed class OllamaChatMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")] public List<OpenAiChoice>? Choices { get; set; }
        [JsonPropertyName("usage")] public OpenAiUsage? Usage { get; set; }
    }
    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }
        [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
    }
    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
    private sealed class OpenAiUsage
    {
        [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    }
    private sealed class OpenAiStreamChunk
    {
        [JsonPropertyName("choices")] public List<OpenAiStreamChoice>? Choices { get; set; }
    }
    private sealed class OpenAiStreamChoice
    {
        [JsonPropertyName("delta")] public OpenAiDelta? Delta { get; set; }
    }
    private sealed class OpenAiDelta
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}