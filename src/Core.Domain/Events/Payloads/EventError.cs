namespace Core.Domain.Events.Payloads;

/// <summary>
/// Represents an error within an event payload.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public record EventError(string Code, string Message);
