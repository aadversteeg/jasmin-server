using Core.Domain.McpServers;
using Core.Domain.Paging;

namespace Core.Application.McpServers;

/// <summary>
/// Store for global events in the system.
/// </summary>
public interface IGlobalEventStore
{
    /// <summary>
    /// Records a global event.
    /// </summary>
    /// <param name="eventType">The type of event.</param>
    /// <param name="serverName">The server name associated with the event.</param>
    void RecordEvent(GlobalEventType eventType, McpServerName serverName);

    /// <summary>
    /// Gets global events with paging, filtering, and sorting.
    /// </summary>
    /// <param name="paging">The paging parameters.</param>
    /// <param name="serverNameFilter">Optional filter by server name.</param>
    /// <param name="dateFilter">Optional date range filter.</param>
    /// <param name="sortDirection">The sort direction (default: Descending).</param>
    /// <returns>A paged result of global events.</returns>
    PagedResult<GlobalEvent> GetEvents(
        PagingParameters paging,
        McpServerName? serverNameFilter = null,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending);
}
