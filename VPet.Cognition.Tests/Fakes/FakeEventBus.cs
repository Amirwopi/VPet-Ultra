using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Events;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="IEventBus"/> that records published events for assertions.
/// </summary>
public sealed class FakeEventBus : IEventBus
{
    public List<CognitionEvent> PublishedEvents { get; } = new();
    private readonly List<(Type, Func<CognitionEvent, CancellationToken, Task>)> _handlers = new();

    public void Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : CognitionEvent
    {
        PublishedEvents.Add(@event);
        foreach (var (type, handler) in _handlers)
        {
            if (type.IsAssignableFrom(typeof(TEvent)))
                handler(@event, cancellationToken);
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : CognitionEvent
    {
        var entry = (typeof(TEvent), (Func<CognitionEvent, CancellationToken, Task>)((e, ct) => handler((TEvent)e, ct)));
        _handlers.Add(entry);
        return new Unsubscriber(() => _handlers.Remove(entry));
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly Action _unsubscribe;
        public Unsubscriber(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}