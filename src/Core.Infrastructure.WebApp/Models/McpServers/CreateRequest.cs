namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Request model for creating a new MCP server configuration.
/// </summary>
public record CreateRequest(
    string Name,
    string Command,
    List<string>? Args,
    Dictionary<string, string>? Env);
