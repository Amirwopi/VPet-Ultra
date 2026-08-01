using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Entities;

/// <summary>
/// The companion's personality state: stable identity, core traits, evolving adaptive traits,
/// preferences, habits, interests, humor, communication style, and relationship state.
/// </summary>
public sealed class Personality
{
    public Identity Identity { get; init; } = new();
    public TraitSet CoreTraits { get; init; } = new();
    public TraitSet AdaptiveTraits { get; set; } = new();
    public Dictionary<string, double> Preferences { get; set; } = new();
    public List<Habit> Habits { get; set; } = new();
    public List<Interest> Interests { get; set; } = new();
    public HumorStyle Humor { get; set; } = HumorStyle.Playful;
    public CommunicationStyle CommunicationStyle { get; set; } = new();
    public RelationshipState Relationship { get; set; } = new();
    public DateTimeOffset LastEvolvedAt { get; set; } = DateTimeOffset.UtcNow;
    public int EvolutionCount { get; set; }
}

public sealed record Identity
{
    public string Name { get; init; } = "Vpet";
    public string Description { get; init; } = "A desktop companion.";
}

/// <summary>A set of named traits with values in [0, 1].</summary>
public sealed class TraitSet
{
    public double Openness { get; set; }
    public double Conscientiousness { get; set; }
    public double Extraversion { get; set; }
    public double Agreeableness { get; set; }
    public double Neuroticism { get; set; }

    // Adaptive-only traits (ignored for CoreTraits seed but used in AdaptiveTraits)
    public double Confidence { get; set; }
    public double Curiosity { get; set; }
    public double Creativity { get; set; }
    public double Playfulness { get; set; }
    public double Assertiveness { get; set; }
    public double Patience { get; set; }
    public double Warmth { get; set; }

    public double this[string trait] => trait.ToLowerInvariant() switch
    {
        "openness" => Openness, "conscientiousness" => Conscientiousness,
        "extraversion" => Extraversion, "agreeableness" => Agreeableness,
        "neuroticism" => Neuroticism, "confidence" => Confidence,
        "curiosity" => Curiosity, "creativity" => Creativity,
        "playfulness" => Playfulness, "assertiveness" => Assertiveness,
        "patience" => Patience, "warmth" => Warmth,
        _ => 0,
    };
}

public sealed record Habit(string Trigger, string Behavior, double Strength);

public sealed record Interest(string Topic, double Level, DateTimeOffset LastExplored);

public enum HumorStyle { Dry, Playful, Sarcastic, Wholesome, Absurdist, None }

public sealed record CommunicationStyle
{
    public double Verbosity { get; set; } = 0.5;
    public double Formality { get; set; } = 0.3;
    public double EmojiUsage { get; set; } = 0.3;
    public double QuestionRate { get; set; } = 0.3;
    public double InitiativeRate { get; set; } = 0.4;
}

public sealed class RelationshipState
{
    public double Trust { get; set; }
    public double Familiarity { get; set; }
    public double Affection { get; set; }
    public int SharedJokes { get; set; }
    public int DeepConversations { get; set; }
    public DateTimeOffset RelationshipStart { get; set; } = DateTimeOffset.UtcNow;
    public List<string> SharedInterests { get; set; } = new();
    public List<string> PetNames { get; set; } = new();
}