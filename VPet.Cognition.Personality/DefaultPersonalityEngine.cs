using System.Text;
using System.Text.Json;
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
/// Default <see cref="IPersonalityEngine"/> implementation. Maintains a personality
/// state that evolves through conversation deltas, performs LLM-based self-reflection,
/// and clamps core traits to prevent excessive drift from the seed.
/// </summary>
public sealed class DefaultPersonalityEngine : IPersonalityEngine
{
    private readonly ILogger<DefaultPersonalityEngine> _logger;
    private Personality _personality;
    private readonly Personality _seedSnapshot;
    private readonly double _evolutionRate;
    private readonly double _coreTraitMaxDrift;

    /// <summary>Optional LLM provider for reflection. If null, reflection uses heuristics.</summary>
    public ILLMProvider? LlmProvider { get; set; }

    /// <summary>Optional event bus for publishing personality events.</summary>
    public IEventBus? EventBus { get; set; }

    /// <summary>Optional persistence callback. Called with JSON whenever personality changes.</summary>
    public Func<string, Task>? PersistAsync { get; set; }

    /// <summary>Optional persistence loader. Called once to load state.</summary>
    public Func<Task<string?>>? LoadAsync { get; set; }

    private bool _loaded;

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
        _seedSnapshot = ClonePersonality(_personality);
    }

    public async Task<Personality> GetAsync(CancellationToken cancellationToken = default)
    {
        await TryLoadAsync(cancellationToken);
        return _personality;
    }

    public async Task EvolveAsync(PersonalityDelta delta, CancellationToken cancellationToken = default)
    {
        await TryLoadAsync(cancellationToken);

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

        if (PersistAsync is not null)
            await PersistAsync(SerializePersonality());
    }

    public async Task ReflectAsync(CancellationToken cancellationToken = default)
    {
        await TryLoadAsync(cancellationToken);

        if (LlmProvider is not null)
        {
            try
            {
                // Ask the LLM to reflect on the conversation so far
                var prompt = BuildReflectionPrompt();
                var request = new ChatRequest
                {
                    SystemPrompt = "You are an introspection module. Analyze the companion's personality and suggest small adjustments. Respond in JSON only.",
                    Messages = new[]
                    {
                        new ChatMessageDto("user", prompt),
                    },
                    MaxTokens = 256,
                    Temperature = 0.3,
                };

                var result = await LlmProvider.ChatAsync(request, cancellationToken);
                if (result.IsSuccess && result.Value is not null)
                {
                    ApplyReflectionSuggestions(result.Value.Content);
                    _logger.LogInformation("Personality reflection completed via LLM.");
                }
                else
                {
                    HeuristicReflection();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "LLM reflection failed; using heuristic.");
                HeuristicReflection();
            }
        }
        else
        {
            HeuristicReflection();
        }

        if (PersistAsync is not null)
            await PersistAsync(SerializePersonality());
    }

    public async Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        await TryLoadAsync(cancellationToken);

        // Check core traits haven't drifted too far from seed
        var drift = new[]
        {
            ("Openness", _personality.CoreTraits.Openness, _seedSnapshot.CoreTraits.Openness),
            ("Conscientiousness", _personality.CoreTraits.Conscientiousness, _seedSnapshot.CoreTraits.Conscientiousness),
            ("Extraversion", _personality.CoreTraits.Extraversion, _seedSnapshot.CoreTraits.Extraversion),
            ("Agreeableness", _personality.CoreTraits.Agreeableness, _seedSnapshot.CoreTraits.Agreeableness),
            ("Neuroticism", _personality.CoreTraits.Neuroticism, _seedSnapshot.CoreTraits.Neuroticism),
        };

        bool reset = false;
        foreach (var (name, current, seed) in drift)
        {
            var diff = Math.Abs(current - seed);
            if (diff > _coreTraitMaxDrift)
            {
                _logger.LogWarning("Core trait {Trait} drifted {Diff:F3} > {Max:F3}; resetting toward seed.", name, diff, _coreTraitMaxDrift);
                ResetCoreTraitTowardSeed(name);
                reset = true;
            }
        }

        if (reset)
        {
            _logger.LogInformation("Personality evaluation: core traits reset toward seed.");
        }
        else
        {
            _logger.LogDebug("Personality evaluation: all core traits within drift limit.");
        }

        if (PersistAsync is not null)
            await PersistAsync(SerializePersonality());
    }

    public async Task<string> RenderSystemPromptAsync(CancellationToken cancellationToken = default)
    {
        await TryLoadAsync(cancellationToken);

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

        if (_personality.Preferences.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("# Known Preferences");
            foreach (var (key, value) in _personality.Preferences)
                sb.AppendLine($"- {key}: {value:F2}");
        }

        if (_personality.Interests.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("# Interests");
            foreach (var interest in _personality.Interests)
                sb.AppendLine($"- {interest.Topic} (level: {interest.Level:F2})");
        }

        return sb.ToString();
    }

    // ── Private helpers ──

    private string BuildReflectionPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Current personality state:");
        sb.AppendLine($"- Confidence: {_personality.AdaptiveTraits.Confidence:F2}");
        sb.AppendLine($"- Curiosity: {_personality.AdaptiveTraits.Curiosity:F2}");
        sb.AppendLine($"- Warmth: {_personality.AdaptiveTraits.Warmth:F2}");
        sb.AppendLine($"- Trust: {_personality.Relationship.Trust:F2}");
        sb.AppendLine($"- Familiarity: {_personality.Relationship.Familiarity:F2}");
        sb.AppendLine($"- Affection: {_personality.Relationship.Affection:F2}");
        sb.AppendLine($"- Evolution count: {_personality.EvolutionCount}");
        sb.AppendLine();
        sb.AppendLine("Suggest small adjustments (max ±0.05 per trait) based on recent interactions.");
        sb.AppendLine("Respond as JSON: {\"confidence\": 0.0, \"curiosity\": 0.0, \"warmth\": 0.0}");
        return sb.ToString();
    }

    private void ApplyReflectionSuggestions(string llmResponse)
    {
        try
        {
            // Extract JSON from the response
            var jsonStart = llmResponse.IndexOf('{');
            var jsonEnd = llmResponse.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0) return;

            var json = llmResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(json);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var trait = prop.Name;
                if (prop.Value.TryGetDouble(out var delta))
                {
                    var current = _personality.AdaptiveTraits[trait];
                    var newValue = Clamp(current + Math.Clamp(delta, -0.05, 0.05));
                    SetAdaptiveTrait(trait, newValue);
                    _logger.LogDebug("Reflection adjusted {Trait}: {Old:F3} → {New:F3}", trait, current, newValue);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse LLM reflection suggestions.");
            HeuristicReflection();
        }
    }

    private void HeuristicReflection()
    {
        // Slightly increase self-awareness traits based on evolution count
        var boost = Math.Min(0.02, _personality.EvolutionCount * 0.001);
        _personality.AdaptiveTraits.Confidence = Clamp(_personality.AdaptiveTraits.Confidence + boost);
        _personality.AdaptiveTraits.Curiosity = Clamp(_personality.AdaptiveTraits.Curiosity + boost * 0.5);
        _logger.LogDebug("Heuristic reflection: confidence+{Boost:F4}, curiosity+{Boost2:F4}", boost, boost * 0.5);
    }

    private void ResetCoreTraitTowardSeed(string traitName)
    {
        switch (traitName.ToLowerInvariant())
        {
            case "openness":
                _personality.CoreTraits.Openness = MoveToward(_personality.CoreTraits.Openness, _seedSnapshot.CoreTraits.Openness, 0.05);
                break;
            case "conscientiousness":
                _personality.CoreTraits.Conscientiousness = MoveToward(_personality.CoreTraits.Conscientiousness, _seedSnapshot.CoreTraits.Conscientiousness, 0.05);
                break;
            case "extraversion":
                _personality.CoreTraits.Extraversion = MoveToward(_personality.CoreTraits.Extraversion, _seedSnapshot.CoreTraits.Extraversion, 0.05);
                break;
            case "agreeableness":
                _personality.CoreTraits.Agreeableness = MoveToward(_personality.CoreTraits.Agreeableness, _seedSnapshot.CoreTraits.Agreeableness, 0.05);
                break;
            case "neuroticism":
                _personality.CoreTraits.Neuroticism = MoveToward(_personality.CoreTraits.Neuroticism, _seedSnapshot.CoreTraits.Neuroticism, 0.05);
                break;
        }
    }

    private static double MoveToward(double current, double target, double step)
    {
        if (current > target) return Math.Max(target, current - step);
        if (current < target) return Math.Min(target, current + step);
        return current;
    }

    private async Task TryLoadAsync(CancellationToken ct)
    {
        if (_loaded || LoadAsync is null) return;
        _loaded = true;
        try
        {
            var json = await LoadAsync();
            if (!string.IsNullOrEmpty(json))
            {
                var loaded = JsonSerializer.Deserialize<Personality>(json);
                if (loaded is not null)
                {
                    _personality = loaded;
                    _logger.LogInformation("Personality loaded from persistence: evolution={Count}", loaded.EvolutionCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load personality from persistence; using seed.");
        }
    }

    private string SerializePersonality()
    {
        return JsonSerializer.Serialize(_personality, new JsonSerializerOptions { WriteIndented = false });
    }

    private static Personality ClonePersonality(Personality p)
    {
        return new Personality
        {
            Identity = new Identity { Name = p.Identity.Name, Description = p.Identity.Description },
            CoreTraits = new TraitSet
            {
                Openness = p.CoreTraits.Openness,
                Conscientiousness = p.CoreTraits.Conscientiousness,
                Extraversion = p.CoreTraits.Extraversion,
                Agreeableness = p.CoreTraits.Agreeableness,
                Neuroticism = p.CoreTraits.Neuroticism,
            },
            AdaptiveTraits = new TraitSet
            {
                Confidence = p.AdaptiveTraits.Confidence,
                Curiosity = p.AdaptiveTraits.Curiosity,
                Creativity = p.AdaptiveTraits.Creativity,
                Playfulness = p.AdaptiveTraits.Playfulness,
                Assertiveness = p.AdaptiveTraits.Assertiveness,
                Patience = p.AdaptiveTraits.Patience,
                Warmth = p.AdaptiveTraits.Warmth,
            },
            Humor = p.Humor,
            CommunicationStyle = new CommunicationStyle
            {
                Verbosity = p.CommunicationStyle.Verbosity,
                Formality = p.CommunicationStyle.Formality,
                EmojiUsage = p.CommunicationStyle.EmojiUsage,
                QuestionRate = p.CommunicationStyle.QuestionRate,
                InitiativeRate = p.CommunicationStyle.InitiativeRate,
            },
            Relationship = new RelationshipState
            {
                Trust = p.Relationship.Trust,
                Familiarity = p.Relationship.Familiarity,
                Affection = p.Relationship.Affection,
                SharedJokes = p.Relationship.SharedJokes,
                DeepConversations = p.Relationship.DeepConversations,
                RelationshipStart = p.Relationship.RelationshipStart,
            },
        };
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