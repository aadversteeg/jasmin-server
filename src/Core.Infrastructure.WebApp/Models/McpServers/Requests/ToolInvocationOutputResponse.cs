using System.Text.Json;

namespace Core.Infrastructure.WebApp.Models.McpServers.Requests;

/// <summary>
/// Response model for tool invocation output.
/// </summary>
public record ToolInvocationOutputResponse(
    IReadOnlyList<ToolContentBlockResponse> Content,
    JsonElement? StructuredContent,
    bool IsError);
