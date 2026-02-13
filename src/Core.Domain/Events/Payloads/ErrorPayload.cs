namespace Core.Domain.Events.Payloads;

/// <summary>
/// Payload for events that carry error information.
/// </summary>
/// <param name="Errors">The list of errors.</param>
public record ErrorPayload(IReadOnlyList<EventError> Errors);
