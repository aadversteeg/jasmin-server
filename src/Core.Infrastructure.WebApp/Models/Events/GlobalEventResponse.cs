namespace Core.Infrastructure.WebApp.Models.Events;

/// <summary>
/// Response model for a global event.
/// </summary>
public record GlobalEventResponse(
    string EventType,
    string ServerName,
    string CreatedAt);
