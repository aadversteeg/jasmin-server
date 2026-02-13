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
        string? requestId = null,
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
        string? requestId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all running instances for a specific server.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <returns>A list of running instances for the server.</returns>
    IReadOnlyList<McpServerInstanceInfo> GetRunningInstances(McpServerName serverName);

    /// <summary>
    /// Gets a specific running instance.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="instanceId">The ID of the instance.</param>
    /// <returns>The instance info if found, null otherwise.</returns>
    McpServerInstanceInfo? GetInstance(McpServerName serverName, McpServerInstanceId instanceId);

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

    /// <summary>
    /// Invokes a tool on a running instance.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="instanceId">The ID of the instance to invoke the tool on.</param>
    /// <param name="toolName">The name of the tool to invoke.</param>
    /// <param name="arguments">Optional arguments to pass to the tool.</param>
    /// <param name="requestId">Optional request ID for tracking this operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the tool invocation result if successful, or an error.</returns>
    Task<Result<McpToolInvocationResult, Error>> InvokeToolAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments,
        string? requestId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a prompt from a running instance.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="instanceId">The ID of the instance to get the prompt from.</param>
    /// <param name="promptName">The name of the prompt to get.</param>
    /// <param name="arguments">Optional arguments to pass to the prompt.</param>
    /// <param name="requestId">Optional request ID for tracking this operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the prompt result if successful, or an error.</returns>
    Task<Result<McpPromptResult, Error>> GetPromptAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string promptName,
        IReadOnlyDictionary<string, string?>? arguments,
        string? requestId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a resource from a running instance.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="instanceId">The ID of the instance to read the resource from.</param>
    /// <param name="resourceUri">The URI of the resource to read.</param>
    /// <param name="requestId">Optional request ID for tracking this operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the resource content if successful, or an error.</returns>
    Task<Result<McpResourceReadResult, Error>> ReadResourceAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string resourceUri,
        string? requestId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a configuration by starting a temporary instance and immediately stopping it.
    /// Does not persist any server configuration.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="args">Optional command arguments.</param>
    /// <param name="env">Optional environment variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or an error with details.</returns>
    Task<Result<Unit, Error>> TestConfigurationAsync(
        string command,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes metadata (tools, prompts, resources) from a running instance.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="instanceId">The ID of the instance to refresh metadata from.</param>
    /// <param name="requestId">Optional request ID for tracking this operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the refreshed metadata if successful, or an error.</returns>
    Task<Result<McpServerMetadata, Error>> RefreshMetadataAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string? requestId = null,
        CancellationToken cancellationToken = default);
}
