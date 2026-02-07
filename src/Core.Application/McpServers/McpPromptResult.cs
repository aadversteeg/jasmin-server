namespace Core.Application.McpServers;

/// <summary>
/// Represents the result of getting a prompt from an MCP server instance.
/// </summary>
/// <param name="Messages">The messages returned by the prompt.</param>
/// <param name="Description">Optional description of the prompt.</param>
public record McpPromptResult(
    IReadOnlyList<McpPromptMessage> Messages,
    string? Description);

/// <summary>
/// Represents a message in a prompt result.
/// </summary>
/// <param name="Role">The role of the message (e.g., "user", "assistant").</param>
/// <param name="Content">The content of the message.</param>
public record McpPromptMessage(
    string Role,
    McpPromptMessageContent Content);

/// <summary>
/// Represents the content of a prompt message.
/// </summary>
/// <param name="Type">The type of content (e.g., "text", "image", "resource").</param>
/// <param name="Text">The text content (for text blocks).</param>
/// <param name="MimeType">The MIME type (for image/resource blocks).</param>
/// <param name="Data">Base64-encoded data (for image blocks).</param>
/// <param name="Uri">The URI (for resource blocks).</param>
public record McpPromptMessageContent(
    string Type,
    string? Text,
    string? MimeType,
    string? Data,
    string? Uri);
