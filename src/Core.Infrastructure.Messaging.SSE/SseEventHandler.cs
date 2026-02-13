using Core.Application.Events;
using Core.Domain.Events;

namespace Core.Infrastructure.Messaging.SSE;

public class SseEventHandler : IEventHandler<Event>
{
    private readonly SseClientManager _clientManager;

    public SseEventHandler(SseClientManager clientManager)
    {
        _clientManager = clientManager;
    }

    public Task HandleAsync(Event @event, CancellationToken cancellationToken)
    {
        _clientManager.Broadcast(@event);
        return Task.CompletedTask;
    }
}
