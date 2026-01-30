using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Paging;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Thread-safe in-memory store for global events.
/// </summary>
public class GlobalEventStore : IGlobalEventStore
{
    private readonly List<GlobalEvent> _events = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void RecordEvent(GlobalEventType eventType, McpServerName serverName)
    {
        var evt = new GlobalEvent(eventType, serverName, DateTime.UtcNow);
        lock (_lock)
        {
            _events.Add(evt);
        }
    }

    /// <inheritdoc />
    public PagedResult<GlobalEvent> GetEvents(
        PagingParameters paging,
        McpServerName? serverNameFilter = null,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending)
    {
        lock (_lock)
        {
            var filtered = _events.AsEnumerable();

            if (serverNameFilter != null)
            {
                filtered = filtered.Where(e => e.ServerName == serverNameFilter);
            }

            if (dateFilter != null)
            {
                filtered = filtered.Where(e => dateFilter.IsInRange(e.TimestampUtc));
            }

            var totalItems = filtered.Count();

            filtered = sortDirection == SortDirection.Ascending
                ? filtered.OrderBy(e => e.TimestampUtc)
                : filtered.OrderByDescending(e => e.TimestampUtc);

            var items = filtered.Skip(paging.Skip).Take(paging.PageSize).ToList();

            return new PagedResult<GlobalEvent>(items, paging.Page, paging.PageSize, totalItems);
        }
    }
}
