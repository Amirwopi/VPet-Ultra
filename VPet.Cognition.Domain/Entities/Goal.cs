using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Entities;

/// <summary>
/// An agent goal with its plan and lifecycle state.
/// </summary>
public sealed class Goal
{
    public GoalId Id { get; init; } = GoalId.New();
    public string Description { get; init; } = string.Empty;
    public GoalPriority Priority { get; set; } = GoalPriority.Normal;
    public GoalStatus Status { get; set; } = GoalStatus.Pending;
    public Plan? Plan { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public double Confidence { get; set; }
}

public sealed class Plan
{
    public List<AgentTask> Steps { get; init; } = new();
    public string Rationale { get; init; } = string.Empty;
}

public sealed class AgentTask
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Description { get; init; } = string.Empty;
    public TaskKind Kind { get; init; }
    public string? ToolName { get; init; }
    public System.Text.Json.JsonElement? Arguments { get; set; }
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;
    public ToolResult? Result { get; set; }
    public int RetryCount { get; set; }
}