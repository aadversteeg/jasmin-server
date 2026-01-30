namespace Core.Domain.McpServers;

/// <summary>
/// Represents a resource exposed by an MCP server.
/// </summary>
/// <param name="Name">The unique name of the resource.</param>
/// <param name="Uri">The URI to access the resource.</param>
/// <param name="Title">Human-readable title.</param>
/// <param name="Description">Description of the resource.</param>
/// <param name="MimeType">The MIME type of the resource content.</param>
public record McpResource(
    string Name,
    string Uri,
    string? Title,
    string? Description,
    string? MimeType);
