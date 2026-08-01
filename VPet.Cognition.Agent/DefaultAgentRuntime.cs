using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.Events;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.AgentRuntime;

/// <summary>
/// Default <see cref="IAgentRuntime"/> implementation. Manages goals, executes plans
/// step-by-step, dispatches tool calls through the permission system, and emits events.
/// </summary>
public sealed class DefaultAgentRuntime : IAgentRuntime
{
    private readonly IToolProvider _toolProvider;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<DefaultAgentRuntime> _logger;
    private readonly ConcurrentDictionary<GoalId, Goal> _goals = new();
    private readonly ConcurrentDictionary<GoalId, CancellationTokenSource> _ctsMap = new();
    private readonly int _maxStepsPerPlan;
    private readonly int _maxRetries;
    private readonly double _confidenceThreshold;

    public event Action<CognitionEvent>? EventRaised;

    public DefaultAgentRuntime(
        IToolProvider toolProvider,
        IPermissionService permissionService,
        int maxStepsPerPlan,
        int maxRetries,
        double confidenceThreshold,
        ILogger<DefaultAgentRuntime> logger)
    {
        _toolProvider = toolProvider;
        _permissionService = permissionService;
        _maxStepsPerPlan = maxStepsPerPlan;
        _maxRetries = maxRetries;
        _confidenceThreshold = confidenceThreshold;
        _logger = logger;
    }

