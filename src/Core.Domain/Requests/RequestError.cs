namespace Core.Domain.Requests;

/// <summary>
/// Represents an error that occurred during request processing.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public record RequestError(string Code, string Message);
