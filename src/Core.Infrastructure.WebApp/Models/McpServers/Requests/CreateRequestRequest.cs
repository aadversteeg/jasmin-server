using System.Text.Json;

namespace Core.Infrastructure.WebApp.Models.McpServers.Requests;

/// <summary>
/// Request model for creating an async server request.
/// </summary>
public record CreateRequestRequest(
    string Action,
    string? InstanceId,
    string? ToolName = null,
    JsonElement? Input = null);
