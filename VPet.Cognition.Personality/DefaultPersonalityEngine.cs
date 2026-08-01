using System.Text;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.PersonalityEngine;

/// <summary>
/// Seed values for initializing a personality.
/// </summary>
public sealed class PersonalitySeed
{
    public string Name { get; set; } = "Vpet";
    public string Description { get; set; } = "A desktop companion.";
}

/// <summary>
/// Default <see cref="IPersonalityEngine"/> implementation. Maintains an in-memory
/// personality state that evolves over time based on conversation deltas.
/// </summary>
public sealed class DefaultPersonalityEngine : IPersonalityEngine
{
    private readonly ILogger<DefaultPersonalityEngine> _logger;
    private Personality _personality;
    private readonly double _evolutionRate;
    private readonly double _coreTraitMaxDrift;

    public DefaultPersonalityEngine(
        PersonalitySeed seed,
        double evolutionRate,
        double coreTraitMaxDrift,
        ILogger<DefaultPersonalityEngine> logger)
    {
        _logger = logger;
        _evolutionRate = evolutionRate;
        _coreTraitMaxDrift = coreTraitMaxDrift;
        _personality = CreateSeedPersonality(seed);
    }

    public Task<Personality> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_personality);

    public Task EvolveAsync(PersonalityDelta delta, CancellationToken cancellationToken = default)
    {
        // Apply adaptive trait deltas (clamped to [0,1], rate-limited)
        foreach (var (trait, value) in delta.AdaptiveTraitDeltas)
        {
            var current = _personality.AdaptiveTraits[trait];
            var newValue = Clamp(current + value * _evolutionRate);
            SetAdaptiveTrait(trait, newValue);
            _logger.LogDebug("Adaptive trait {Trait} evolved: {Old:F3} → {New:F3}", trait, current, newValue);
        }

        // Apply preference deltas
        foreach (var (key, value) in delta.PreferenceDeltas)
        {
            if (!_personality.Preferences.ContainsKey(key))
                _personality.Preferences[key] = 0.5;
            _personality.Preferences[key] = Clamp(_personality.Preferences[key] + value * _evolutionRate);
        }

        // Apply relationship deltas
        _personality.Relationship.Trust = Clamp(_personality.Relationship.Trust + delta.TrustDelta * _evolutionRate);
        _personality.Relationship.Familiarity = Clamp(_personality.Relationship.Familiarity + delta.FamiliarityDelta * _evolutionRate);
        _personality.Relationship.Affection = Clamp(_personality.Relationship.Affection + delta.AffectionDelta * _evolutionRate);

        _personality.LastEvolvedAt = DateTimeOffset.UtcNow;
        _personality.EvolutionCount++;

        _logger.LogInformation("Personality evolved #{Count}: trust={Trust:F2} fam={Fam:F2} aff={Aff:F2}",
            _personality.EvolutionCount,
            _personality.Relationship.Trust,
            _personality.Relationship.Familiarity,
            _personality.Relationship.Affection);

        return Task.CompletedTask;
    }

    public Task ReflectAsync(CancellationToken cancellationToken = default)
    {
        // Reflection: slightly increase self-awareness traits
        _personality.AdaptiveTraits.Confidence = Clamp(_personality.AdaptiveTraits.Confidence + 0.01);
        _logger.LogDebug("Personality reflection: confidence={Conf:F3}", _personality.AdaptiveTraits.Confidence);
        return Task.CompletedTask;
    }

    public Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        // Evaluate: check core traits haven't drifted too far from seed
        // (In a full implementation, this would compare against the seed and reset if needed)
        _logger.LogDebug("Personality evaluation: evolution count={Count}", _personality.EvolutionCount);
        return Task.CompletedTask;
    }

    public Task<string> RenderSystemPromptAsync(CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Your Identity");
        sb.AppendLine($"Name: {_personality.Identity.Name}");
        sb.AppendLine($"Description: {_personality.Identity.Description}");
        sb.AppendLine();

        sb.AppendLine("# Your Personality (Big Five + Adaptive)");
        sb.AppendLine($"Openness: {_personality.CoreTraits.Openness:F2}");
        sb.AppendLine($"Conscientiousness: {_personality.CoreTraits.Conscientiousness:F2}");
        sb.AppendLine($"Extraversion: {_personality.CoreTraits.Extraversion:F2}");
        sb.AppendLine($"Agreeableness: {_personality.CoreTraits.Agreeableness:F2}");
        sb.AppendLine($"Neuroticism: {_personality.CoreTraits.Neuroticism:F2}");
        sb.AppendLine($"Confidence: {_personality.AdaptiveTraits.Confidence:F2}");
        sb.AppendLine($"Curiosity: {_personality.AdaptiveTraits.Curiosity:F2}");
        sb.AppendLine($"Warmth: {_personality.AdaptiveTraits.Warmth:F2}");
        sb.AppendLine($"Humor: {_personality.Humor}");
        sb.AppendLine();

        sb.AppendLine("# Communication Style");
        sb.AppendLine($"Verbosity: {_personality.CommunicationStyle.Verbosity:F2}");
        sb.AppendLine($"Formality: {_personality.CommunicationStyle.Formality:F2}");
        sb.AppendLine($"Emoji usage: {_personality.CommunicationStyle.EmojiUsage:F2}");
        sb.AppendLine();

        sb.AppendLine("# Relationship with User");
        sb.AppendLine($"Trust: {_personality.Relationship.Trust:F2}");
        sb.AppendLine($"Familiarity: {_personality.Relationship.Familiarity:F2}");
        sb.AppendLine($"Affection: {_personality.Relationship.Affection:F2}");

        // Add preferences if any
        if (_personality.Preferences.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("# Known Preferences");
            foreach (var (key, value) in _personality.Preferences)
                sb.AppendLine($"- {key}: {value:F2}");
        }

        return Task.FromResult(sb.ToString());
    }

    private static Personality CreateSeedPersonality(PersonalitySeed seed)
    {
        return new Personality
        {
            Identity = new Identity { Name = seed.Name, Description = seed.Description },
            CoreTraits = new TraitSet
            {
                Openness = 0.7,
                Conscientiousness = 0.6,
                Extraversion = 0.5,
                Agreeableness = 0.8,
                Neuroticism = 0.3,
            },
            AdaptiveTraits = new TraitSet
            {
                Confidence = 0.5,
                Curiosity = 0.7,
                Creativity = 0.6,
                Playfulness = 0.6,
                Warmth = 0.7,
                Patience = 0.7,
            },
            Humor = HumorStyle.Playful,
            CommunicationStyle = new CommunicationStyle
            {
                Verbosity = 0.4,
                Formality = 0.3,
                EmojiUsage = 0.3,
                QuestionRate = 0.3,
                InitiativeRate = 0.4,
            },
            Relationship = new RelationshipState
            {
                Trust = 0.3,
                Familiarity = 0.1,
                Affection = 0.2,
            },
        };
    }

    private void SetAdaptiveTrait(string trait, double value)
    {
        switch (trait.ToLowerInvariant())
        {
            case "confidence": _personality.AdaptiveTraits.Confidence = value; break;
            case "curiosity": _personality.AdaptiveTraits.Curiosity = value; break;
            case "creativity": _personality.AdaptiveTraits.Creativity = value; break;
            case "playfulness": _personality.AdaptiveTraits.Playfulness = value; break;
            case "assertiveness": _personality.AdaptiveTraits.Assertiveness = value; break;
            case "patience": _personality.AdaptiveTraits.Patience = value; break;
            case "warmth": _personality.AdaptiveTraits.Warmth = value; break;
            default: _logger.LogDebug("Unknown adaptive trait: {Trait}", trait); break;
        }
    }

    private static double Clamp(double v) => Math.Max(0, Math.Min(1, v));
}