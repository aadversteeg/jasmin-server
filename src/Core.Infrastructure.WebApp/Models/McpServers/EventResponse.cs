namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for an MCP server event.
/// </summary>
public record EventResponse(
    string EventType,
    string TimestampUtc,
    string? ErrorMessage,
    string? InstanceId,
    string? RequestId);
