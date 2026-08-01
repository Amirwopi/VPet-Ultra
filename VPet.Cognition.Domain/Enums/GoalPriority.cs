namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// Priority of an agent goal. Higher values preempt lower ones in the priority queue.
/// </summary>
public enum GoalPriority
{
    Low = 0,
    Normal = 1,
    High = 2,

    /// <summary>Only user-initiated goals may use this priority.</summary>
    Critical = 3,
}