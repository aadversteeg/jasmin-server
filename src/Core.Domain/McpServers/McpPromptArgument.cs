namespace Core.Domain.McpServers;

/// <summary>
/// Represents an argument for an MCP prompt.
/// </summary>
/// <param name="Name">The argument name.</param>
/// <param name="Description">Description of the argument.</param>
/// <param name="Required">Whether the argument is required.</param>
public record McpPromptArgument(
    string Name,
    string? Description,
    bool Required);
