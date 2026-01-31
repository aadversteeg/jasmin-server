using Core.Application.Events;
using Core.Domain.McpServers;

namespace Core.Infrastructure.Messaging.InMemory;

/// <summary>
/// Event handler that stores MCP server events in the event store.
/// </summary>
public class EventStoreHandler : IEventHandler<McpServerEvent>
{
    private readonly EventStore _eventStore;

    public EventStoreHandler(EventStore eventStore)
    {
        _eventStore = eventStore;
    }

    /// <inheritdoc />
    public Task HandleAsync(McpServerEvent @event, CancellationToken cancellationToken)
    {
        _eventStore.Store(@event);
        return Task.CompletedTask;
    }
}
