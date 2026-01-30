namespace Core.Infrastructure.WebApp.Models.McpServers.Tools;

/// <summary>
/// Response model for a tool exposed by an MCP server.
/// </summary>
public record ToolResponse(
    string Name,
    string? Title,
    string? Description,
    object? InputSchema);
