using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VPet.Cognition.Host.Options;
using VPet.Cognition.MemoryStore;

namespace VPet.Cognition.Host.Services;

/// <summary>
/// Background service that periodically runs memory consolidation, forgetting,
/// and compression. Runs on a configurable interval (default: 5 minutes).
/// </summary>
public sealed class MemoryMaintenanceService : BackgroundService
{
    private readonly IMemoryStore _memoryStore;
    private readonly CognitionStore _cognitionStore;
    private readonly IOptions<CognitionOptions> _options;
    private readonly ILogger<MemoryMaintenanceService> _logger;

    public MemoryMaintenanceService(
        IMemoryStore memoryStore,
        CognitionStore cognitionStore,
        IOptions<CognitionOptions> options,
        ILogger<MemoryMaintenanceService> logger)
    {
        _memoryStore = memoryStore;
        _cognitionStore = cognitionStore;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consolidationInterval = _options.Value.Memory.ConsolidationInterval;
        var forgettingInterval = _options.Value.Memory.ForgettingInterval;

        _logger.LogInformation("MemoryMaintenanceService started: consolidation={Cons}, forgetting={Forget}",
            consolidationInterval, forgettingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(consolidationInterval, stoppingToken);

                // 1. Consolidate: reduce importance of old memories
                await _memoryStore.ConsolidateAsync(stoppingToken);
                _logger.LogDebug("Memory consolidation completed.");

                // 2. Forget: mark low-importance, old memories as forgotten
                await _memoryStore.ForgetAsync(stoppingToken);
                _logger.LogDebug("Memory forgetting completed.");

                // 3. Compress: hard-delete forgotten memories older than 7 days
                await _memoryStore.CompressAsync(stoppingToken);
                _logger.LogDebug("Memory compression completed.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory maintenance cycle failed.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("MemoryMaintenanceService stopped.");
    }
}

/// <summary>
/// Background service that periodically saves emotion state to persistence.
/// Runs on a configurable interval (default: 1 minute).
/// </summary>
public sealed class EmotionPersistenceService : BackgroundService
{
    private readonly IEmotionEngine _emotionEngine;
    private readonly CognitionStore _cognitionStore;
    private readonly ILogger<EmotionPersistenceService> _logger;
    private readonly TimeSpan _interval;

    public EmotionPersistenceService(
        IEmotionEngine emotionEngine,
        CognitionStore cognitionStore,
        IOptions<CognitionOptions> options,
        ILogger<EmotionPersistenceService> logger)
    {
        _emotionEngine = emotionEngine;
        _cognitionStore = cognitionStore;
        _logger = logger;
        _interval = options.Value.Emotion.PersistInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmotionPersistenceService started: interval={Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);

                var state = await _emotionEngine.GetAsync(stoppingToken);
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    vector = state.Vector,
                    dominant = state.Dominant.ToString(),
                    updatedAt = state.LastUpdatedAt,
                });

                await _cognitionStore.SaveEmotionAsync(json);
                _logger.LogDebug("Emotion state persisted.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Emotion persistence failed.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("EmotionPersistenceService stopped.");
    }
}

/// <summary>
/// Background service that periodically runs personality evaluation.
/// Checks core trait drift and resets if needed.
/// </summary>
public sealed class PersonalityEvaluationService : BackgroundService
{
    private readonly IPersonalityEngine _personalityEngine;
    private readonly ILogger<PersonalityEvaluationService> _logger;
    private readonly TimeSpan _interval;

    public PersonalityEvaluationService(
        IPersonalityEngine personalityEngine,
        IOptions<CognitionOptions> options,
        ILogger<PersonalityEvaluationService> logger)
    {
        _personalityEngine = personalityEngine;
        _logger = logger;
        // Evaluate every 10x the reflection interval
        _interval = TimeSpan.FromMinutes(options.Value.Personality.EvaluationIntervalTurns);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PersonalityEvaluationService started: interval={Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await _personalityEngine.EvaluateAsync(stoppingToken);
                _logger.LogDebug("Personality evaluation completed.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Personality evaluation failed.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("PersonalityEvaluationService stopped.");
    }
}