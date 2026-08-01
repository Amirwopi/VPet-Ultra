using System.Collections.Concurrent;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Events;

namespace VPet.Cognition.Host.EventBus;

/// <summary>
/// Typed in-process event bus. Handlers are awaited sequentially per event type;
/// a handler throwing is logged and does not block other handlers.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<CognitionEvent, CancellationToken, Task>>> _handlers = new();
    private readonly Microsoft.Extensions.Logging.ILogger<EventBus> _logger;

    public EventBus(Microsoft.Extensions.Logging.ILogger<EventBus> logger) => _logger = logger;

    public void Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : CognitionEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            foreach (var handler in list.ToArray())
            {
                _ = SafeInvoke(handler, @event, cancellationToken);
            }
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : CognitionEvent
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new());
        var wrapped = (Func<CognitionEvent, CancellationToken, Task>)((e, ct) => handler((TEvent)e, ct));
        lock (list)
            list.Add(wrapped);

        return new Unsubscriber(() =>
        {
            lock (list)
                list.Remove(wrapped);
        });
    }

    private async Task SafeInvoke(Func<CognitionEvent, CancellationToken, Task> handler, CognitionEvent @event, CancellationToken ct)
    {
        try
        {
            await handler(@event, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event bus handler threw for {EventType} (CorrelationId={CorrelationId})",
                @event.GetType().Name, @event.CorrelationId);
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly Action _unsubscribe;
        public Unsubscriber(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}