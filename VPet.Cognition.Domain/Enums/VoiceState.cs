namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// State of the voice conversation finite-state machine.
/// </summary>
public enum VoiceState
{
    Idle = 0,
    Listening = 1,
    Thinking = 2,
    Speaking = 3,
    Interrupted = 4,
}