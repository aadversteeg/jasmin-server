namespace Core.Domain.McpServers;

/// <summary>
/// Represents an event in the MCP server lifecycle.
/// </summary>
/// <param name="EventType">The type of event.</param>
/// <param name="TimestampUtc">The UTC timestamp when the event occurred.</param>
/// <param name="ErrorMessage">Optional error message for failure events.</param>
public record McpServerEvent(
    McpServerEventType EventType,
    DateTime TimestampUtc,
    string? ErrorMessage = null);
