namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Request model for testing an MCP server configuration without persisting it.
/// </summary>
public record TestConfigurationRequest(
    string Command,
    List<string>? Args,
    Dictionary<string, string>? Env);
