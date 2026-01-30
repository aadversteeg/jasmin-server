using System.Collections.Concurrent;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Paging;

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
    private readonly ConcurrentDictionary<string, McpServerMetadata> _metadataCache = new();
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
        // Only remove the status and metadata, preserve name mapping and events for audit trail.
        // Events remain queryable and will be associated with any recreated server
        // with the same name.
        if (_nameToIdMap.TryGetValue(name.Value, out var id))
        {
            _statusCache.TryRemove(id.Value, out _);
            _metadataCache.TryRemove(id.Value, out _);
        }
    }

    /// <inheritdoc />
    public void RecordEvent(
        McpServerId id,
        McpServerEventType eventType,
        IReadOnlyList<McpServerEventError>? errors = null,
        McpServerInstanceId? instanceId = null,
        McpServerRequestId? requestId = null,
        McpServerEventConfiguration? oldConfiguration = null,
        McpServerEventConfiguration? configuration = null)
    {
        var evt = new McpServerEvent(eventType, DateTime.UtcNow, errors, instanceId, requestId, oldConfiguration, configuration);
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

    /// <inheritdoc />
    public PagedResult<McpServerEvent> GetEvents(
        McpServerId id,
        PagingParameters paging,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending)
    {
        if (!_eventHistory.TryGetValue(id.Value, out var events))
        {
            return new PagedResult<McpServerEvent>([], paging.Page, paging.PageSize, 0);
        }

        lock (_eventLock)
        {
            var filtered = events.AsEnumerable();

            if (dateFilter != null)
            {
                filtered = filtered.Where(e => dateFilter.IsInRange(e.TimestampUtc));
            }

            var totalItems = filtered.Count();

            filtered = sortDirection == SortDirection.Ascending
                ? filtered.OrderBy(e => e.TimestampUtc)
                : filtered.OrderByDescending(e => e.TimestampUtc);

            var items = filtered.Skip(paging.Skip).Take(paging.PageSize).ToList();

            return new PagedResult<McpServerEvent>(items, paging.Page, paging.PageSize, totalItems);
        }
    }

    /// <inheritdoc />
    public void SetMetadata(McpServerId id, McpServerMetadata metadata)
    {
        _metadataCache[id.Value] = metadata;
    }

    /// <inheritdoc />
    public McpServerMetadata? GetMetadata(McpServerId id)
    {
        return _metadataCache.TryGetValue(id.Value, out var metadata) ? metadata : null;
    }
}
