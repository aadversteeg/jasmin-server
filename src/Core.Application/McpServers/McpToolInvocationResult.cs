using System.Text.Json;

namespace Core.Application.McpServers;

/// <summary>
/// Represents the result of invoking a tool on an MCP server instance.
/// </summary>
/// <param name="Content">The content blocks returned by the tool.</param>
/// <param name="StructuredContent">Optional structured JSON content.</param>
/// <param name="IsError">Whether the tool invocation resulted in an error.</param>
public record McpToolInvocationResult(
    IReadOnlyList<McpToolContentBlock> Content,
    JsonElement? StructuredContent,
    bool IsError);
