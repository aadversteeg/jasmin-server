namespace Core.Infrastructure.WebApp.Models.Events;

/// <summary>
/// Response model for an event type.
/// </summary>
public record EventTypeResponse(
    string Name,
    int Value,
    string Category,
    string? Description);
