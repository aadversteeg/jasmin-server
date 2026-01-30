namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Request model for updating an existing MCP server configuration.
/// </summary>
public record UpdateRequest(
    string Command,
    List<string>? Args,
    Dictionary<string, string>? Env);
