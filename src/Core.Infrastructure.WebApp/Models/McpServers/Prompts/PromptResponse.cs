namespace Core.Infrastructure.WebApp.Models.McpServers.Prompts;

/// <summary>
/// Response model for a prompt exposed by an MCP server.
/// </summary>
public record PromptResponse(
    string Name,
    string? Title,
    string? Description,
    IReadOnlyList<PromptArgumentResponse>? Arguments);
