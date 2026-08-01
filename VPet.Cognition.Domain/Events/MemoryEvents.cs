using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Memory-related domain events.
/// </summary>
public sealed record MemoryStoredEvent(MemoryId MemoryId, Enums.MemoryType Type) : CognitionEvent;
public sealed record MemoryRetrievedEvent(MemoryId MemoryId, double Relevance) : CognitionEvent;
public sealed record MemoryConsolidatedEvent(MemoryId SourceId, MemoryId? TargetId, string Action) : CognitionEvent;
public sealed record MemoryForgottenEvent(MemoryId MemoryId) : CognitionEvent;
public sealed record MemoryCompressedEvent(MemoryId SummaryId, int OriginalCount) : CognitionEvent;