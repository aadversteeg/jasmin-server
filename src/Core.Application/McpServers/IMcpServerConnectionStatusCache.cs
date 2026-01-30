using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Represents a cached status entry with timestamp.
/// </summary>
/// <param name="Status">The connection status.</param>
/// <param name="UpdatedOnUtc">The UTC timestamp when the status was last updated.</param>
public record McpServerStatusCacheEntry(McpServerConnectionStatus Status, DateTime? UpdatedOnUtc);

/// <summary>
/// Cache for MCP server connection status.
/// </summary>
public interface IMcpServerConnectionStatusCache
{
    /// <summary>
    /// Gets or creates an McpServerId for the given server name.
    /// If the name already has a mapping, returns the existing Id.
    /// Otherwise creates a new Id and stores the mapping.
    /// </summary>
    /// <param name="name">The server name.</param>
    /// <returns>The McpServerId associated with the name.</returns>
    McpServerId GetOrCreateId(McpServerName name);

    /// <summary>
    /// Gets the cached status entry for a server by its Id.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <returns>The cached entry with status and timestamp, or Unknown status with null timestamp if not cached.</returns>
    McpServerStatusCacheEntry GetEntry(McpServerId id);

    /// <summary>
    /// Sets the connection status for a server by its Id with current UTC timestamp.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <param name="status">The connection status to cache.</param>
    void SetStatus(McpServerId id, McpServerConnectionStatus status);

    /// <summary>
    /// Removes the cached status and name mapping for a server.
    /// </summary>
    /// <param name="name">The server name.</param>
    void RemoveByName(McpServerName name);

    /// <summary>
    /// Records an event for the specified server.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <param name="eventType">The type of event.</param>
    /// <param name="errorMessage">Optional error message for failure events.</param>
    /// <param name="instanceId">Optional instance identifier for instance-specific events.</param>
    /// <param name="requestId">Optional request identifier for request-initiated events.</param>
    void RecordEvent(McpServerId id, McpServerEventType eventType, string? errorMessage = null, McpServerInstanceId? instanceId = null, McpServerRequestId? requestId = null);

    /// <summary>
    /// Gets all events for the specified server, ordered by timestamp.
    /// </summary>
    /// <param name="id">The server identifier.</param>
    /// <returns>The list of events for the server.</returns>
    IReadOnlyList<McpServerEvent> GetEvents(McpServerId id);
}
