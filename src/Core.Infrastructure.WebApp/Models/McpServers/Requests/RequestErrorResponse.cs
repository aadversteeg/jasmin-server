namespace Core.Infrastructure.WebApp.Models.McpServers.Requests;

/// <summary>
/// API response model for a request error.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public record RequestErrorResponse(string Code, string Message);
