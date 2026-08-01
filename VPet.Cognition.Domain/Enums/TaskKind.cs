namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// The kind of step an agent plan may contain.
/// </summary>
public enum TaskKind
{
    /// <summary>An LLM chat call.</summary>
    LlmCall = 0,

    /// <summary>A tool execution (goes through the permission system).</summary>
    ToolCall = 1,

    /// <summary>Wait for a duration.</summary>
    Wait = 2,

    /// <summary>Store an episodic memory.</summary>
    Remember = 3,

    /// <summary>Show a message in the conversation UI / speak via TTS.</summary>
    Notify = 4,
}