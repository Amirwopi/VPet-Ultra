using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// A hint to the animation system derived from the current emotion.
/// </summary>
public sealed record AnimationHint(GraphType? PreferredGraph, ModeType? PreferredMode, double Intensity);

/// <summary>
/// Prosody parameters for TTS derived from the current emotion.
/// </summary>
public sealed record VoiceProsody(double SpeedMultiplier, double PitchMultiplier, double VolumeMultiplier);

/// <summary>
/// A bias applied to agent decisions derived from the current emotion.
/// </summary>
public sealed record DecisionBias(double RiskAppetite, double Proactivity, double ToolWillingness);

/// <summary>
/// Abstraction over the emotion engine.
/// </summary>
public interface IEmotionEngine
{
    Task<EmotionState> GetAsync(CancellationToken cancellationToken = default);
    Task ApplyAsync(EmotionStimulus stimulus, CancellationToken cancellationToken = default);
    Task DecayAsync(CancellationToken cancellationToken = default);
    Task<AnimationHint> MapToAnimationAsync(CancellationToken cancellationToken = default);
    Task<VoiceProsody> MapToVoiceAsync(CancellationToken cancellationToken = default);
    Task<DecisionBias> MapToDecisionAsync(CancellationToken cancellationToken = default);
}