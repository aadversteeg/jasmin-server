using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Paging;

namespace Core.Infrastructure.Messaging.InMemory;

/// <summary>
/// In-memory implementation of the global event store.
/// </summary>
public class EventStore : IEventStore
{
    private readonly List<McpServerEvent> _events = new();
    private readonly object _lock = new();

    /// <summary>
    /// Stores an event in the event store.
    /// </summary>
    /// <param name="event">The event to store.</param>
    public void Store(McpServerEvent @event)
    {
        lock (_lock)
        {
            _events.Add(@event);
        }
    }

    /// <inheritdoc />
    public PagedResult<McpServerEvent> GetEvents(
        PagingParameters paging,
        McpServerName? serverNameFilter = null,
        McpServerInstanceId? instanceIdFilter = null,
        McpServerRequestId? requestIdFilter = null,
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

            if (instanceIdFilter != null)
            {
                filtered = filtered.Where(e => e.InstanceId == instanceIdFilter);
            }

            if (requestIdFilter != null)
            {
                filtered = filtered.Where(e => e.RequestId == requestIdFilter);
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

            return new PagedResult<McpServerEvent>(items, paging.Page, paging.PageSize, totalItems);
        }
    }

    /// <inheritdoc />
    public IEnumerable<McpServerEvent> GetEventsAfter(
        DateTime afterTimestampUtc,
        McpServerName? serverNameFilter = null)
    {
        lock (_lock)
        {
            var filtered = _events
                .Where(e => e.TimestampUtc > afterTimestampUtc)
                .OrderBy(e => e.TimestampUtc);

            if (serverNameFilter != null)
            {
                return filtered.Where(e => e.ServerName == serverNameFilter).ToList();
            }

            return filtered.ToList();
        }
    }
}
