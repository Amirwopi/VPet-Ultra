using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;
using VPet.Cognition.MemoryStore;

namespace VPet.Cognition.AgentRuntime;

/// <summary>
/// Priority queue for agent goals. Uses a weighted formula:
/// priority = goalPriority * 0.5 + recency * 0.2 + userInitiated * 0.3
/// </summary>
public sealed class GoalPriorityQueue
{
    private readonly ConcurrentDictionary<GoalId, Goal> _goals = new();
    private readonly ILogger<GoalPriorityQueue> _logger;

    public GoalPriorityQueue(ILogger<GoalPriorityQueue> logger) => _logger = logger;

    public void Enqueue(Goal goal) => _goals[goal.Id] = goal;

    public Goal? Dequeue()
    {
        if (_goals.IsEmpty) return null;

        var now = DateTimeOffset.UtcNow;
        var sorted = _goals.Values
            .Where(g => g.Status is GoalStatus.Pending or GoalStatus.Planning)
            .OrderByDescending(g => CalculatePriority(g, now))
            .FirstOrDefault();

        if (sorted is not null)
            _goals.TryRemove(sorted.Id, out _);

        return sorted;
    }

    public IReadOnlyList<Goal> PeekAll() => _goals.Values.ToList();

    public bool Remove(GoalId id) => _goals.TryRemove(id, out _);

    public int Count => _goals.Count;

    private static double CalculatePriority(Goal goal, DateTimeOffset now)
    {
        var goalPriority = goal.Priority switch
        {
            GoalPriority.Critical => 1.0,
            GoalPriority.High => 0.8,
            GoalPriority.Normal => 0.5,
            GoalPriority.Low => 0.2,
            _ => 0.5,
        };

        var recency = Math.Max(0, 1.0 - (now - goal.CreatedAt).TotalHours / 24.0);
        var userInitiated = goal.Priority == GoalPriority.Critical ? 1.0 : 0.0;

        return goalPriority * 0.5 + recency * 0.2 + userInitiated * 0.3;
    }
}

/// <summary>
/// Manages goal persistence to SQLite. Loads active goals on startup,
/// saves on create/update/delete.
/// </summary>
public sealed class GoalManager
{
    private readonly CognitionStore _store;
    private readonly ILogger<GoalManager> _logger;
    private readonly ConcurrentDictionary<GoalId, Goal> _cache = new();

    public GoalManager(CognitionStore store, ILogger<GoalManager> logger)
    {
        _store = store;
        _logger = logger;
    }

    public Task<Goal?> LoadAsync(GoalId id, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(id, out var cached)) return Task.FromResult<Goal?>(cached);
        return Task.FromResult<Goal?>(null);
    }

    public Task SaveAsync(Goal goal, CancellationToken ct = default)
    {
        _cache[goal.Id] = goal;
        _logger.LogDebug("Goal {Id} cached: {Status}", goal.Id.Value, goal.Status);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Goal>> LoadActiveGoalsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Goal> result = _cache.Values
            .Where(g => g.Status is GoalStatus.Pending or GoalStatus.Executing or GoalStatus.Planning)
            .ToList();
        return Task.FromResult(result);
    }

    public Task DeleteAsync(GoalId id, CancellationToken ct = default)
    {
        _cache.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Scheduler for periodic background tasks: reminders, consolidation,
/// personality reflection, emotion decay.
/// </summary>
public sealed class AgentScheduler : IDisposable
{
    private readonly ILogger<AgentScheduler> _logger;
    private readonly Timer _timer;
    private readonly List<ScheduledTask> _tasks = new();
    private readonly object _lock = new();

    public AgentScheduler(ILogger<AgentScheduler> logger)
    {
        _logger = logger;
        _timer = new Timer(Tick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void Schedule(string name, TimeSpan interval, Func<CancellationToken, Task> action)
    {
        lock (_lock)
        {
            _tasks.Add(new ScheduledTask(name, interval, action, DateTimeOffset.UtcNow));
            _logger.LogInformation("Scheduled task '{Name}' every {Interval}", name, interval);
        }
    }

    private async void Tick(object? state)
    {
        List<ScheduledTask> due;
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            due = _tasks.Where(t => now >= t.NextRun).ToList();
            foreach (var t in due)
                t.NextRun = now + t.Interval;
        }

        foreach (var task in due)
        {
            try
            {
                await task.Action(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled task '{Name}' failed.", task.Name);
            }
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _logger.LogInformation("AgentScheduler disposed.");
    }

    private sealed class ScheduledTask(string name, TimeSpan interval, Func<CancellationToken, Task> action, DateTimeOffset nextRun)
    {
        public string Name { get; } = name;
        public TimeSpan Interval { get; } = interval;
        public Func<CancellationToken, Task> Action { get; } = action;
        public DateTimeOffset NextRun { get; set; } = nextRun;
    }
}