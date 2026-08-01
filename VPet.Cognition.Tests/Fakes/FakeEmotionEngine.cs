using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="IEmotionEngine"/> with a mutable state for assertions.
/// </summary>
public sealed class FakeEmotionEngine : IEmotionEngine
{
    public EmotionState State { get; set; } = new();
    public int ApplyCount { get; private set; }
    public int DecayCount { get; private set; }
    public EmotionStimulus? LastStimulus { get; private set; }

    public Task<EmotionState> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(State);

    public Task ApplyAsync(EmotionStimulus stimulus, CancellationToken cancellationToken = default)
    {
        ApplyCount++;
        LastStimulus = stimulus;
        if (stimulus.Primary is { } e)
        {
            var current = State.Vector;
            var updated = e switch
            {
                EmotionType.Joy => current with { Joy = Math.Min(1, current.Joy + stimulus.Intensity) },
                EmotionType.Sadness => current with { Sadness = Math.Min(1, current.Sadness + stimulus.Intensity) },
                EmotionType.Anger => current with { Anger = Math.Min(1, current.Anger + stimulus.Intensity) },
                EmotionType.Calmness => current with { Calmness = Math.Min(1, current.Calmness + stimulus.Intensity) },
                _ => current,
            };
            State.Vector = updated;
            State.LastUpdatedAt = DateTimeOffset.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task DecayAsync(CancellationToken cancellationToken = default)
    {
        DecayCount++;
        State.Vector = EmotionVector.Neutral;
        return Task.CompletedTask;
    }

    public Task<AnimationHint> MapToAnimationAsync(CancellationToken cancellationToken = default)
    {
        var hint = State.Dominant switch
        {
            EmotionType.Joy => new AnimationHint(GraphType.Touch_Head, ModeType.Happy, 0.8),
            EmotionType.Sadness => new AnimationHint(GraphType.Sleep, ModeType.PoorCondition, 0.6),
            EmotionType.Fear => new AnimationHint(GraphType.SideHide_Left_Main, ModeType.PoorCondition, 0.7),
            _ => new AnimationHint(null, null, 0.5),
        };
        return Task.FromResult(hint);
    }

    public Task<VoiceProsody> MapToVoiceAsync(CancellationToken cancellationToken = default)
    {
        var prosody = State.Dominant switch
        {
            EmotionType.Joy => new VoiceProsody(1.2, 1.1, 1.1),
            EmotionType.Sadness => new VoiceProsody(0.8, 0.9, 0.9),
            _ => new VoiceProsody(1.0, 1.0, 1.0),
        };
        return Task.FromResult(prosody);
    }

    public Task<DecisionBias> MapToDecisionAsync(CancellationToken cancellationToken = default)
    {
        var bias = State.Dominant switch
        {
            EmotionType.Joy => new DecisionBias(0.8, 0.8, 0.7),
            EmotionType.Fear => new DecisionBias(0.2, 0.3, 0.2),
            _ => new DecisionBias(0.5, 0.5, 0.5),
        };
        return Task.FromResult(bias);
    }
}