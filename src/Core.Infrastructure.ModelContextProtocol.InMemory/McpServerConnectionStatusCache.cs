using System.Collections.Concurrent;
using Core.Application.McpServers;
using Core.Domain.McpServers;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Thread-safe in-memory cache for MCP server connection status.
/// </summary>
public class McpServerConnectionStatusCache : IMcpServerConnectionStatusCache
{
    private readonly ConcurrentDictionary<string, McpServerConnectionStatus> _cache = new();

    /// <inheritdoc />
    public McpServerConnectionStatus GetStatus(McpServerId id)
    {
        return _cache.TryGetValue(id.Value, out var status)
            ? status
            : McpServerConnectionStatus.Unknown;
    }

    /// <inheritdoc />
    public void SetStatus(McpServerId id, McpServerConnectionStatus status)
    {
        _cache[id.Value] = status;
    }

    /// <inheritdoc />
    public void RemoveStatus(McpServerId id)
    {
        _cache.TryRemove(id.Value, out _);
    }
}
