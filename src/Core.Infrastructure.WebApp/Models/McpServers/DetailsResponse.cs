namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for complete MCP server configuration.
/// </summary>
public record DetailsResponse(
    string Name,
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
