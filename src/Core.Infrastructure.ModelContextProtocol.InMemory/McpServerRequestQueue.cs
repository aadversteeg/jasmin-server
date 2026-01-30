using System.Threading.Channels;
using Core.Application.McpServers;
using Core.Domain.McpServers;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Channel-based queue for MCP server requests.
/// Requests are processed sequentially, one at a time.
/// </summary>
public class McpServerRequestQueue : IMcpServerRequestQueue
{
    private readonly Channel<McpServerRequest> _channel =
        Channel.CreateUnbounded<McpServerRequest>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    /// <inheritdoc />
    public void Enqueue(McpServerRequest request)
    {
        _channel.Writer.TryWrite(request);
    }

    /// <inheritdoc />
    public ValueTask<McpServerRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
