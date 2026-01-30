using System.Collections.Concurrent;
using Core.Application.McpServers;
using Core.Domain.McpServers;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Thread-safe in-memory cache for MCP server connection status.
/// Maintains a mapping from server names to internal IDs.
/// </summary>
public class McpServerConnectionStatusCache : IMcpServerConnectionStatusCache
{
    private readonly ConcurrentDictionary<string, McpServerId> _nameToIdMap = new();
    private readonly ConcurrentDictionary<string, McpServerStatusCacheEntry> _statusCache = new();

    /// <inheritdoc />
    public McpServerId GetOrCreateId(McpServerName name)
    {
        return _nameToIdMap.GetOrAdd(name.Value, _ => McpServerId.Create());
    }

    /// <inheritdoc />
    public McpServerStatusCacheEntry GetEntry(McpServerId id)
    {
        return _statusCache.TryGetValue(id.Value, out var entry)
            ? entry
            : new McpServerStatusCacheEntry(McpServerConnectionStatus.Unknown, null);
    }

    /// <inheritdoc />
    public void SetStatus(McpServerId id, McpServerConnectionStatus status)
    {
        _statusCache[id.Value] = new McpServerStatusCacheEntry(status, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public void RemoveByName(McpServerName name)
    {
        if (_nameToIdMap.TryRemove(name.Value, out var id))
        {
            _statusCache.TryRemove(id.Value, out _);
        }
    }
}
