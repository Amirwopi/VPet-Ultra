using Microsoft.Extensions.Logging;

namespace VPet.Cognition.Voice;

/// <summary>
/// Conversation state machine for voice interaction.
/// States: Idle → Listening → Thinking → Speaking → Interrupted → Idle
/// </summary>
public sealed class ConversationFsm
{
    private readonly ILogger<ConversationFsm> _logger;
    private readonly object _lock = new();
    private ConversationState _state = ConversationState.Idle;

    public event Action<ConversationState, ConversationState>? StateChanged;

    public ConversationFsm(ILogger<ConversationFsm> logger) => _logger = logger;

    public ConversationState State
    {
        get { lock (_lock) return _state; }
    }

    public bool TransitionTo(ConversationState newState)
    {
        lock (_lock)
        {
            if (!IsValidTransition(_state, newState))
            {
                _logger.LogWarning("Invalid FSM transition: {From} → {To}", _state, newState);
                return false;
            }

            var old = _state;
            _state = newState;
            _logger.LogDebug("FSM: {Old} → {New}", old, newState);
            StateChanged?.Invoke(old, newState);
            return true;
        }
    }

    private static bool IsValidTransition(ConversationState from, ConversationState to)
    {
        return (from, to) switch
        {
            (ConversationState.Idle, ConversationState.Listening) => true,
            (ConversationState.Listening, ConversationState.Thinking) => true,
            (ConversationState.Listening, ConversationState.Idle) => true, // silence timeout
            (ConversationState.Thinking, ConversationState.Speaking) => true,
            (ConversationState.Thinking, ConversationState.Idle) => true, // error
            (ConversationState.Speaking, ConversationState.Idle) => true, // finished
            (ConversationState.Speaking, ConversationState.Interrupted) => true, // barge-in
            (ConversationState.Interrupted, ConversationState.Listening) => true, // restart
            (ConversationState.Interrupted, ConversationState.Idle) => true,
            _ => false,
        };
    }
}

/// <summary>
/// Conversation states for voice interaction.
/// </summary>
public enum ConversationState
{
    /// <summary>Waiting for user input (wake word or manual trigger).</summary>
    Idle,

    /// <summary>Listening to user speech (STT active).</summary>
    Listening,

    /// <summary>Processing with LLM (waiting for response).</summary>
    Thinking,

    /// <summary>Speaking the response (TTS active).</summary>
    Speaking,

    /// <summary>User interrupted (barge-in during speaking).</summary>
    Interrupted,
}