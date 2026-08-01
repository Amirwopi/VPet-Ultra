using System.Runtime.CompilerServices;
using System.Text.Json;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Errors;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="ILLMProvider"/> that returns canned responses and streams pre-set chunks.
/// </summary>
public sealed class FakeLlmProvider : ILLMProvider
{
    public string ProviderId { get; set; } = "fake";
    public IReadOnlyList<ModelInfo> AvailableModels { get; set; } = new[]
    {
        new ModelInfo("fake-model", 8192, SupportsTools: true, SupportsStreaming: true, SupportsJson: true),
    };

    /// <summary>Response returned by <see cref="ChatAsync"/>.</summary>
    public ChatResponse NextResponse { get; set; } = new(
        Content: "Hello from the fake LLM!",
        ToolCalls: Array.Empty<ToolCallRequest>(),
        Usage: new Usage(10, 5),
        FinishReason: "stop",
        Model: "fake-model");

    /// <summary>Chunks yielded by <see cref="StreamChatAsync"/>.</summary>
    public IReadOnlyList<ChatChunk> NextStream { get; set; } = Array.Empty<ChatChunk>();

    /// <summary>If set, <see cref="ChatAsync"/> returns this failure instead of <see cref="NextResponse"/>.</summary>
    public Result<ChatResponse>? NextFailure { get; set; }

    public int TokenCount { get; set; } = 0;
    public bool IsAvailable { get; set; } = true;

    public Task<Result<ChatResponse>> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        TokenCount += (request.SystemPrompt.Length + string.Join("", request.Messages.Select(m => m.Content))).Length / 4;
        return NextFailure is { } failure
            ? Task.FromResult(failure)
            : Task.FromResult(Result<ChatResponse>.Success(NextResponse));
    }

    public async IAsyncEnumerable<ChatChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var chunk in NextStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);
            yield return chunk;
        }
    }

    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult(text.Length / 4);

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(IsAvailable);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}