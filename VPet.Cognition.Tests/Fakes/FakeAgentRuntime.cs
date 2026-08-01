using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.Events;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="IAgentRuntime"/> that records goals without executing them.
/// </summary>
public sealed class FakeAgentRuntime : IAgentRuntime
{
    private readonly List<Goal> _goals = new();
    public IReadOnlyList<Goal> Goals => _goals;
    public event Action<CognitionEvent>? EventRaised;

    public Task<GoalId> SetGoalAsync(string description, GoalPriority priority, CancellationToken cancellationToken = default)
    {
        var goal = new Goal { Description = description, Priority = priority };
        _goals.Add(goal);
        EventRaised?.Invoke(new PlanCreatedEvent(goal.Id, 0));
        return Task.FromResult(goal.Id);
    }

    public Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CancelAsync(GoalId goalId, CancellationToken cancellationToken = default)
    {
        var goal = _goals.FirstOrDefault(g => g.Id == goalId);
        if (goal is not null)
            goal.Status = GoalStatus.Cancelled;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Goal>> GetActiveGoalsAsync(CancellationToken cancellationToken = default)
    {
        var active = _goals.Where(g => g.Status is GoalStatus.Pending or GoalStatus.Planning or GoalStatus.Executing).ToList();
        return Task.FromResult((IReadOnlyList<Goal>)active);
    }
}