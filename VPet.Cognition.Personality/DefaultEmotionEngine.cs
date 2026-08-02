using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.Events;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.PersonalityEngine;

/// <summary>
/// Default <see cref="IEmotionEngine"/> implementation. Maintains a 10-dimensional
/// emotion vector that blends with stimuli and decays over time toward a personality
/// baseline. Publishes <see cref="EmotionChangedEvent"/> on each change.
/// </summary>
public sealed class DefaultEmotionEngine : IEmotionEngine
{
    private readonly ILogger<DefaultEmotionEngine> _logger;
    private readonly double _blendFactor;
    private readonly double _decayRatePerSec;
    private readonly double _antagonisticSuppression;
    private EmotionVector _vector = EmotionVector.Neutral;
    private DateTimeOffset _lastUpdate = DateTimeOffset.UtcNow;

    /// <summary>Optional event bus for publishing EmotionChangedEvent.</summary>
    public IEventBus? EventBus { get; set; }

    /// <summary>Optional personality baseline for decay target (0–1 per dimension).</summary>
    public EmotionVector? PersonalityBaseline { get; set; }

    public DefaultEmotionEngine(
        double blendFactor,
        double decayRatePerSec,
        double antagonisticSuppression,
        ILogger<DefaultEmotionEngine> logger)
    {
        _blendFactor = blendFactor;
        _decayRatePerSec = decayRatePerSec;
        _antagonisticSuppression = antagonisticSuppression;
        _logger = logger;
    }

    public Task<EmotionState> GetAsync(CancellationToken cancellationToken = default)
    {
        var state = new EmotionState
        {
            Vector = _vector,
            LastUpdatedAt = _lastUpdate,
        };
        return Task.FromResult(state);
    }

