using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Application.McpServers;

/// <summary>
/// Manages MCP server instances, including starting and stopping servers.
/// </summary>
public interface IMcpServerInstanceManager
{
    /// <summary>
    /// Starts a new instance of the specified MCP server.
    /// </summary>
    /// <param name="serverName">The name of the server to start.</param>
    /// <param name="requestId">Optional request ID for tracking this operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the instance ID if successful, or an error.</returns>
    Task<Result<McpServerInstanceId, Error>> StartInstanceAsync(
        McpServerName serverName,
        McpServerRequestId? requestId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a running instance.
    /// </summary>
    /// <param name="instanceId">The ID of the instance to stop.</param>
    /// <param name="requestId">Optional request ID for tracking this operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit, Error>> StopInstanceAsync(
        McpServerInstanceId instanceId,
        McpServerRequestId? requestId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all running instances for a specific server.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <returns>A list of running instances for the server.</returns>
    IReadOnlyList<McpServerInstanceInfo> GetRunningInstances(McpServerName serverName);

    /// <summary>
    /// Gets all running instances across all servers.
    /// </summary>
    /// <returns>A list of all running instances.</returns>
    IReadOnlyList<McpServerInstanceInfo> GetAllRunningInstances();

    /// <summary>
    /// Stops all running instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAllAsync(CancellationToken cancellationToken = default);
}
