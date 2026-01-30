using System.Text.Json;

namespace Core.Infrastructure.WebApp.Models.McpServers.Requests;

/// <summary>
/// Response model for an MCP server request.
/// </summary>
public record RequestResponse(
    string RequestId,
    string ServerName,
    string Action,
    string Status,
    string CreatedAt,
    string? CompletedAt,
    string? TargetInstanceId,
    string? ResultInstanceId,
    IReadOnlyList<RequestErrorResponse>? Errors,
    string? ToolName = null,
    JsonElement? Input = null,
    ToolInvocationOutputResponse? Output = null);
