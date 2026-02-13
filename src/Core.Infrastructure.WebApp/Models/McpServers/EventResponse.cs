using System.Text.Json;

namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for an event.
/// </summary>
public record EventResponse(
    string EventType,
    string Target,
    string Timestamp,
    JsonElement? Payload,
    string? RequestId);
