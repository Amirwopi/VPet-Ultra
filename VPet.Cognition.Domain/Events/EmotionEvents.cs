using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Emotion-related domain events.
/// </summary>
public sealed record EmotionChangedEvent(EmotionVector Before, EmotionVector After, Enums.EmotionType Dominant) : CognitionEvent;
public sealed record EmotionDecayedEvent(EmotionVector Current) : CognitionEvent;