using System.Text.Json;

namespace Core.Infrastructure.WebApp.Models.Requests;

/// <summary>
/// Response model for a generic async request.
/// </summary>
/// <param name="Id">The request ID.</param>
/// <param name="Action">The request action (e.g. "mcp-server.instance.invoke-tool").</param>
/// <param name="Target">The target URI (e.g. "mcp-servers/chronos/instances/abc123").</param>
/// <param name="Status">The request status (pending, running, completed, failed).</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="CompletedAt">The completion timestamp, if completed or failed.</param>
/// <param name="Parameters">The action-specific parameters, if any.</param>
/// <param name="Output">The action-specific output, if completed.</param>
/// <param name="Errors">The errors, if failed.</param>
public record RequestResponse(
    string Id,
    string Action,
    string Target,
    string Status,
    string CreatedAt,
    string? CompletedAt,
    JsonElement? Parameters,
    JsonElement? Output,
    IReadOnlyList<RequestErrorResponse>? Errors);
