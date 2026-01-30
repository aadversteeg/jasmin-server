using Ave.Extensions.Functional;
using Core.Domain.McpServers;

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
    /// Updates an existing request in the store.
    /// </summary>
    /// <param name="request">The request to update.</param>
    void Update(McpServerRequest request);
}
