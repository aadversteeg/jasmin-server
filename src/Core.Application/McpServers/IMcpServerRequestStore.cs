using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Paging;

namespace Core.Application.McpServers;

/// <summary>
/// Store for MCP server async requests.
/// </summary>
public interface IMcpServerRequestStore
{
    /// <summary>
    /// Adds a new request to the store.
    /// </summary>
    /// <param name="request">The request to add.</param>
    void Add(McpServerRequest request);

    /// <summary>
    /// Gets a request by its ID.
    /// </summary>
    /// <param name="id">The request ID.</param>
    /// <returns>The request if found, or None.</returns>
    Maybe<McpServerRequest> GetById(McpServerRequestId id);

    /// <summary>
    /// Gets all requests for a specific server.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <returns>The list of requests for the server, ordered by creation time descending.</returns>
    IReadOnlyList<McpServerRequest> GetByServerName(McpServerName serverName);

    /// <summary>
    /// Gets requests for a specific server with paging, filtering, and sorting.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <param name="paging">The paging parameters.</param>
    /// <param name="dateFilter">Optional date range filter.</param>
    /// <param name="orderBy">The field to order by (createdAt or completedAt).</param>
    /// <param name="sortDirection">The sort direction (default: Descending).</param>
    /// <returns>A paged result of requests.</returns>
    PagedResult<McpServerRequest> GetByServerName(
        McpServerName serverName,
        PagingParameters paging,
        DateRangeFilter? dateFilter = null,
        string orderBy = "createdAt",
        SortDirection sortDirection = SortDirection.Descending);

    /// <summary>
    /// Updates an existing request in the store.
    /// </summary>
    /// <param name="request">The request to update.</param>
    void Update(McpServerRequest request);
}
