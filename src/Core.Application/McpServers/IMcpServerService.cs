using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;

using McpServerEvent = Core.Domain.McpServers.McpServerEvent;

namespace Core.Application.McpServers;

/// <summary>
/// Service for managing MCP server configurations.
/// </summary>
public interface IMcpServerService
{
    /// <summary>
    /// Gets a list of all configured MCP servers.
    /// </summary>
    /// <returns>A result containing the list of MCP server summary information.</returns>
    Result<IReadOnlyList<McpServerInfo>, Error> GetAll();

    /// <summary>
    /// Gets the full configuration for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <returns>A result containing Maybe with the server definition if found.</returns>
    Result<Maybe<McpServerDefinition>, Error> GetById(McpServerName id);

    /// <summary>
    /// Creates a new MCP server configuration.
    /// </summary>
    /// <param name="definition">The server definition to create.</param>
    /// <returns>A result containing the created server definition, or an error if creation failed.</returns>
    Result<McpServerDefinition, Error> Create(McpServerDefinition definition);

    /// <summary>
    /// Updates an existing MCP server configuration.
    /// </summary>
    /// <param name="definition">The server definition with updated values.</param>
    /// <returns>A result containing the updated server definition, or an error if update failed.</returns>
    Result<McpServerDefinition, Error> Update(McpServerDefinition definition);

    /// <summary>
    /// Deletes an MCP server and all its data.
    /// </summary>
    /// <param name="id">The identifier of the MCP server to delete.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result<Unit, Error> Delete(McpServerName id);

    /// <summary>
    /// Deletes only the configuration of an MCP server, leaving the server entry.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <returns>A result containing the updated server definition without configuration.</returns>
    Result<McpServerDefinition, Error> DeleteConfiguration(McpServerName id);

    /// <summary>
    /// Gets the events for a specific MCP server.
    /// </summary>
    /// <param name="name">The name of the MCP server.</param>
    /// <returns>A result containing the list of events.</returns>
    Result<IReadOnlyList<McpServerEvent>, Error> GetEvents(McpServerName name);

    /// <summary>
    /// Gets the events for a specific MCP server with paging, filtering, and sorting.
    /// </summary>
    /// <param name="name">The name of the MCP server.</param>
    /// <param name="paging">The paging parameters.</param>
    /// <param name="dateFilter">Optional date range filter.</param>
    /// <param name="sortDirection">The sort direction (default: Descending).</param>
    /// <returns>A result containing the paged list of events.</returns>
    Result<PagedResult<McpServerEvent>, Error> GetEvents(
        McpServerName name,
        PagingParameters paging,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending);

    /// <summary>
    /// Gets global events with paging, filtering, and sorting.
    /// </summary>
    /// <param name="paging">The paging parameters.</param>
    /// <param name="serverNameFilter">Optional filter by server name.</param>
    /// <param name="dateFilter">Optional date range filter.</param>
    /// <param name="sortDirection">The sort direction (default: Descending).</param>
    /// <returns>A result containing the paged list of global events.</returns>
    Result<PagedResult<GlobalEvent>, Error> GetGlobalEvents(
        PagingParameters paging,
        McpServerName? serverNameFilter = null,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending);
}
