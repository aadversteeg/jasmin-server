namespace Core.Infrastructure.WebApp.Models;

/// <summary>
/// Response model for complete MCP server configuration.
/// </summary>
public record McpServerDefinitionResponse(
    string Name,
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
