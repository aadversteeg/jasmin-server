using System.Collections.Concurrent;
using System.Threading.Channels;
using Core.Domain.McpServers;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Messaging.SSE;

public class SseClientManager
{
    private readonly ConcurrentDictionary<Guid, Channel<McpServerEvent>> _clients = new();
    private readonly ILogger<SseClientManager> _logger;

    public SseClientManager(ILogger<SseClientManager> logger)
    {
        _logger = logger;
    }

    public Guid RegisterClient(int channelCapacity = 100)
    {
        var clientId = Guid.NewGuid();
        var channel = Channel.CreateBounded<McpServerEvent>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        _clients[clientId] = channel;
        _logger.LogInformation("SSE client {ClientId} connected. Total clients: {Count}", clientId, _clients.Count);
        return clientId;
    }

    public void UnregisterClient(Guid clientId)
    {
        if (_clients.TryRemove(clientId, out var channel))
        {
            channel.Writer.Complete();
            _logger.LogInformation("SSE client {ClientId} disconnected. Total clients: {Count}", clientId, _clients.Count);
        }
    }

    public IAsyncEnumerable<McpServerEvent> GetEventsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (_clients.TryGetValue(clientId, out var channel))
        {
            return channel.Reader.ReadAllAsync(cancellationToken);
        }
        throw new InvalidOperationException($"Client {clientId} not found");
    }

    public void Broadcast(McpServerEvent @event)
    {
        foreach (var (clientId, channel) in _clients)
        {
            if (!channel.Writer.TryWrite(@event))
            {
                _logger.LogDebug("Event dropped for SSE client {ClientId} - queue full", clientId);
            }
        }
    }

    public int ClientCount => _clients.Count;
}
