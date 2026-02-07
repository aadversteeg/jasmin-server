namespace Core.Infrastructure.WebApp.Models.McpServers.Requests;

/// <summary>
/// Response model for prompt output.
/// </summary>
public record PromptOutputResponse(
    IReadOnlyList<PromptMessageResponse> Messages,
    string? Description);

/// <summary>
/// Response model for a prompt message.
/// </summary>
public record PromptMessageResponse(
    string Role,
    PromptMessageContentResponse Content);

/// <summary>
/// Response model for prompt message content.
/// </summary>
public record PromptMessageContentResponse(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);
