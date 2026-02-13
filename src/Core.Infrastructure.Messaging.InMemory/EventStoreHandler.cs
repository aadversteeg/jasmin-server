using Core.Application.Events;
using Core.Domain.Events;

namespace Core.Infrastructure.Messaging.InMemory;

/// <summary>
/// Event handler that stores events in the event store.
/// </summary>
public class EventStoreHandler : IEventHandler<Event>
{
    private readonly EventStore _eventStore;

    public EventStoreHandler(EventStore eventStore)
    {
        _eventStore = eventStore;
    }

    /// <inheritdoc />
    public Task HandleAsync(Event @event, CancellationToken cancellationToken)
    {
        _eventStore.Store(@event);
        return Task.CompletedTask;
    }
}
