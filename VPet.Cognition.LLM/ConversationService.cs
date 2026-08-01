using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Errors;

namespace VPet.Cognition.LLM;

/// <summary>
/// Orchestrates a conversation turn: assemble prompt → stream LLM → collect tool calls → return.
/// This is the core service called by the conversation UI / voice FSM.
/// </summary>
public sealed class ConversationService
{
    private readonly ILLMProvider _llm;
    private readonly PromptAssembler _promptAssembler;
    private readonly StreamAdapter _streamAdapter;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        ILLMProvider llm,
        PromptAssembler promptAssembler,
        StreamAdapter streamAdapter,
        ILogger<ConversationService> logger)
    {
        _llm = llm;
        _promptAssembler = promptAssembler;
        _streamAdapter = streamAdapter;
        _logger = logger;
    }

    /// <summary>
    /// Execute one conversation turn with streaming output.
    /// </summary>
    /// <param name="systemPrompt">Personality + emotion + identity system prompt.</param>
    /// <param name="memoryContext">Retrieved memory context block.</param>
    /// <param name="history">Conversation history (oldest first).</param>
    /// <param name="userInput">Current user message.</param>
    /// <param name="tools">Available tools.</param>
    /// <param name="onToken">Callback for each streamed text token.</param>
    /// <param name="contextWindow">LLM context window size.</param>
    /// <param name="maxResponseTokens">Max tokens for the response.</param>
    /// <param name="cancellationToken">Cancellation (user cancel).</param>
    /// <returns>The full response text and any tool calls.</returns>
    public async Task<Result<ConversationTurn>> StreamConversationAsync(
        string systemPrompt,
        string memoryContext,
        IReadOnlyList<ChatMessage> history,
        string userInput,
        IReadOnlyList<ToolSchema> tools,
        Action<string> onToken,
        int contextWindow = 8192,
        int maxResponseTokens = 1024,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _promptAssembler.AssembleAsync(
                systemPrompt, memoryContext, history, userInput, tools,
                contextWindow, maxResponseTokens, cancellationToken);

            var (fullText, toolCalls) = await _streamAdapter.StreamAsync(
                _llm.StreamChatAsync(request, cancellationToken), onToken, cancellationToken);

            // Check for XML tool tags (fallback for non-native tool models)
            var xmlToolCalls = _streamAdapter.ExtractXmlToolCalls(fullText);
            var allToolCalls = toolCalls.Count > 0 ? toolCalls : xmlToolCalls;

            _logger.LogInformation("Conversation turn complete: {TextLen} chars, {ToolCallCount} tool calls",
                fullText.Length, allToolCalls.Count);

            return Result<ConversationTurn>.Success(new ConversationTurn(fullText, allToolCalls));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Conversation turn cancelled by user");
            return Result<ConversationTurn>.Failure(ProviderError.Unavailable(_llm.ProviderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversation turn failed");
            return Result<ConversationTurn>.Failure(ProviderError.Network(_llm.ProviderId, ex.Message));
        }
    }

    /// <summary>
    /// Feed tool results back to the LLM for a follow-up response.
    /// </summary>
    public async Task<Result<ConversationTurn>> StreamToolFollowUpAsync(
        string systemPrompt,
        string memoryContext,
        IReadOnlyList<ChatMessage> history,
        string userInput,
        IReadOnlyList<ToolCallResult> toolResults,
        IReadOnlyList<ToolSchema> tools,
        Action<string> onToken,
        int contextWindow = 8192,
        int maxResponseTokens = 1024,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await _promptAssembler.AssembleAsync(
                systemPrompt, memoryContext, history, userInput, tools,
                contextWindow, maxResponseTokens, cancellationToken);

            // Append tool results as tool-role messages
            var toolMessages = _streamAdapter.BuildToolResultMessages(toolResults);
            var allMessages = request.Messages.Concat(toolMessages).ToList();
            request = request with { Messages = allMessages };

            var (fullText, toolCalls) = await _streamAdapter.StreamAsync(
                _llm.StreamChatAsync(request, cancellationToken), onToken, cancellationToken);

            return Result<ConversationTurn>.Success(new ConversationTurn(fullText, toolCalls));
        }
        catch (OperationCanceledException)
        {
            return Result<ConversationTurn>.Failure(ProviderError.Unavailable(_llm.ProviderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool follow-up failed");
            return Result<ConversationTurn>.Failure(ProviderError.Network(_llm.ProviderId, ex.Message));
        }
    }
}

/// <summary>
/// The result of one conversation turn.
/// </summary>
public sealed record ConversationTurn(string Text, IReadOnlyList<ToolCallRequest> ToolCalls);