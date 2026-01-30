namespace Core.Application.McpServers;

/// <summary>
/// Represents a content block in a tool invocation result.
/// </summary>
/// <param name="Type">The type of content (e.g., "text", "image", "resource").</param>
/// <param name="Text">The text content (for text blocks).</param>
/// <param name="MimeType">The MIME type (for image/resource blocks).</param>
/// <param name="Data">Base64-encoded data (for image blocks).</param>
/// <param name="Uri">The URI (for resource blocks).</param>
public record McpToolContentBlock(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);
