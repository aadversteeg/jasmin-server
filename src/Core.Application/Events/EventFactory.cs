using System.Text.Json;
using Core.Domain.Events;

namespace Core.Application.Events;

/// <summary>
/// Factory for creating <see cref="Event"/> instances with optional serialized payloads.
/// </summary>
public static class EventFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates an event without a payload.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="target">The target URI.</param>
    /// <param name="requestId">Optional request ID that triggered this event.</param>
    public static Event Create(EventType type, string target, string? requestId = null)
    {
        return new Event(type, target, DateTime.UtcNow, null, requestId);
    }

    /// <summary>
    /// Creates an event with a typed payload serialized to <see cref="JsonElement"/>.
    /// </summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <param name="type">The event type.</param>
    /// <param name="target">The target URI.</param>
    /// <param name="payload">The payload to serialize.</param>
    /// <param name="requestId">Optional request ID that triggered this event.</param>
    public static Event Create<TPayload>(EventType type, string target, TPayload payload, string? requestId = null)
    {
        var jsonElement = JsonSerializer.SerializeToElement(payload, SerializerOptions);
        return new Event(type, target, DateTime.UtcNow, jsonElement, requestId);
    }
}
