using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Errors;

namespace VPet.Cognition.LLM.Ollama;

/// <summary>
/// An <see cref="ILLMProvider"/> that speaks Ollama's native /api/chat endpoint.
/// This is the default offline-first provider. No API key required.
/// </summary>
public sealed class OllamaLlmProvider : ILLMProvider
{
    public string ProviderId => "ollama";

    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly TimeSpan _timeout;
    private readonly ILogger<OllamaLlmProvider> _logger;
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = null };

    public IReadOnlyList<ModelInfo> AvailableModels { get; }

    /// <summary>
    /// Create a new Ollama provider.
    /// </summary>
    /// <param name="endpoint">Base URL (default "http://localhost:11434").</param>
    /// <param name="model">Model name (e.g. "qwen2.5:3b").</param>
    /// <param name="timeout">Request timeout.</param>
    /// <param name="httpClient">Injected HttpClient (from IHttpClientFactory).</param>
    /// <param name="logger">Logger.</param>
    public OllamaLlmProvider(
        string endpoint,
        string model,
        TimeSpan timeout,
        HttpClient httpClient,
        ILogger<OllamaLlmProvider> logger)
    {
        _model = model;
        _timeout = timeout;
        _httpClient = httpClient;
        _logger = logger;

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
            using var response = await _httpClient.PostAsJsonAsync("api/chat", body, _json, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("Ollama returned {Status}: {Body}", (int)response.StatusCode, errBody);
                return (int)response.StatusCode >= 500
                    ? ProviderError.ServerError(ProviderId, (int)response.StatusCode)
                    : ProviderError.Network(ProviderId, $"HTTP {(int)response.StatusCode}: {errBody}");
            }

            var dto = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(_json, cts.Token)
                ?? throw new JsonException("Null response body");

            var msg = dto.Message ?? throw new JsonException("No message in response");
            var toolCalls = ParseToolCalls(msg.ToolCalls);
            var usage = new Usage(dto.PromptEvalCount ?? 0, dto.EvalCount ?? 0);

            return Result<ChatResponse>.Success(new ChatResponse(
                msg.Content, toolCalls, usage, dto.DoneReason ?? "stop", dto.Model ?? _model));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProviderError.Timeout(ProviderId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error contacting Ollama");
            return ProviderError.Network(ProviderId, ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Deserialization error from Ollama");
            return ProviderError.Deserialize(ProviderId, ex.Message);
        }
    }

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Use stream: false for reliability — Ollama streaming connections get dropped
        // by the remote server (SocketException 10054/995). Non-streaming works fine
        // (confirmed via curl: 3 consecutive requests all succeeded in ~2.7s each).
        var body = BuildRequestBody(request, stream: false);
        var jsonStr = JsonSerializer.Serialize(body, _json);

        // Retry up to 3 times — each attempt gets its own CTS.
        string? responseText = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);
                using var response = await _httpClient.PostAsync("api/chat",
                    new StringContent(jsonStr, Encoding.UTF8, "application/json"), cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Ollama returned {Status}: {Body}", (int)response.StatusCode, errBody);
                    yield break;
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson, _json);
                responseText = dto?.Message?.Content ?? "";
                break;
            }
            catch (Exception ex) when (attempt < 2 && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Ollama attempt {Attempt} failed: {Error}. Retrying in {Delay}ms...",
                    attempt + 1, ex.Message, 1000 * (attempt + 1));
                await Task.Delay(1000 * (attempt + 1), cancellationToken);
            }
        }

        if (responseText is null)
        {
            _logger.LogError("Ollama failed after 3 attempts.");
            yield break;
        }

        // Yield the complete text as a single chunk
        if (!string.IsNullOrEmpty(responseText))
            yield return new ChatChunk(new ChatDelta(responseText));
    }

    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(Math.Max(1, text.Length / 4));

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await _httpClient.GetAsync("api/tags", cts.Token);
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

    private OllamaChatRequest BuildRequestBody(ChatRequest request, bool stream)
    {
        var body = new OllamaChatRequest
        {
            Model = string.IsNullOrEmpty(request.Model) ? _model : request.Model,
            Stream = stream,
            Options = new OllamaOptions
            {
                NumPredict = request.MaxTokens > 0 ? request.MaxTokens : null,
                Temperature = request.Temperature,
                TopP = request.TopP,
                Seed = request.Seed,
                Stop = request.Stop,
            },
            Format = request.ResponseFormat,
        };

        if (!string.IsNullOrEmpty(request.SystemPrompt))
            body.Messages.Add(new OllamaMessage { Role = "system", Content = request.SystemPrompt });

        foreach (var msg in request.Messages)
        {
            var oMsg = new OllamaMessage { Role = msg.Role, Content = msg.Content };
            if (msg.ToolCalls is { Count: > 0 })
            {
                oMsg.ToolCalls = msg.ToolCalls.Select(tc => new OllamaToolCall
                {
                    Id = tc.Id,
                    Function = new OllamaToolCallFunction { Name = tc.ToolName, Arguments = tc.Arguments }
                }).ToList();
            }
            body.Messages.Add(oMsg);
        }

        if (request.Tools.Count > 0)
        {
            body.Tools = request.Tools.Select(t => new OllamaTool
            {
                Function = new OllamaFunction
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = t.ParametersJsonSchema,
                }
            }).ToList();
        }

        return body;
    }

    private ChatChunk? MapChunk(OllamaChatResponse? dto)
    {
        if (dto?.Message is null)
            return null;

        // Text content
        if (!string.IsNullOrEmpty(dto.Message.Content))
            return new ChatChunk(new ChatDelta(Text: dto.Message.Content));

        // Tool calls
        if (dto.Message.ToolCalls is { Count: > 0 })
        {
            var tc = dto.Message.ToolCalls[0];
            if (tc.Function is { } fn)
            {
                var args = fn.Arguments ?? JsonDocument.Parse("{}").RootElement;
                return new ChatChunk(new ChatDelta(ToolCall: new ToolCallRequest(tc.Id, fn.Name, args)));
            }
        }

        // Final chunk with usage
        if (dto.Done)
            return new ChatChunk(new ChatDelta(), new Usage(dto.PromptEvalCount ?? 0, dto.EvalCount ?? 0));

        return null;
    }

    private IReadOnlyList<ToolCallRequest> ParseToolCalls(List<OllamaToolCall>? toolCalls)
    {
        if (toolCalls is null || toolCalls.Count == 0)
            return Array.Empty<ToolCallRequest>();

        var result = new List<ToolCallRequest>(toolCalls.Count);
        foreach (var tc in toolCalls)
        {
            var args = tc.Function.Arguments ?? JsonDocument.Parse("{}").RootElement;
            result.Add(new ToolCallRequest(tc.Id, tc.Function.Name, args));
        }
        return result;
    }
}