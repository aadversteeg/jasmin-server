namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Request model for creating a new MCP server.
/// </summary>
public record CreateRequest(
    string Name,
    ConfigurationRequest? Configuration);
