using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Information about a running MCP server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier for this instance.</param>
/// <param name="ServerName">The name of the server this instance belongs to.</param>
/// <param name="StartedAtUtc">The UTC timestamp when the instance was started.</param>
public record McpServerInstanceInfo(
    McpServerInstanceId InstanceId,
    McpServerName ServerName,
    DateTime StartedAtUtc);
