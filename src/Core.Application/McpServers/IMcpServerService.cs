using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

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
    /// Deletes an MCP server configuration.
    /// </summary>
    /// <param name="id">The identifier of the MCP server to delete.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result<Unit, Error> Delete(McpServerName id);
}
