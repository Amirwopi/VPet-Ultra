using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Errors;

namespace VPet.Cognition.LLM.OpenAICompatible;

/// <summary>
/// An <see cref="ILLMProvider"/> that speaks the OpenAI-compatible /v1/chat/completions endpoint.
/// Covers Qwen (DashScope), GLM (Zhipu), DeepSeek, Llama (via Together/Groq/localhost), OpenAI,
/// and any Ollama instance via its /v1/ compatibility layer.
/// </summary>
public sealed class OpenAICompatibleLlmProvider : ILLMProvider
{
    public string ProviderId { get; }

    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _apiKey;
    private readonly TimeSpan _timeout;
    private readonly ILogger<OpenAICompatibleLlmProvider> _logger;
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = null };

    public IReadOnlyList<ModelInfo> AvailableModels { get; }

    /// <summary>
    /// Create a new OpenAI-compatible provider.
    /// </summary>
    /// <param name="providerId">Stable id (e.g. "openai-compatible", "deepseek").</param>
    /// <param name="endpoint">Base URL ending with /v1/ (e.g. "http://localhost:11434/v1/").</param>
    /// <param name="model">Default model name.</param>
    /// <param name="apiKey">API key (empty for local; resolved by caller from env/DPAPI).</param>
    /// <param name="timeout">Request timeout.</param>
    /// <param name="httpClient">Injected HttpClient (from IHttpClientFactory).</param>
    /// <param name="logger">Logger.</param>
    public OpenAICompatibleLlmProvider(
        string providerId,
        string endpoint,
        string model,
        string apiKey,
        TimeSpan timeout,
        HttpClient httpClient,
        ILogger<OpenAICompatibleLlmProvider> logger)
    {
        ProviderId = providerId;
        _model = model;
        _apiKey = apiKey;
        _timeout = timeout;
        _httpClient = httpClient;
        _logger = logger;

        // Ensure base address ends with /
        if (!endpoint.EndsWith('/'))
            endpoint += '/';
        _httpClient.BaseAddress = new Uri(endpoint);
        _httpClient.Timeout = timeout;

        AvailableModels = new[] { new ModelInfo(model, 8192, SupportsTools: true, SupportsStreaming: true, SupportsJson: true) };
    }

    public async Task<Result<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var body = BuildRequestBody(request, stream: false);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("chat/completions", body, _json, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("LLM {Provider} returned {Status}: {Body}", ProviderId, (int)response.StatusCode, errBody);
                return (int)response.StatusCode >= 500
                    ? ProviderError.ServerError(ProviderId, (int)response.StatusCode)
                    : ProviderError.Network(ProviderId, $"HTTP {(int)response.StatusCode}: {errBody}");
            }

            var dto = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(_json, cts.Token)
                ?? throw new JsonException("Null response body");
            var choice = dto.Choices.FirstOrDefault() ?? throw new JsonException("No choices in response");

            var toolCalls = ParseToolCalls(choice.Message.ToolCalls);
            var usage = new Usage(dto.Usage?.PromptTokens ?? 0, dto.Usage?.CompletionTokens ?? 0);

            return Result<ChatResponse>.Success(new ChatResponse(
                choice.Message.Content ?? string.Empty,
                toolCalls,
                usage,
                choice.FinishReason ?? "stop",
                dto.Model ?? _model));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProviderError.Timeout(ProviderId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error contacting {Provider}", ProviderId);
            return ProviderError.Network(ProviderId, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Deserialization error from {Provider}", ProviderId);
            return ProviderError.Deserialize(ProviderId, ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var body = BuildRequestBody(request, stream: true);
        using var content = new StringContent(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        using var response = await _httpClient.PostAsync("chat/completions", content, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cts.Token);
            _logger.LogWarning("LLM {Provider} stream returned {Status}: {Body}", ProviderId, (int)response.StatusCode, errBody);
            yield return new ChatChunk(new ChatDelta(Text: null, ToolCall: null));
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cts.Token);
            if (line is null)
                break;

            // SSE format: lines start with "data: "
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]")
                break;

            ChatChunk? chunk = null;
            try
            {
                var dto = JsonSerializer.Deserialize<OpenAiStreamChunk>(data, _json);
                chunk = MapStreamChunk(dto);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug("Skipping malformed stream chunk: {Error}", ex.Message);
            }

            if (chunk is { } c)
                yield return c;
        }
    }

    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        // Approximate: ~4 chars per token for English/multilingual
        return Task.FromResult(Math.Max(1, text.Length / 4));
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await _httpClient.GetAsync("models", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }

    private OpenAiChatRequest BuildRequestBody(ChatRequest request, bool stream)
    {
        var body = new OpenAiChatRequest
        {
            Model = string.IsNullOrEmpty(request.Model) ? _model : request.Model,
            Stream = stream,
            MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : null,
            Temperature = request.Temperature,
            TopP = request.TopP,
            Seed = request.Seed,
            Stop = request.Stop,
        };

        // System prompt as first message
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            body.Messages.Add(new OpenAiMessage { Role = "system", Content = request.SystemPrompt });

        // Conversation messages
        foreach (var msg in request.Messages)
        {
            var openMsg = new OpenAiMessage { Role = msg.Role, Content = msg.Content };
            if (msg.ToolCalls is { Count: > 0 })
            {
                openMsg.ToolCalls = msg.ToolCalls.Select(tc => new OpenAiToolCall
                {
                    Id = tc.Id,
                    Function = new OpenAiToolCallFunction { Name = tc.ToolName, Arguments = tc.Arguments.GetRawText() }
                }).ToList();
            }
            body.Messages.Add(openMsg);
        }

        // Tools
        if (request.Tools.Count > 0)
        {
            body.Tools = request.Tools.Select(t => new OpenAiTool
            {
                Function = new OpenAiFunction
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = t.ParametersJsonSchema,
                }
            }).ToList();
        }

        if (!string.IsNullOrEmpty(request.ResponseFormat))
            body.ResponseFormat = new OpenAiResponseFormat { Type = request.ResponseFormat };

        return body;
    }

    private ChatChunk? MapStreamChunk(OpenAiStreamChunk? dto)
    {
        if (dto?.Choices is null || dto.Choices.Count == 0)
            return null;

        var choice = dto.Choices[0];
        var delta = choice.Delta;

        // Text content
        if (!string.IsNullOrEmpty(delta.Content))
            return new ChatChunk(new ChatDelta(Text: delta.Content));

        // Tool call (streamed fragments)
        if (delta.ToolCalls is { Count: > 0 })
        {
            var tc = delta.ToolCalls[0];
            if (tc.Function is { } fn && (!string.IsNullOrEmpty(fn.Name) || !string.IsNullOrEmpty(fn.Arguments)))
            {
                var args = JsonDocument.Parse(string.IsNullOrEmpty(fn.Arguments) ? "{}" : fn.Arguments).RootElement;
                return new ChatChunk(new ChatDelta(ToolCall: new ToolCallRequest(
                    tc.Id ?? Guid.NewGuid().ToString("N"),
                    fn.Name ?? string.Empty,
                    args)));
            }
        }

        // Usage on final chunk
        if (dto.Usage is { } u)
            return new ChatChunk(new ChatDelta(), new Usage(u.PromptTokens, u.CompletionTokens));

        return null;
    }

    private IReadOnlyList<ToolCallRequest> ParseToolCalls(List<OpenAiToolCall>? toolCalls)
    {
        if (toolCalls is null || toolCalls.Count == 0)
            return Array.Empty<ToolCallRequest>();

        var result = new List<ToolCallRequest>(toolCalls.Count);
        foreach (var tc in toolCalls)
        {
            var args = string.IsNullOrEmpty(tc.Function.Arguments)
                ? JsonDocument.Parse("{}").RootElement
                : JsonDocument.Parse(tc.Function.Arguments).RootElement;
            result.Add(new ToolCallRequest(tc.Id, tc.Function.Name, args));
        }
        return result;
    }
}