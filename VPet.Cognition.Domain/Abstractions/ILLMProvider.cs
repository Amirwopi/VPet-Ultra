using System.Text.Json;
using VPet.Cognition.Domain.Entities;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// A chat message in a request to the LLM.
/// </summary>
public sealed record ChatMessageDto(string Role, string Content, IReadOnlyList<ToolCallRequest>? ToolCalls = null);

/// <summary>
/// A request to an <see cref="ILLMProvider"/>.
/// </summary>
public sealed record ChatRequest
{
    public string SystemPrompt { get; init; } = string.Empty;
    public IReadOnlyList<ChatMessageDto> Messages { get; init; } = Array.Empty<ChatMessageDto>();
    public IReadOnlyList<ToolSchema> Tools { get; init; } = Array.Empty<ToolSchema>();
    public int MaxTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 0.7;
    public double TopP { get; init; } = 1.0;
    public int? Seed { get; init; }
    public string? Stop { get; init; }
    public string Model { get; init; } = string.Empty;
    public string? ResponseFormat { get; init; }
}

/// <summary>
/// A non-streaming response from an <see cref="ILLMProvider"/>.
/// </summary>
public sealed record ChatResponse(string Content, IReadOnlyList<ToolCallRequest> ToolCalls, Usage Usage, string FinishReason, string Model);

/// <summary>
/// A single streamed chunk from an <see cref="ILLMProvider"/>.
/// </summary>
public sealed record ChatChunk(ChatDelta Delta, Usage? Usage = null);

/// <summary>
/// A delta within a streamed chunk: either text or a tool-call fragment.
/// </summary>
public sealed record ChatDelta(string? Text = null, ToolCallRequest? ToolCall = null);

/// <summary>
/// Token usage statistics.
/// </summary>
public sealed record Usage(int PromptTokens, int CompletionTokens)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Metadata about a model offered by a provider.
/// </summary>
public sealed record ModelInfo(string Id, int ContextWindow, bool SupportsTools, bool SupportsStreaming, bool SupportsJson);

/// <summary>
/// Abstraction over a Large Language Model provider. Implementations include
/// OpenAI-compatible (Qwen/GLM/DeepSeek/Llama/OpenAI), Ollama, and llama.cpp.
/// </summary>
public interface ILLMProvider : IAsyncDisposable
{
    /// <summary>Stable identifier of this provider (e.g. "ollama", "openai-compatible").</summary>
    string ProviderId { get; }

    /// <summary>Models offered by this provider.</summary>
    IReadOnlyList<ModelInfo> AvailableModels { get; }

    /// <summary>Perform a non-streaming chat completion.</summary>
    Task<Errors.Result<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>Perform a streaming chat completion, yielding chunks as they arrive.</summary>
    IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>Approximate token count for a text.</summary>
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Whether the provider endpoint is reachable and ready.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}