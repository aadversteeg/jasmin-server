namespace Core.Domain.McpServers;

/// <summary>
/// Represents a prompt exposed by an MCP server.
/// </summary>
/// <param name="Name">The unique name of the prompt.</param>
/// <param name="Title">Human-readable title.</param>
/// <param name="Description">Description of the prompt.</param>
/// <param name="Arguments">The prompt's arguments.</param>
public record McpPrompt(
    string Name,
    string? Title,
    string? Description,
    IReadOnlyList<McpPromptArgument>? Arguments);
