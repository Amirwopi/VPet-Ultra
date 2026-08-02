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

    /// <summary>Optional LLM provider for planning.</summary>
    public ILLMProvider? LlmProvider { get; set; }

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

    private async Task<Plan> PlanGoalAsync(string description, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Planning goal: {Desc}", description);

        var prompt = "Break down the following goal into steps. Each step should be a simple action.\n" +
            "Respond as JSON array: [{\"kind\":\"ToolCall\",\"description\":\"...\",\"tool\":\"...\",\"args\":{}}]\n" +
            "Available kinds: ToolCall, Wait, Remember, Notify, LlmCall\n" +
            $"Goal: {description}\n" +
            $"Max steps: {_maxStepsPerPlan}";

        var request = new ChatRequest
        {
            SystemPrompt = "You are a planning module. Break goals into actionable steps. Respond in JSON only.",
            Messages = new[] { new ChatMessageDto("user", prompt) },
            MaxTokens = 512,
            Temperature = 0.3,
        };

        var result = await LlmProvider!.ChatAsync(request, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            _logger.LogWarning("LLM planning failed: {Error}", result.Error);
            return new Plan { Steps = new() };
        }

        var steps = ParsePlanSteps(result.Value.Content);
        _logger.LogInformation("Plan created with {Count} steps", steps.Count);
        return new Plan { Steps = steps };
    }

    private List<AgentTask> ParsePlanSteps(string json)
    {
        var steps = new List<AgentTask>();
        try
        {
            var jsonStart = json.IndexOf('[');
            var jsonEnd = json.LastIndexOf(']');
            if (jsonStart < 0 || jsonEnd < 0) return steps;

            var jsonArray = json.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(jsonArray);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var kind = element.TryGetProperty("kind", out var kEl) ? kEl.GetString() : "ToolCall";
                var desc = element.TryGetProperty("description", out var dEl) ? dEl.GetString() : "";
                var tool = element.TryGetProperty("tool", out var tEl) ? tEl.GetString() : "";
                var args = element.TryGetProperty("args", out var aEl) ? aEl.GetRawText() : "{}";

                steps.Add(new AgentTask
                {
                    Description = desc ?? "",
                    Kind = Enum.Parse<TaskKind>(kind ?? "ToolCall", ignoreCase: true),
                    ToolName = tool,
                    Arguments = JsonDocument.Parse(args).RootElement.Clone(),
                    Status = AgentTaskStatus.Pending,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse plan steps from LLM response.");
        }
        return steps;
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
        // If no plan exists, try to create one via LLM
        if (goal.Plan is null && LlmProvider is not null)
        {
            try
            {
                goal.Plan = await PlanGoalAsync(goal.Description, cancellationToken);
                EventRaised?.Invoke(new PlanCreatedEvent(goal.Id, goal.Plan.Steps.Count));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Planning failed for goal {Id}", goal.Id.Value);
            }
        }

        if (goal.Plan is null)
        {
            // No plan and no LLM — mark as done with low confidence
            goal.Confidence = 0.5;
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
                        _logger.LogDebug("Remember task: {Desc}", task.Description);
                        break;

                    case TaskKind.Notify:
                        EventRaised?.Invoke(new NotifyEvent(task.Description));
                        break;

                    case TaskKind.LlmCall:
                        _logger.LogDebug("LLM call task: {Desc}", task.Description);
                        break;
                }

                // Verify the task result
                if (task.Result is { } result && !result.Success)
                {
                    throw new InvalidOperationException($"Task failed: {result.Error}");
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

                    // Reflect on failure
                    _logger.LogInformation("Task {Id} failed after {Retries} retries. Reflecting...",
                        task.Id, retries);
                    EventRaised?.Invoke(new TaskFailedEvent(task.Id, ex.Message, retries));
                    return;
                }

                // Exponential backoff: 1s, 2s, 4s
                var backoff = TimeSpan.FromSeconds(Math.Pow(2, retries - 1));
                _logger.LogDebug("Backing off for {Delay} before retry {Retry}", backoff, retries);
                await Task.Delay(backoff, cancellationToken);
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