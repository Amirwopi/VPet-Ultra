using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Agent-related domain events.
/// </summary>
public sealed record PlanCreatedEvent(GoalId GoalId, int StepCount) : CognitionEvent;
public sealed record TaskStartedEvent(GoalId GoalId, string TaskId) : CognitionEvent;
public sealed record TaskCompletedEvent(GoalId GoalId, string TaskId, bool Success) : CognitionEvent;
public sealed record DecisionMadeEvent(GoalId GoalId, double Confidence, bool Autonomous) : CognitionEvent;