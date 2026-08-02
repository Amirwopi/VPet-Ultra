namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// Abstraction over the cognition host's conversation processing.
/// Decouples the WPF layer from the Host project (avoids circular dependency).
/// </summary>
public interface ICognitionFacade
{
    /// <summary>
    /// Process a conversation turn: retrieve memory, apply personality/emotion, return context.
    /// </summary>
    Task<string> ProcessConversationTurnAsync(string userInput, CancellationToken ct = default);

    /// <summary>
    /// Store a completed conversation turn to memory.
    /// </summary>
    Task StoreConversationTurnAsync(string userInput, string assistantResponse, CancellationToken ct = default);
}