using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="IPersonalityEngine"/> with a mutable state for assertions.
/// </summary>
public sealed class FakePersonalityEngine : IPersonalityEngine
{
    public Personality State { get; set; } = new();
    public string LastSystemPrompt { get; private set; } = string.Empty;
    public int EvolveCount { get; private set; }
    public int ReflectCount { get; private set; }
    public int EvaluateCount { get; private set; }

    public Task<Personality> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(State);

    public Task EvolveAsync(PersonalityDelta delta, CancellationToken cancellationToken = default)
    {
        EvolveCount++;
        foreach (var (trait, value) in delta.AdaptiveTraitDeltas)
        {
            // simple apply for testing
        }
        State.Relationship.Trust = Math.Min(1, State.Relationship.Trust + delta.TrustDelta);
        State.Relationship.Affection = Math.Min(1, State.Relationship.Affection + delta.AffectionDelta);
        State.EvolutionCount++;
        return Task.CompletedTask;
    }

    public Task ReflectAsync(CancellationToken cancellationToken = default)
    {
        ReflectCount++;
        return Task.CompletedTask;
    }

    public Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        EvaluateCount++;
        return Task.CompletedTask;
    }

    public Task<string> RenderSystemPromptAsync(CancellationToken cancellationToken = default)
    {
        LastSystemPrompt = $"You are {State.Identity.Name}. {State.Identity.Description}";
        return Task.FromResult(LastSystemPrompt);
    }
}