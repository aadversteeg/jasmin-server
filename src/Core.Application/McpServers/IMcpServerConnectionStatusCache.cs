using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Represents a cached status entry with timestamp.
/// </summary>
/// <param name="Status">The connection status.</param>
/// <param name="VerifiedAtUtc">The UTC timestamp when the status was last verified.</param>
public record McpServerStatusCacheEntry(McpServerConnectionStatus Status, DateTime? VerifiedAtUtc);

/// <summary>
/// Cache for MCP server connection status.
/// </summary>
public interface IMcpServerConnectionStatusCache
{
    /// <summary>
    /// Gets the cached status entry for a server.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <returns>The cached entry with status and timestamp, or Unknown status with null timestamp if not cached.</returns>
    McpServerStatusCacheEntry GetEntry(McpServerId id);

    /// <summary>
    /// Sets the connection status for a server with current UTC timestamp.
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
