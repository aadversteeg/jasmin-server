namespace Core.Domain.McpServers;

/// <summary>
/// Represents a tool exposed by an MCP server.
/// </summary>
/// <param name="Name">The unique name of the tool.</param>
/// <param name="Title">Human-readable title.</param>
/// <param name="Description">Description of what the tool does.</param>
/// <param name="InputSchema">JSON schema for the tool's input parameters.</param>
public record McpTool(
    string Name,
    string? Title,
    string? Description,
    string? InputSchema);
