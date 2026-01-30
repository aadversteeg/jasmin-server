namespace Core.Infrastructure.WebApp.Models.McpServers.Requests;

/// <summary>
/// Response model for a content block in a tool invocation result.
/// </summary>
public record ToolContentBlockResponse(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);
