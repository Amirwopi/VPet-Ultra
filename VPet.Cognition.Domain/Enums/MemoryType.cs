namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// The 11 cognitive memory types, ordered by retention scope.
/// </summary>
public enum MemoryType
{
    /// <summary>Current conversation context; in-memory only, not persisted.</summary>
    Working = 0,

    /// <summary>Recent turns pending consolidation into long-term.</summary>
    ShortTerm = 1,

    /// <summary>Consolidated facts retained indefinitely (importance-gated).</summary>
    LongTerm = 2,

    /// <summary>Facts about the world or the user ("user likes coffee").</summary>
    Semantic = 3,

    /// <summary>Events ("on 2026-08-01 we talked about X").</summary>
    Episodic = 4,

    /// <summary>User-companion relationship state.</summary>
    Relationship = 5,

    /// <summary>Scratch space for the current agent task.</summary>
    Workspace = 6,

    /// <summary>Emotionally salient events.</summary>
    Emotional = 7,

    /// <summary>The companion's self-reflections.</summary>
    Reflection = 8,

    /// <summary>Active or completed goals.</summary>
    Goal = 9,

    /// <summary>The companion's stable self-concept.</summary>
    Identity = 10,
}