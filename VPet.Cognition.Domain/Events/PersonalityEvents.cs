namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Personality-related domain events.
/// </summary>
public sealed record PersonalityEvolvedEvent(string Reason, int TraitCount) : CognitionEvent;
public sealed record PersonalityReflectedEvent(string ReflectionSummary) : CognitionEvent;