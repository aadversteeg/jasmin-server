namespace Core.Infrastructure.WebApp.Models.McpServers.Prompts;

/// <summary>
/// Response model for a prompt argument.
/// </summary>
public record PromptArgumentResponse(
    string Name,
    string? Description,
    bool Required);
