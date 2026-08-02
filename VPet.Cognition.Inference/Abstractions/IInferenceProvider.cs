using System.Text.Json;
using VPet.Cognition.Inference.Models;

namespace VPet.Cognition.Inference.Abstractions;

/// <summary>
/// Capability flags that a provider may support.
/// </summary>
[Flags]
public enum InferenceCapabilities
{
    None = 0,
    Text = 1,
    Streaming = 2,
    Vision = 4,
    Embedding = 8,
    ToolCalling = 16,
    Json = 32,
    All = Text | Streaming | Vision | Embedding | ToolCalling | Json,
}

/// <summary>
/// A request for text completion / chat.
/// </summary>
public sealed class InferenceRequest
{
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public List<InferenceMessage> Messages { get; set; } = new();
    public int MaxTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.7;
    public double? TopP { get; set; }
    public int? Seed { get; set; }
    public string? Stop { get; set; }
    public List<ToolSchema> Tools { get; set; } = new();
    public string? ResponseFormat { get; set; }
    /// <summary>Optional image bytes for vision-capable models.</summary>
    public byte[]? ImageBytes { get; set; }
}

/// <summary>
/// A single message in an inference conversation.
/// </summary>
public sealed class InferenceMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public byte[]? ImageBytes { get; set; }
    public List<ToolCallRequest>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
}

/// <summary>
/// A non-streaming inference response.
/// </summary>
public sealed record InferenceResponse(
    string Text,
    IReadOnlyList<ToolCallRequest> ToolCalls,
    int PromptTokens,
    int CompletionTokens,
    string FinishReason,
    string ModelUsed);

/// <summary>
/// A single streaming chunk.
/// </summary>
public sealed record InferenceChunk(string? TextDelta, ToolCallRequest? ToolCallDelta, int? PromptTokens, int? CompletionTokens);

/// <summary>
/// The core abstraction over any inference engine — local (LLamaSharp) or online (OpenAI/Ollama).
/// Each provider declares its capabilities; the caller checks before using vision/embedding/etc.
/// </summary>
public interface IInferenceProvider : IAsyncDisposable
{
    /// <summary>Stable provider id (e.g. "local-llama", "ollama", "openai-compatible").</summary>
    string ProviderId { get; }

    /// <summary>What this provider can do.</summary>
    InferenceCapabilities Capabilities { get; }

    /// <summary>Models currently available (loaded locally or listed from server).</summary>
    IReadOnlyList<ModelInfo> AvailableModels { get; }

    /// <summary>Non-streaming chat completion.</summary>
    Task<Result<InferenceResponse>> CompleteAsync(InferenceRequest request, CancellationToken ct = default);

    /// <summary>Streaming chat completion — yields text deltas as they arrive.</summary>
    IAsyncEnumerable<InferenceChunk> StreamAsync(InferenceRequest request, CancellationToken ct = default);

    /// <summary>Generate an embedding vector for the given text.</summary>
    Task<Result<float[]>> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Check if the provider is ready (model loaded / server reachable).</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Load a model (local providers load GGUF; online providers verify endpoint).</summary>
    Task<Result> LoadModelAsync(string modelId, CancellationToken ct = default);

    /// <summary>Unload the current model (free RAM).</summary>
    Task UnloadModelAsync(CancellationToken ct = default);
}

/// <summary>
/// A tool-call request emitted by the model.
/// </summary>
public sealed record ToolCallRequest(string Id, string ToolName, JsonElement Arguments);

/// <summary>
/// Schema describing a tool to the model.
/// </summary>
public sealed record ToolSchema
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public JsonElement? ParametersJsonSchema { get; init; }
    public bool RequiresConfirmation { get; init; }
}

/// <summary>
/// Generic result type.
/// </summary>
public sealed record Result(bool IsSuccess, string? Error, object? Value)
{
    public static Result Ok() => new(true, null, null);
    public static Result Fail(string error) => new(false, error, null);
    public static Result<T> Ok<T>(T value) => new(true, null, value);
    public static Result<T> Fail<T>(string error) => new(false, error, default);
}

public sealed record Result<T>(bool IsSuccess, string? Error, T? Value)
{
    public static Result<T> Ok(T value) => new(true, null, value);
    public static Result<T> Fail(string error) => new(false, error, default);
}