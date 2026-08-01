using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Entities;

/// <summary>
/// The companion's current emotion state.
/// </summary>
public sealed class EmotionState
{
    public EmotionVector Vector { get; set; } = EmotionVector.Neutral;
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public EmotionType Dominant => Vector.Dominant;
    public double Arousal => Vector.Arousal;
    public double Valence => Vector.Valence;
}

/// <summary>
/// A stimulus that changes the emotion state.
/// </summary>
public sealed record EmotionStimulus
{
    public EmotionType? Primary { get; init; }
    public double Intensity { get; init; }
    public string? Source { get; init; }
    public Dictionary<EmotionType, double>? Blends { get; init; }
}