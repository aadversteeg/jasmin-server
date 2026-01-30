namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for MCP server summary information.
/// </summary>
public record ListResponse(string Name, string Status, string? UpdatedAt);
