using Core.Domain.McpServers;
using Core.Domain.Paging;

namespace Core.Application.McpServers;

/// <summary>
/// Store for querying MCP server events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Gets events with paging, filtering, and sorting.
    /// </summary>
    /// <param name="paging">The paging parameters.</param>
    /// <param name="serverNameFilter">Optional filter by server name.</param>
    /// <param name="instanceIdFilter">Optional filter by instance ID.</param>
    /// <param name="requestIdFilter">Optional filter by request ID.</param>
    /// <param name="dateFilter">Optional date range filter.</param>
    /// <param name="sortDirection">The sort direction (default: Descending).</param>
    /// <returns>A paged result of events.</returns>
    PagedResult<McpServerEvent> GetEvents(
        PagingParameters paging,
        McpServerName? serverNameFilter = null,
        McpServerInstanceId? instanceIdFilter = null,
        McpServerRequestId? requestIdFilter = null,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending);
}
