using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Errors;

namespace VPet.Cognition.LLM;

/// <summary>
/// LLM provider wrapper that implements a fallback chain and retry with exponential backoff.
/// If the primary provider fails, it retries up to maxRetries times with
/// exponential backoff (1s, 2s, 4s), then falls back to the secondary provider.
/// </summary>
public sealed class FallbackLlmProvider : ILLMProvider
{
    private readonly ILLMProvider _primary;
    private readonly ILLMProvider? _fallback;
    private readonly int _maxRetries;
    private readonly ILogger<FallbackLlmProvider> _logger;

    public FallbackLlmProvider(
        ILLMProvider primary,
        ILLMProvider? fallback,
        int maxRetries,
        ILogger<FallbackLlmProvider> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _maxRetries = maxRetries;
        _logger = logger;
    }

    public string ProviderId => _fallback is not null ? $"{_primary.ProviderId}+{_fallback.ProviderId}" : _primary.ProviderId;

    public IReadOnlyList<ModelInfo> AvailableModels => _primary.AvailableModels;

    public async Task<Result<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        // Try primary with retries
        var result = await TryWithRetries(() => _primary.ChatAsync(request, cancellationToken), cancellationToken);

        if (result.IsSuccess || _fallback is null)
            return result;

        // Primary failed after all retries — try fallback
        _logger.LogWarning("Primary provider {Primary} failed after {Retries} retries; falling back to {Fallback}",
            _primary.ProviderId, _maxRetries, _fallback.ProviderId);

        try
        {
            return await _fallback.ChatAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback provider {Fallback} also failed.", _fallback.ProviderId);
            return Result<ChatResponse>.Failure(ProviderError.Unavailable($"{_primary.ProviderId}+{_fallback.ProviderId}"));
        }
    }

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For streaming, try primary first; if it throws, switch to fallback
        bool primaryFailed = false;

        await using var primaryEnum = _primary.StreamChatAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await primaryEnum.MoveNextAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Primary streaming failed; trying fallback.");
                    primaryFailed = true;
                    break;
                }

                if (!hasNext) yield break;
                yield return primaryEnum.Current;
            }
        }
        finally
        {
            await primaryEnum.DisposeAsync();
        }

        if (primaryFailed && _fallback is not null)
        {
            await using var fallbackEnum = _fallback.StreamChatAsync(request, cancellationToken).GetAsyncEnumerator(cancellationToken);
            while (await fallbackEnum.MoveNextAsync())
            {
                yield return fallbackEnum.Current;
            }
        }
    }

    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        => _primary.CountTokensAsync(text, cancellationToken);

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => _primary.IsAvailableAsync(cancellationToken);

    public ValueTask DisposeAsync()
    {
        var primary = _primary.DisposeAsync();
        var fallback = _fallback?.DisposeAsync() ?? default;
        if (fallback.IsCompletedSuccessfully)
            return primary;
        return new ValueTask(Task.WhenAll(primary.AsTask(), fallback.AsTask()));
    }

    private async Task<Result<ChatResponse>> TryWithRetries(
        Func<Task<Result<ChatResponse>>> action,
        CancellationToken ct)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            if (ct.IsCancellationRequested)
                return Result<ChatResponse>.Failure(ProviderError.Unavailable("cancelled"));

            try
            {
                var result = await action();
                if (result.IsSuccess)
                    return result;

                // Non-exception failure — retry if attempts remain
                if (attempt < _maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 1s, 2s, 4s
                    _logger.LogWarning("LLM attempt {Attempt} failed: {Error}. Retrying in {Delay}...", attempt + 1, result.Error, delay);
                    await Task.Delay(delay, ct);
                }
                else
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < _maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning(ex, "LLM attempt {Attempt} threw. Retrying in {Delay}...", attempt + 1, delay);
                    await Task.Delay(delay, ct);
                }
            }
        }

        return Result<ChatResponse>.Failure(
            ProviderError.Unavailable($"All {_maxRetries + 1} attempts failed. Last: {lastException?.Message ?? "unknown"}"));
    }
}

/// <summary>
/// Simple token counter that approximates token count from text length.
/// Uses ~4 chars per token (standard heuristic for English text).
/// </summary>
public sealed class TokenCounter
{
    private const double CharsPerToken = 4.0;

    /// <summary>Approximate token count for a text.</summary>
    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    /// <summary>Approximate token count for a chat history.</summary>
    public int CountTokens(IEnumerable<ChatMessageDto> messages)
    {
        int total = 0;
        foreach (var msg in messages)
        {
            total += CountTokens(msg.Content);
            // overhead per message (role tags, separators)
            total += 4;
        }
        return total;
    }

    /// <summary>Truncate chat history to fit within a token budget (oldest first removed).</summary>
    public List<ChatMessageDto> TruncateToBudget(IEnumerable<ChatMessageDto> messages, int maxTokens)
    {
        var list = messages.ToList();
        while (CountTokens(list) > maxTokens && list.Count > 2)
        {
            list.RemoveAt(0);
        }
        return list;
    }
}