using System.Collections.Concurrent;
using Core.Application.McpServers;
using Core.Domain.McpServers;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Thread-safe in-memory cache for MCP server connection status and events.
/// Maintains a mapping from server names to internal IDs.
/// </summary>
public class McpServerConnectionStatusCache : IMcpServerConnectionStatusCache
{
    private readonly ConcurrentDictionary<string, McpServerId> _nameToIdMap = new();
    private readonly ConcurrentDictionary<string, McpServerStatusCacheEntry> _statusCache = new();
    private readonly ConcurrentDictionary<string, List<McpServerEvent>> _eventHistory = new();
    private readonly object _eventLock = new();

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
            _eventHistory.TryRemove(id.Value, out _);
        }
    }

    /// <inheritdoc />
    public void RecordEvent(McpServerId id, McpServerEventType eventType, string? errorMessage = null)
    {
        var evt = new McpServerEvent(eventType, DateTime.UtcNow, errorMessage);
        _eventHistory.AddOrUpdate(
            id.Value,
            _ => new List<McpServerEvent> { evt },
            (_, list) =>
            {
                lock (_eventLock)
                {
                    list.Add(evt);
                }
                return list;
            });
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerEvent> GetEvents(McpServerId id)
    {
        if (_eventHistory.TryGetValue(id.Value, out var events))
        {
            lock (_eventLock)
            {
                return events.OrderBy(e => e.TimestampUtc).ToList().AsReadOnly();
            }
        }
        return Array.Empty<McpServerEvent>();
    }
}
