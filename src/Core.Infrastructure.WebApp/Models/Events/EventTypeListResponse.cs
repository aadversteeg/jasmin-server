namespace Core.Infrastructure.WebApp.Models.Events;

/// <summary>
/// Response model for a list of event types.
/// </summary>
public record EventTypeListResponse(
    IReadOnlyList<EventTypeResponse> Items);