    public Task<GoalId> SetGoalAsync(string description, GoalPriority priority, CancellationToken cancellationToken = default)
    {
        var goal = new Goal
        {
            Id = GoalId.New(),
            Description = description,
            Priority = priority,
            Status = GoalStatus.Pending,
        };

        _goals[goal.Id] = goal;
        _logger.LogInformation("Goal set: {Id} priority={Priority} desc={Desc}", goal.Id.Value, priority, description);

        EventRaised?.Invoke(new GoalSetEvent(goal.Id.Value, description, priority));
        return Task.FromResult(goal.Id);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var pendingGoals = _goals.Values
            .Where(g => g.Status is GoalStatus.Pending or GoalStatus.Executing)
            .OrderByDescending(g => (int)g.Priority)
            .ToList();

        foreach (var goal in pendingGoals)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (goal.Status == GoalStatus.Completed || goal.Status == GoalStatus.Cancelled) continue;

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ctsMap[goal.Id] = cts;

            try
            {
                goal.Status = GoalStatus.Executing;
                await ExecuteGoalAsync(goal, cts.Token);
                goal.Status = GoalStatus.Completed;
                goal.CompletedAt = DateTimeOffset.UtcNow;
                EventRaised?.Invoke(new GoalCompletedEvent(goal.Id.Value, goal.Description, goal.Confidence));
                _logger.LogInformation("Goal completed: {Id}", goal.Id.Value);
            }
            catch (OperationCanceledException)
            {
                goal.Status = GoalStatus.Cancelled;
                _logger.LogInformation("Goal cancelled: {Id}", goal.Id.Value);
            }
            catch (Exception ex)
            {
                goal.Status = GoalStatus.Failed;
                _logger.LogError(ex, "Goal failed: {Id}", goal.Id.Value);
                EventRaised?.Invoke(new GoalFailedEvent(goal.Id.Value, goal.Description, ex.Message));
            }
            finally
            {
                _ctsMap.TryRemove(goal.Id, out _);
                cts.Dispose();
            }
        }
    }

    public Task CancelAsync(GoalId goalId, CancellationToken cancellationToken = default)
    {
        if (_ctsMap.TryGetValue(goalId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Cancelling goal {Id}", goalId.Value);
        }

        if (_goals.TryGetValue(goalId, out var goal))
        {
            goal.Status = GoalStatus.Cancelled;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Goal>> GetActiveGoalsAsync(CancellationToken cancellationToken = default)
    {
        var active = _goals.Values
            .Where(g => g.Status is GoalStatus.Pending or GoalStatus.Executing or GoalStatus.Planning)
            .ToList();
        return Task.FromResult<IReadOnlyList<Goal>>(active);
    }

    private async Task ExecuteGoalAsync(Goal goal, CancellationToken cancellationToken)
    {
        if (goal.Plan is null)
        {
            // Simple execution: no plan, just mark as done
            goal.Confidence = 1.0;
            return;
        }

        var stepsExecuted = 0;
        foreach (var task in goal.Plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (stepsExecuted >= _maxStepsPerPlan)
            {
                _logger.LogWarning("Goal {Id} hit max steps limit ({Max})", goal.Id.Value, _maxStepsPerPlan);
                break;
            }

            task.Status = AgentTaskStatus.Running;
            await ExecuteTaskAsync(goal, task, cancellationToken);
            stepsExecuted++;
        }

        // Calculate confidence from successful steps
        var totalSteps = goal.Plan.Steps.Count;
        var successSteps = goal.Plan.Steps.Count(s => s.Status == AgentTaskStatus.Done);
        goal.Confidence = totalSteps > 0 ? (double)successSteps / totalSteps : 0;

        if (goal.Confidence < _confidenceThreshold)
        {
            _logger.LogWarning("Goal {Id} confidence {Conf:F2} below threshold {Threshold}",
                goal.Id.Value, goal.Confidence, _confidenceThreshold);
        }
    }

    private async Task ExecuteTaskAsync(Goal goal, AgentTask task, CancellationToken cancellationToken)
    {
        var retries = 0;
        while (retries <= _maxRetries)
        {
            try
            {
                switch (task.Kind)
                {
                    case TaskKind.ToolCall:
                        await ExecuteToolCallAsync(task, cancellationToken);
                        break;

                    case TaskKind.Wait:
                        if (task.Arguments is { } waitArgs && waitArgs.TryGetProperty("seconds", out var secEl) && secEl.ValueKind == JsonValueKind.Number)
                        {
                            var seconds = secEl.GetDouble();
                            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
                        }
                        break;

                    case TaskKind.Remember:
                        // Memory storage is handled by MemoryService at the caller level
                        _logger.LogDebug("Remember task: {Desc}", task.Description);
                        break;

                    case TaskKind.Notify:
                        EventRaised?.Invoke(new NotifyEvent(task.Description));
                        break;

                    case TaskKind.LlmCall:
                        // LLM calls are handled by ConversationService at the caller level
                        _logger.LogDebug("LLM call task: {Desc}", task.Description);
                        break;
                }

                task.Status = AgentTaskStatus.Done;
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                retries++;
                task.RetryCount = retries;
                _logger.LogWarning(ex, "Task {Id} attempt {Retry}/{Max} failed: {Error}",
                    task.Id, retries, _maxRetries, ex.Message);

                if (retries > _maxRetries)
                {
                    task.Status = AgentTaskStatus.Failed;
                    task.Result = new ToolResult(false, string.Empty, ex.Message, TimeSpan.Zero);
                    return;
                }
            }
        }
    }

    private async Task ExecuteToolCallAsync(AgentTask task, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(task.ToolName))
        {
            task.Status = AgentTaskStatus.Failed;
            task.Result = new ToolResult(false, string.Empty, "ToolName is null", TimeSpan.Zero);
            return;
        }

        // Check permission
        var permission = await _permissionService.CheckAsync(task.ToolName, cancellationToken);
        if (permission == PermissionState.Denied)
        {
            task.Status = AgentTaskStatus.Skipped;
            task.Result = new ToolResult(false, string.Empty, "Permission denied", TimeSpan.Zero);
            _logger.LogWarning("Tool {Tool} skipped: permission denied", task.ToolName);
            return;
        }

        if (permission == PermissionState.NotRequested)
        {
            var args = task.Arguments ?? JsonDocument.Parse("{}").RootElement;
            permission = await _permissionService.RequestAsync(task.ToolName, _toolProvider.Schema, args, cancellationToken);
            if (permission != PermissionState.Granted)
            {
                task.Status = AgentTaskStatus.Skipped;
                task.Result = new ToolResult(false, string.Empty, "Permission not granted", TimeSpan.Zero);
                return;
            }
        }

        // Execute the tool
        var toolArgs = task.Arguments ?? JsonDocument.Parse("{}").RootElement;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result = await _toolProvider.ExecuteAsync(toolArgs, cancellationToken);
        sw.Stop();

        task.Result = result;
        _logger.LogInformation("Tool {Tool} executed: success={Success} duration={Ms}ms",
            task.ToolName, result.Success, sw.ElapsedMilliseconds);
    }
}