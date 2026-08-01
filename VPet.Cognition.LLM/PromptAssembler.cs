using System.Text;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;

namespace VPet.Cognition.LLM;

/// <summary>
/// Assembles a <see cref="ChatRequest"/> from system prompt (personality + emotion + identity),
/// retrieved memories, conversation history, and user input — within a token budget.
/// </summary>
public sealed class PromptAssembler
{
    private readonly ILLMProvider _llm;
    private readonly ILogger<PromptAssembler> _logger;

    public PromptAssembler(ILLMProvider llm, ILogger<PromptAssembler> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    /// <summary>
    /// Build a chat request from the given components, truncating within the token budget.
    /// </summary>
    /// <param name="systemPrompt">Base system prompt (personality + emotion + identity).</param>
    /// <param name="memoryContext">Formatted memory context block (may be empty).</param>
    /// <param name="history">Conversation history (oldest first).</param>
    /// <param name="userInput">Current user message.</param>
    /// <param name="tools">Available tools (optional).</param>
    /// <param name="contextWindow">Max tokens for the context (system + memory + history).</param>
    /// <param name="maxResponseTokens">Tokens reserved for the response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ChatRequest> AssembleAsync(
        string systemPrompt,
        string memoryContext,
        IReadOnlyList<ChatMessage> history,
        string userInput,
        IReadOnlyList<ToolSchema> tools,
        int contextWindow,
        int maxResponseTokens,
        CancellationToken cancellationToken = default)
    {
        var budget = contextWindow - maxResponseTokens;
        if (budget < 256)
            budget = 256;

        // Build the full system prompt
        var fullSystem = BuildSystemPrompt(systemPrompt, memoryContext);
        var systemTokens = await _llm.CountTokensAsync(fullSystem, cancellationToken);

        // User input tokens
        var userTokens = await _llm.CountTokensAsync(userInput, cancellationToken);

        var remaining = budget - systemTokens - userTokens;
        if (remaining < 0)
            remaining = 0;

        // Truncate history oldest-first to fit
        var truncatedHistory = await TruncateHistoryAsync(history, remaining, cancellationToken);

        var messages = new List<ChatMessageDto>();
        foreach (var msg in truncatedHistory)
            messages.Add(new ChatMessageDto(msg.Role, msg.Content));

        // Add current user input
        messages.Add(new ChatMessageDto("user", userInput));

        _logger.LogDebug("Assembled prompt: system={SystemTokens}t, history={HistoryCount} msgs, user={UserTokens}t, budget={Budget}t",
            systemTokens, truncatedHistory.Count, userTokens, budget);

        return new ChatRequest
        {
            SystemPrompt = fullSystem,
            Messages = messages,
            Tools = tools,
            MaxTokens = maxResponseTokens,
        };
    }

    private static string BuildSystemPrompt(string basePrompt, string memoryContext)
    {
        var sb = new StringBuilder(basePrompt);
        if (!string.IsNullOrEmpty(memoryContext))
        {
            sb.AppendLine();
            sb.AppendLine("# Context (memories)");
            sb.Append(memoryContext);
        }
        return sb.ToString();
    }

    private async Task<IReadOnlyList<ChatMessage>> TruncateHistoryAsync(
        IReadOnlyList<ChatMessage> history, int budgetTokens, CancellationToken ct)
    {
        if (history.Count == 0 || budgetTokens <= 0)
            return Array.Empty<ChatMessage>();

        // Iterate newest-first, accumulate until budget
        var result = new List<ChatMessage>();
        var tokens = 0;
        for (int i = history.Count - 1; i >= 0; i--)
        {
            var msg = history[i];
            var msgTokens = await _llm.CountTokensAsync(msg.Content, ct);
            if (tokens + msgTokens > budgetTokens)
                break;
            result.Insert(0, msg);
            tokens += msgTokens;
        }

        return result;
    }
}