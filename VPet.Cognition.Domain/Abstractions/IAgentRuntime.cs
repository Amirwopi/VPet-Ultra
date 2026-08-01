using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// Abstraction over the autonomous agent runtime.
/// </summary>
public interface IAgentRuntime
{
    Task<GoalId> SetGoalAsync(string description, GoalPriority priority, CancellationToken cancellationToken = default);
    Task RunAsync(CancellationToken cancellationToken = default);
    Task CancelAsync(GoalId goalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Goal>> GetActiveGoalsAsync(CancellationToken cancellationToken = default);
    event Action<Events.CognitionEvent>? EventRaised;
}