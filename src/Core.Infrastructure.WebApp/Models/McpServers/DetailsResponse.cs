using Core.Infrastructure.WebApp.Models.McpServers.Instances;
using Core.Infrastructure.WebApp.Models.McpServers.Requests;

namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for MCP server details.
/// </summary>
public record DetailsResponse(
    string Name,
    string Status,
    string? UpdatedAt,
    ConfigurationResponse? Configuration = null,
    IReadOnlyList<EventResponse>? Events = null,
    IReadOnlyList<RequestResponse>? Requests = null,
    IReadOnlyList<InstanceResponse>? Instances = null);
