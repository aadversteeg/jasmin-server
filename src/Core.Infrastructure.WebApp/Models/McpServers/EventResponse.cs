namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for an MCP server event.
/// </summary>
public record EventResponse(
    string EventType,
    string TimestampUtc,
    IReadOnlyList<EventErrorResponse>? Errors,
    string? InstanceId,
    string? RequestId,
    EventConfigurationResponse? OldConfiguration = null,
    EventConfigurationResponse? NewConfiguration = null);
