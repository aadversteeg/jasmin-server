using System.Text.Json;

namespace Core.Domain.Events;

/// <summary>
/// Represents a generic event with a typed payload serialized to JSON.
/// </summary>
/// <param name="Type">The hierarchical event type.</param>
/// <param name="Target">The target URI (e.g. <c>mcp-servers/{name}/instances/{id}</c>).</param>
/// <param name="TimestampUtc">When the event occurred.</param>
/// <param name="Payload">Optional typed payload serialized as JSON.</param>
/// <param name="RequestId">Optional ID of the request that triggered this event.</param>
public record Event(
    EventType Type,
    string Target,
    DateTime TimestampUtc,
    JsonElement? Payload = null,
    string? RequestId = null);
