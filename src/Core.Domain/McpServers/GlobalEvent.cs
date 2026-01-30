namespace Core.Domain.McpServers;

/// <summary>
/// Represents a global event in the system (e.g., server created/deleted).
/// </summary>
/// <param name="EventType">The type of global event.</param>
/// <param name="ServerName">The name of the server associated with this event.</param>
/// <param name="TimestampUtc">The UTC timestamp when the event occurred.</param>
public record GlobalEvent(
    GlobalEventType EventType,
    McpServerName ServerName,
    DateTime TimestampUtc);
