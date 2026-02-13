using Core.Application.Events;
using Core.Domain.Events;
using Core.Domain.Paging;

namespace Core.Infrastructure.Messaging.InMemory;

/// <summary>
/// In-memory implementation of the global event store.
/// </summary>
public class EventStore : IEventStore
{
    private readonly List<Event> _events = new();
    private readonly object _lock = new();

    /// <summary>
    /// Stores an event in the event store.
    /// </summary>
    /// <param name="event">The event to store.</param>
    public void Store(Event @event)
    {
        lock (_lock)
        {
            _events.Add(@event);
        }
    }

    /// <inheritdoc />
    public PagedResult<Event> GetEvents(
        PagingParameters paging,
        string? targetFilter = null,
        EventType? eventTypeFilter = null,
        string? requestIdFilter = null,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending)
    {
        lock (_lock)
        {
            var filtered = _events.AsEnumerable();

            if (!string.IsNullOrEmpty(targetFilter))
            {
                filtered = filtered.Where(e => MatchesTargetPrefix(e.Target, targetFilter));
            }

            if (eventTypeFilter.HasValue)
            {
                filtered = filtered.Where(e => e.Type == eventTypeFilter.Value);
            }

            if (!string.IsNullOrEmpty(requestIdFilter))
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

            return new PagedResult<Event>(items, paging.Page, paging.PageSize, totalItems);
        }
    }

    /// <inheritdoc />
    public IEnumerable<Event> GetEventsAfter(
        DateTime afterTimestampUtc,
        string? targetFilter = null)
    {
        lock (_lock)
        {
            var filtered = _events
                .Where(e => e.TimestampUtc > afterTimestampUtc)
                .OrderBy(e => e.TimestampUtc);

            if (!string.IsNullOrEmpty(targetFilter))
            {
                return filtered.Where(e => MatchesTargetPrefix(e.Target, targetFilter)).ToList();
            }

            return filtered.ToList();
        }
    }

    private static bool MatchesTargetPrefix(string target, string filter)
    {
        return target.Equals(filter, StringComparison.OrdinalIgnoreCase)
            || target.StartsWith(filter + "/", StringComparison.OrdinalIgnoreCase);
    }
}