    public Task ApplyAsync(EmotionStimulus stimulus, CancellationToken cancellationToken = default)
    {
        var before = _vector;

        // Apply decay first (time-based)
        DecayInternal();

        if (stimulus.Primary is { } primary)
        {
            var intensity = stimulus.Intensity;
            var current = _vector[primary];

            // Blend: new = current * (1 - blend) + stimulus * blend
            var newValue = current * (1 - _blendFactor) + intensity * _blendFactor;

            _vector = SetDimension(_vector, primary, newValue);

            // Suppress antagonistic emotions
            _vector = SuppressAntagonists(primary);
        }

        // Apply blends if provided
        if (stimulus.Blends is not null)
        {
            foreach (var (emotion, intensity) in stimulus.Blends)
            {
                var current = _vector[emotion];
                var newValue = current * (1 - _blendFactor) + intensity * _blendFactor;
                _vector = SetDimension(_vector, emotion, newValue);
            }
        }

        _lastUpdate = DateTimeOffset.UtcNow;
        _logger.LogInformation("Emotion applied: {Dominant} arousal={Arousal:F2} valence={Valence:F2}",
            _vector.Dominant, _vector.Arousal, _vector.Valence);

        // Publish EmotionChangedEvent
        if (EventBus is not null && !before.Equals(_vector))
        {
            EventBus.Publish(new EmotionChangedEvent(before, _vector, _vector.Dominant), cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task DecayAsync(CancellationToken cancellationToken = default)
    {
        var before = _vector;
        DecayInternal();

        // Publish decay event if significant change
        if (EventBus is not null && !before.Equals(_vector))
        {
            EventBus.Publish(new EmotionDecayedEvent(_vector), cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task<AnimationHint> MapToAnimationAsync(CancellationToken cancellationToken = default)
    {
        var dominant = _vector.Dominant;
        var intensity = _vector[dominant];

        GraphType? graph = dominant switch
        {
            EmotionType.Joy => GraphType.Say,
            EmotionType.Sadness => GraphType.Sleep,
            EmotionType.Anger => GraphType.Touch_Body,
            EmotionType.Curiosity => GraphType.Default,
            EmotionType.Excitement => GraphType.StartUP,
            EmotionType.Fear => GraphType.Idel,
            EmotionType.Calmness => GraphType.Idel,
            EmotionType.Stress => GraphType.Work,
            EmotionType.Surprise => GraphType.Touch_Head,
            EmotionType.Frustration => GraphType.Work,
            _ => null,
        };

        ModeType? mode = dominant switch
        {
            EmotionType.Joy or EmotionType.Excitement or EmotionType.Calmness => ModeType.Happy,
            EmotionType.Sadness or EmotionType.Fear or EmotionType.Stress => ModeType.PoorCondition,
            EmotionType.Anger or EmotionType.Frustration => ModeType.Nomal,
            _ => ModeType.Nomal,
        };

        return Task.FromResult(new AnimationHint(graph, mode, intensity));
    }

    public Task<VoiceProsody> MapToVoiceAsync(CancellationToken cancellationToken = default)
    {
        var dominant = _vector.Dominant;
        var dominantIntensity = _vector[dominant];

        var energy = dominant switch
        {
            EmotionType.Joy => 0.6,
            EmotionType.Excitement => 1.0,
            EmotionType.Anger => 0.8,
            EmotionType.Surprise => 0.9,
            EmotionType.Frustration => 0.7,
            EmotionType.Stress => 0.6,
            EmotionType.Fear => 0.5,
            EmotionType.Curiosity => 0.3,
            EmotionType.Sadness => -0.3,
            EmotionType.Calmness => -0.2,
            _ => 0.0,
        };

        var speed = 1.0 + energy * dominantIntensity * 0.3;
        var pitch = 1.0 + _vector.Valence * 0.2;
        var volume = 1.0 + Math.Abs(energy) * dominantIntensity * 0.1;

        return Task.FromResult(new VoiceProsody(
            Math.Clamp(speed, 0.5, 2.0),
            Math.Clamp(pitch, 0.5, 2.0),
            Math.Clamp(volume, 0.5, 2.0)));
    }

    public Task<DecisionBias> MapToDecisionAsync(CancellationToken cancellationToken = default)
    {
        var riskAppetite = 0.5 - _vector.Fear * 0.3 + _vector.Excitement * 0.2;
        var proactivity = 0.3 + _vector.Curiosity * 0.4 + _vector.Excitement * 0.3;
        var toolWillingness = 0.5 + _vector.Curiosity * 0.3 - _vector.Stress * 0.2;

        return Task.FromResult(new DecisionBias(
            Math.Clamp(riskAppetite, 0, 1),
            Math.Clamp(proactivity, 0, 1),
            Math.Clamp(toolWillingness, 0, 1)));
    }

    private void DecayInternal()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = (now - _lastUpdate).TotalSeconds;
        if (elapsed <= 0) return;

        var decay = _decayRatePerSec * elapsed;

        // Decay toward personality baseline if set, otherwise toward neutral (0 / calmness=1)
        var baseline = PersonalityBaseline ?? new EmotionVector(
            joy: 0, sadness: 0, anger: 0, curiosity: 0.1,
            stress: 0, excitement: 0, fear: 0,
            calmness: 0.5, surprise: 0, frustration: 0);

        _vector = new EmotionVector(
            joy: DecayToward(_vector.Joy, baseline.Joy, decay),
            sadness: DecayToward(_vector.Sadness, baseline.Sadness, decay),
            anger: DecayToward(_vector.Anger, baseline.Anger, decay),
            curiosity: DecayToward(_vector.Curiosity, baseline.Curiosity, decay),
            stress: DecayToward(_vector.Stress, baseline.Stress, decay),
            excitement: DecayToward(_vector.Excitement, baseline.Excitement, decay),
            fear: DecayToward(_vector.Fear, baseline.Fear, decay),
            calmness: DecayToward(_vector.Calmness, baseline.Calmness, decay * 0.5),
            surprise: DecayToward(_vector.Surprise, baseline.Surprise, decay * 2),
            frustration: DecayToward(_vector.Frustration, baseline.Frustration, decay));

        _lastUpdate = now;
    }

    private static double DecayToward(double current, double target, double decay)
    {
        if (current > target)
            return Math.Max(target, current - decay);
        if (current < target)
            return Math.Min(target, current + decay);
        return current;
    }

    private EmotionVector SuppressAntagonists(EmotionType primary)
    {
        // Antagonistic pairs: joy↔sadness, calmness↔anger, excitement↔fear, calmness↔stress
        var suppression = _antagonisticSuppression;
        return primary switch
        {
            EmotionType.Joy => _vector with { Sadness = Math.Max(0, _vector.Sadness - suppression) },
            EmotionType.Sadness => _vector with { Joy = Math.Max(0, _vector.Joy - suppression) },
            EmotionType.Calmness => _vector with
            {
                Anger = Math.Max(0, _vector.Anger - suppression),
                Stress = Math.Max(0, _vector.Stress - suppression),
            },
            EmotionType.Anger => _vector with { Calmness = Math.Max(0, _vector.Calmness - suppression) },
            EmotionType.Stress => _vector with { Calmness = Math.Max(0, _vector.Calmness - suppression) },
            EmotionType.Excitement => _vector with { Fear = Math.Max(0, _vector.Fear - suppression) },
            EmotionType.Fear => _vector with { Excitement = Math.Max(0, _vector.Excitement - suppression) },
            _ => _vector,
        };
    }

    private static EmotionVector SetDimension(EmotionVector v, EmotionType type, double value)
    {
        var clamped = Math.Max(0, Math.Min(1, value));
        return type switch
        {
            EmotionType.Joy => v with { Joy = clamped },
            EmotionType.Sadness => v with { Sadness = clamped },
            EmotionType.Anger => v with { Anger = clamped },
            EmotionType.Curiosity => v with { Curiosity = clamped },
            EmotionType.Stress => v with { Stress = clamped },
            EmotionType.Excitement => v with { Excitement = clamped },
            EmotionType.Fear => v with { Fear = clamped },
            EmotionType.Calmness => v with { Calmness = clamped },
            EmotionType.Surprise => v with { Surprise = clamped },
            EmotionType.Frustration => v with { Frustration = clamped },
            _ => v,
        };
    }
}