using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Client for interacting with MCP servers.
/// </summary>
public interface IMcpServerClient
{
    /// <summary>
    /// Tests connection to an MCP server by starting it and immediately stopping.
    /// </summary>
    /// <param name="definition">The server definition containing connection details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection succeeded, false otherwise.</returns>
    Task<bool> TestConnectionAsync(McpServerDefinition definition, CancellationToken cancellationToken = default);
}
