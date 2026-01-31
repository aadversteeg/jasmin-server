using Core.Application.Events;
using Core.Domain.McpServers;

namespace Core.Infrastructure.Messaging.SSE;

public class SseEventHandler : IEventHandler<McpServerEvent>
{
    private readonly SseClientManager _clientManager;

    public SseEventHandler(SseClientManager clientManager)
    {
        _clientManager = clientManager;
    }

    public Task HandleAsync(McpServerEvent @event, CancellationToken cancellationToken)
    {
        _clientManager.Broadcast(@event);
        return Task.CompletedTask;
    }
}
