using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Domain.ValueObjects;

/// <summary>
/// A 10-dimensional emotion vector, one value per <see cref="EmotionType"/> in [0, 1].
/// </summary>
public readonly record struct EmotionVector
{
    public double Joy { get; init; }
    public double Sadness { get; init; }
    public double Anger { get; init; }
    public double Curiosity { get; init; }
    public double Stress { get; init; }
    public double Excitement { get; init; }
    public double Fear { get; init; }
    public double Calmness { get; init; }
    public double Surprise { get; init; }
    public double Frustration { get; init; }

    public EmotionVector(
        double joy = 0, double sadness = 0, double anger = 0, double curiosity = 0,
        double stress = 0, double excitement = 0, double fear = 0, double calmness = 0,
        double surprise = 0, double frustration = 0)
    {
        Joy = Clamp(joy);
        Sadness = Clamp(sadness);
        Anger = Clamp(anger);
        Curiosity = Clamp(curiosity);
        Stress = Clamp(stress);
        Excitement = Clamp(excitement);
        Fear = Clamp(fear);
        Calmness = Clamp(calmness);
        Surprise = Clamp(surprise);
        Frustration = Clamp(frustration);
    }

    private static double Clamp(double v) => Math.Max(0, Math.Min(1, v));

    /// <summary>Access a dimension by <see cref="EmotionType"/>.</summary>
    public double this[EmotionType e] => e switch
    {
        EmotionType.Joy => Joy,
        EmotionType.Sadness => Sadness,
        EmotionType.Anger => Anger,
        EmotionType.Curiosity => Curiosity,
        EmotionType.Stress => Stress,
        EmotionType.Excitement => Excitement,
        EmotionType.Fear => Fear,
        EmotionType.Calmness => Calmness,
        EmotionType.Surprise => Surprise,
        EmotionType.Frustration => Frustration,
        _ => 0,
    };

    /// <summary>The emotion with the highest value.</summary>
    public EmotionType Dominant
    {
        get
        {
            var vals = new (EmotionType, double)[]
            {
                (EmotionType.Joy, Joy), (EmotionType.Sadness, Sadness),
                (EmotionType.Anger, Anger), (EmotionType.Curiosity, Curiosity),
                (EmotionType.Stress, Stress), (EmotionType.Excitement, Excitement),
                (EmotionType.Fear, Fear), (EmotionType.Calmness, Calmness),
                (EmotionType.Surprise, Surprise), (EmotionType.Frustration, Frustration),
            };
            EmotionType dom = EmotionType.Calmness;
            double max = -1;
            foreach (var (t, v) in vals)
                if (v > max) { max = v; dom = t; }
            return dom;
        }
    }

    /// <summary>Computed arousal: avg(high) - avg(low).</summary>
    public double Arousal =>
        ((Excitement + Stress + Anger + Surprise + Fear) / 5.0)
        - ((Calmness + Sadness) / 2.0);

    /// <summary>Computed valence: avg(positive) - avg(negative).</summary>
    public double Valence =>
        ((Joy + Calmness + Curiosity) / 3.0)
        - ((Sadness + Anger + Stress + Fear + Frustration) / 5.0);

    public static EmotionVector Neutral => new(calmness: 0.5);

    public override string ToString() => $"Emotion({Dominant})";
}