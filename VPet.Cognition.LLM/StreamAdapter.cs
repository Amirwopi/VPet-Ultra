using System.Text;
using System.Text.Json;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.LLM.ToolCall;

namespace VPet.Cognition.LLM;

/// <summary>
/// Adapts an <see cref="IAsyncEnumerable{ChatChunk}"/> stream to a <c>SayInfoWithStream</c>-like
/// callback interface. Decouples the LLM stream from the VPet Core MessageBar.
/// </summary>
public sealed class StreamAdapter
{
    private readonly List<ToolCallRequest> _pendingToolCalls = new();

    /// <summary>
    /// Stream tokens from the LLM, invoking <paramref name="onToken"/> for each text delta.
    /// Collects tool calls for later execution.
    /// </summary>
    /// <param name="stream">The LLM token stream.</param>
    /// <param name="onToken">Called for each text chunk (the typewriter effect).</param>
    /// <param name="cancellationToken">Cancellation token (user cancel).</param>
    /// <returns>The full accumulated text and any collected tool calls.</returns>
    public async Task<(string FullText, IReadOnlyList<ToolCallRequest> ToolCalls)> StreamAsync(
        IAsyncEnumerable<ChatChunk> stream,
        Action<string> onToken,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        _pendingToolCalls.Clear();

        await foreach (var chunk in stream.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (chunk.Delta.Text is { } text)
            {
                sb.Append(text);
                onToken(text);
            }

            if (chunk.Delta.ToolCall is { } tc)
            {
                _pendingToolCalls.Add(tc);
            }
        }

        return (sb.ToString(), _pendingToolCalls);
    }

    /// <summary>
    /// After streaming, check if the text contains XML tool tags (fallback for non-native tool models).
    /// </summary>
    public IReadOnlyList<ToolCallRequest> ExtractXmlToolCalls(string text)
    {
        var parsed = XmlToolCallParser.Parse(text);
        if (parsed.Count > 0)
            _pendingToolCalls.AddRange(parsed);
        return _pendingToolCalls;
    }

    /// <summary>
    /// Build a follow-up message list with tool results to feed back to the LLM.
    /// </summary>
    public IReadOnlyList<ChatMessageDto> BuildToolResultMessages(IReadOnlyList<ToolCallResult> results)
    {
        var messages = new List<ChatMessageDto>();
        foreach (var r in results)
        {
            var content = JsonSerializer.Serialize(new { success = r.Success, output = r.Output, error = r.Error });
            messages.Add(new ChatMessageDto("tool", content));
        }
        return messages;
    }
}