namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// State of a single agent task within a plan. Named <c>AgentTaskStatus</c> to avoid
/// collision with <see cref="System.Threading.Tasks.TaskStatus"/>.
/// </summary>
public enum AgentTaskStatus
{
    Pending = 0,
    Running = 1,
    Done = 2,
    Failed = 3,
    Skipped = 4,
}