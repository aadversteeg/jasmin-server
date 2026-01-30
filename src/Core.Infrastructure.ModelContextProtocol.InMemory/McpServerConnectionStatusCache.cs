using System.Collections.Concurrent;
using Core.Application.McpServers;
using Core.Domain.McpServers;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Thread-safe in-memory cache for MCP server connection status.
/// </summary>
public class McpServerConnectionStatusCache : IMcpServerConnectionStatusCache
{
    private readonly ConcurrentDictionary<string, McpServerStatusCacheEntry> _cache = new();

    /// <inheritdoc />
    public McpServerStatusCacheEntry GetEntry(McpServerName id)
    {
        return _cache.TryGetValue(id.Value, out var entry)
            ? entry
            : new McpServerStatusCacheEntry(McpServerConnectionStatus.Unknown, null);
    }

    /// <inheritdoc />
    public void SetStatus(McpServerName id, McpServerConnectionStatus status)
    {
        _cache[id.Value] = new McpServerStatusCacheEntry(status, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public void RemoveStatus(McpServerName id)
    {
        _cache.TryRemove(id.Value, out _);
    }
}
