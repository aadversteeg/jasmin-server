namespace Core.Infrastructure.WebApp.Models.McpServers.Resources;

/// <summary>
/// Response model for resource content.
/// </summary>
public record ResourceContentResponse(
    string Uri,
    string? MimeType,
    string? Text,
    string? Blob);

/// <summary>
/// Response model for a resource read operation output.
/// </summary>
public record ResourceReadOutputResponse(
    IReadOnlyList<ResourceContentResponse> Contents);
