using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Application.McpServers;

/// <summary>
/// Repository for retrieving MCP server configurations.
/// </summary>
public interface IMcpServerRepository
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
    Result<Maybe<McpServerDefinition>, Error> GetById(McpServerId id);
}
