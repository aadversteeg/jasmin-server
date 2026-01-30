namespace Core.Domain.McpServers;

/// <summary>
/// Represents an event in the MCP server lifecycle.
/// </summary>
/// <param name="EventType">The type of event.</param>
/// <param name="TimestampUtc">The UTC timestamp when the event occurred.</param>
/// <param name="Errors">Optional list of errors for failure events.</param>
/// <param name="InstanceId">Optional instance ID for instance-specific events.</param>
/// <param name="RequestId">Optional request ID for request-initiated events.</param>
/// <param name="OldConfiguration">Previous configuration for update/delete events.</param>
/// <param name="Configuration">Configuration for create/update/start events.</param>
/// <param name="ToolInvocationData">Data for tool invocation events.</param>
public record McpServerEvent(
    McpServerEventType EventType,
    DateTime TimestampUtc,
    IReadOnlyList<McpServerEventError>? Errors = null,
    McpServerInstanceId? InstanceId = null,
    McpServerRequestId? RequestId = null,
    McpServerEventConfiguration? OldConfiguration = null,
    McpServerEventConfiguration? Configuration = null,
    McpServerToolInvocationEventData? ToolInvocationData = null);
