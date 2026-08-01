using VPet.Cognition.Domain.Events;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// A typed in-process event bus for cognitive domain events.
/// Handlers are awaited sequentially per event type; one handler throwing does not block others.
/// </summary>
public interface IEventBus
{
    void Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : CognitionEvent;
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : CognitionEvent;
}