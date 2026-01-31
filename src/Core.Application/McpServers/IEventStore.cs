using Core.Domain.McpServers;
using Core.Domain.Paging;

namespace Core.Application.McpServers;

/// <summary>
/// Global store for all MCP server events.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Records an event for the specified server.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="eventType">The type of event.</param>
    /// <param name="errors">Optional list of errors for failure events.</param>
    /// <param name="instanceId">Optional instance identifier for instance-specific events.</param>
    /// <param name="requestId">Optional request identifier for request-initiated events.</param>
    /// <param name="oldConfiguration">Previous configuration for update/delete events.</param>
    /// <param name="configuration">Configuration for create/update/start events.</param>
    /// <param name="toolInvocationData">Data for tool invocation events.</param>
    void RecordEvent(
        McpServerName serverName,
        McpServerEventType eventType,
        IReadOnlyList<McpServerEventError>? errors = null,
        McpServerInstanceId? instanceId = null,
        McpServerRequestId? requestId = null,
        McpServerEventConfiguration? oldConfiguration = null,
        McpServerEventConfiguration? configuration = null,
        McpServerToolInvocationEventData? toolInvocationData = null);

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
