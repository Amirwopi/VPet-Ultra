using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Agent-related domain events.
/// </summary>
public sealed record GoalSetEvent(Guid GoalId, string Description, GoalPriority Priority) : CognitionEvent;
public sealed record GoalCompletedEvent(Guid GoalId, string Description, double Confidence) : CognitionEvent;
public sealed record GoalFailedEvent(Guid GoalId, string Description, string Error) : CognitionEvent;
public sealed record PlanCreatedEvent(GoalId GoalId, int StepCount) : CognitionEvent;
public sealed record TaskStartedEvent(GoalId GoalId, string TaskId) : CognitionEvent;
public sealed record TaskCompletedEvent(GoalId GoalId, string TaskId, bool Success) : CognitionEvent;
public sealed record DecisionMadeEvent(GoalId GoalId, double Confidence, bool Autonomous) : CognitionEvent;
public sealed record NotifyEvent(string Message) : CognitionEvent;
public sealed record TaskFailedEvent(string TaskId, string Error, int RetryCount) : CognitionEvent;