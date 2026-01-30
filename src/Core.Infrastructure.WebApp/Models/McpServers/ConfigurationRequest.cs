namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Request model for creating or updating MCP server configuration.
/// </summary>
public record ConfigurationRequest(
    string Command,
    List<string>? Args,
    Dictionary<string, string>? Env);
