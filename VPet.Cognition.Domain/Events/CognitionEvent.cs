namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Base type for all cognitive domain events published on the <c>IEventBus</c>.
/// Carries a correlation id so a full cognitive operation can be traced across modules.
/// </summary>
public abstract record CognitionEvent
{
    /// <summary>Unique id grouping all events of one cognitive operation.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();

    /// <summary>When the event was raised.</summary>
    public DateTimeOffset RaisedAt { get; init; } = DateTimeOffset.UtcNow;
}