namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// API response model for an event error.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public record EventErrorResponse(string Code, string Message);
