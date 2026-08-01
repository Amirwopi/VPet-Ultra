namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// The 10 supported emotions, each a dimension in <see cref="ValueObjects.EmotionVector"/>.
/// </summary>
public enum EmotionType
{
    Joy = 0,
    Sadness = 1,
    Anger = 2,
    Curiosity = 3,
    Stress = 4,
    Excitement = 5,
    Fear = 6,
    Calmness = 7,
    Surprise = 8,
    Frustration = 9,
}