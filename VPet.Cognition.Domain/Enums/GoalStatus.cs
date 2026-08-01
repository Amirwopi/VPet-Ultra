namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// Lifecycle state of an agent goal.
/// </summary>
public enum GoalStatus
{
    Pending = 0,
    Planning = 1,
    Executing = 2,
    Verifying = 3,
    Reflecting = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7,
}