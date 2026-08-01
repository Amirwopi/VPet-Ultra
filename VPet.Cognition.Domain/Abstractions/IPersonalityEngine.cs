using VPet.Cognition.Domain.Entities;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// Abstraction over the personality engine.
/// </summary>
public interface IPersonalityEngine
{
    Task<Personality> GetAsync(CancellationToken cancellationToken = default);
    Task EvolveAsync(PersonalityDelta delta, CancellationToken cancellationToken = default);
    Task ReflectAsync(CancellationToken cancellationToken = default);
    Task EvaluateAsync(CancellationToken cancellationToken = default);
    Task<string> RenderSystemPromptAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A delta applied to the personality state during evolution.
/// </summary>
public sealed record PersonalityDelta
{
    public Dictionary<string, double> AdaptiveTraitDeltas { get; init; } = new();
    public Dictionary<string, double> PreferenceDeltas { get; init; } = new();
    public double TrustDelta { get; init; }
    public double FamiliarityDelta { get; init; }
    public double AffectionDelta { get; init; }
    public string? Reason { get; init; }
}