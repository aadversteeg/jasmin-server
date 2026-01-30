using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Cache for MCP server connection status.
/// </summary>
public interface IMcpServerConnectionStatusCache
{
    /// <summary>
    /// Gets the cached connection status for a server.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <returns>The cached status, or Unknown if not cached.</returns>
    McpServerConnectionStatus GetStatus(McpServerId id);

    /// <summary>
    /// Sets the connection status for a server.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <param name="status">The connection status to cache.</param>
    void SetStatus(McpServerId id, McpServerConnectionStatus status);

    /// <summary>
    /// Removes the cached status for a server.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    void RemoveStatus(McpServerId id);
}
