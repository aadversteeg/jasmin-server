using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Information about a running MCP server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier for this instance.</param>
/// <param name="ServerName">The name of the server this instance belongs to.</param>
/// <param name="StartedAtUtc">The UTC timestamp when the instance was started.</param>
/// <param name="Configuration">The configuration used to start this instance.</param>
/// <param name="Metadata">The metadata retrieved from this instance (tools, prompts, resources).</param>
public record McpServerInstanceInfo(
    McpServerInstanceId InstanceId,
    McpServerName ServerName,
    DateTime StartedAtUtc,
    McpServerEventConfiguration? Configuration,
    McpServerMetadata? Metadata);
