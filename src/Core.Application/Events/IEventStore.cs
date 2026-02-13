using Core.Domain.Events;
using Core.Domain.Paging;

namespace Core.Application.Events;

/// <summary>
/// Store for querying events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Gets events with paging, filtering, and sorting.
    /// </summary>
    /// <param name="paging">The paging parameters.</param>
    /// <param name="targetFilter">Optional filter by target URI prefix.</param>
    /// <param name="eventTypeFilter">Optional filter by event type.</param>
    /// <param name="requestIdFilter">Optional filter by request ID.</param>
    /// <param name="dateFilter">Optional date range filter.</param>
    /// <param name="sortDirection">The sort direction (default: Descending).</param>
    /// <returns>A paged result of events.</returns>
    PagedResult<Event> GetEvents(
        PagingParameters paging,
        string? targetFilter = null,
        EventType? eventTypeFilter = null,
        string? requestIdFilter = null,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending);

    /// <summary>
    /// Gets all events that occurred after the specified timestamp.
    /// Used for SSE reconnection to replay missed events.
    /// </summary>
    /// <param name="afterTimestampUtc">The timestamp to filter events after (exclusive).</param>
    /// <param name="targetFilter">Optional filter by target URI prefix.</param>
    /// <returns>Events ordered by timestamp ascending.</returns>
    IEnumerable<Event> GetEventsAfter(
        DateTime afterTimestampUtc,
        string? targetFilter = null);
}
