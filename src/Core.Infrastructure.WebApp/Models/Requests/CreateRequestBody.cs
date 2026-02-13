using System.Text.Json;

namespace Core.Infrastructure.WebApp.Models.Requests;

/// <summary>
/// Request body for creating a new async request.
/// </summary>
/// <param name="Action">The request action (e.g. "mcp-server.instance.invoke-tool").</param>
/// <param name="Target">Optional target URI (e.g. "mcp-servers/chronos/instances/abc123"). Required for most actions.</param>
/// <param name="Parameters">Optional action-specific parameters.</param>
public record CreateRequestBody(
    string Action,
    string? Target = null,
    JsonElement? Parameters = null);
