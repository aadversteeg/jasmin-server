namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for MCP server configuration.
/// </summary>
public record ConfigurationResponse(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
