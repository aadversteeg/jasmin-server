namespace Core.Infrastructure.WebApp.Models.McpServers.Resources;

/// <summary>
/// Response model for a resource exposed by an MCP server.
/// </summary>
public record ResourceResponse(
    string Name,
    string Uri,
    string? Title,
    string? Description,
    string? MimeType);
